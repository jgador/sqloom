using System;
using System.IO;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Sqloom.Host.QueryStore;
using Sqloom.Pipeline.Artifacts;
using Sqloom.Pipeline.QueryStore;

namespace Sqloom.Host;

/// <summary>
/// Runs the Sqloom observe stage against Query Store.
/// </summary>
internal sealed class ObserveCommand
    : ICommandHandler
{
    public HostCommandKind CommandKind => HostCommandKind.Observe;

    public async Task<int> ExecuteAsync(CommandExecutionContext context)
    {
        var manifest = context.Application?.Describe(new Sqloom.Testing.SqloomApplicationContext
        {
            CurrentDirectory = context.CurrentDirectory,
        });
        HostConsoleWriter.PrintBanner(
            manifest?.Name,
            HostApplication.GetProjectNames(context.Application));

        var readOnlyConnectionString = ObserveArgumentParser.GetQueryStoreConnectionString(context.Arguments);
        if (string.IsNullOrWhiteSpace(readOnlyConnectionString))
        {
            Console.Error.WriteLine(
                "Query Store capture requires --read-only-connection-string.");
            return 1;
        }

        var arguments = ObserveArgumentParser.Parse(
            context.Arguments,
            manifest,
            readOnlyConnectionString,
            context.CurrentDirectory);
        arguments.DebugEnabled = context.DebugEnabled;
        var result = await ExecuteAsync(arguments).ConfigureAwait(false);
        HostConsoleWriter.PrintQueryStoreSnapshot(
            result.Snapshot,
            result.JsonOutputPath,
            arguments.AppOnly,
            arguments.ShowClassification);
        return 0;
    }

    internal async Task<(QueryStoreSnapshot Snapshot, string JsonOutputPath)> ExecuteAsync(
        ObserveArguments arguments,
        CancellationToken cancellationToken = default)
    {
        var discoveredObjectCatalog = await CaptureDbCatalogAsync(
                arguments.ReadOnlyConnection,
                arguments.ObservationOptions,
                cancellationToken)
            .ConfigureAwait(false);
        var workloadProfile = arguments.BaseWorkloadProfile.WithDiscoveredObjectCatalog(
            discoveredObjectCatalog);

        var rawSnapshot = await SqlServerQueryStoreCollector
            .CaptureAsync(
                arguments.ReadOnlyConnection,
                arguments.ObservationOptions,
                cancellationToken)
            .ConfigureAwait(false);
        QueryStoreSnapshot snapshotWithDiscovery = new()
        {
            CapturedAtUtc = rawSnapshot.CapturedAtUtc,
            LookbackWindow = rawSnapshot.LookbackWindow,
            DatabaseOptions = rawSnapshot.DatabaseOptions,
            WorkloadProfileName = workloadProfile.Name,
            DiscoveredObjectCatalog = discoveredObjectCatalog,
            Plans = rawSnapshot.Plans,
            Waits = rawSnapshot.Waits,
        };

        WorkloadClassifier classifier = new();
        var snapshot = classifier.ApplyClassification(
            snapshotWithDiscovery,
            workloadProfile);
        var jsonOutputPath = ResolveSnapshotPath(
            arguments.JsonOutputPathOverride,
            arguments.CurrentDirectory,
            snapshot.CapturedAtUtc);
        HostDebugWriter.PrintObserveRun(
            arguments.DebugEnabled,
            arguments,
            jsonOutputPath,
            snapshot);

        await JsonFileWriter.WriteAsync(
                jsonOutputPath,
                snapshot,
                static serializerOptions => serializerOptions.Converters.Add(new JsonStringEnumConverter()),
                cancellationToken)
            .ConfigureAwait(false);

        return (snapshot, jsonOutputPath);
    }

    private static async Task<DbObjectCatalog> CaptureDbCatalogAsync(
        string readOnlyConnectionString,
        QueryStoreOptions options,
        CancellationToken cancellationToken)
    {
        DbObjectScanOptions discoveryOptions = new()
        {
            CommandTimeoutSeconds = options.CommandTimeoutSeconds,
        };

        try
        {
            return await SqlServerDiscoveredObjectCollector
                .CaptureAsync(
                    readOnlyConnectionString,
                    discoveryOptions,
                    cancellationToken)
                .ConfigureAwait(false);
        }
        catch (SqlException sqlException)
        {
            // Object discovery is best-effort: keep Query Store evidence usable when metadata capture fails.
            return new DbObjectCatalog
            {
                CapturedAtUtc = DateTimeOffset.UtcNow,
                SourceName = GetDiscoverySourceName(readOnlyConnectionString),
                IsComplete = false,
                Warnings =
                [
                    $"Discovered-object catalog capture failed: {sqlException.Message}",
                ],
                Objects = Array.Empty<DiscoveredDatabaseObject>(),
            };
        }
    }

    private static string GetDiscoverySourceName(string readOnlyConnectionString)
    {
        SqlConnectionStringBuilder builder = new(readOnlyConnectionString);
        return string.IsNullOrWhiteSpace(builder.InitialCatalog)
            ? "current database"
            : builder.InitialCatalog;
    }

    private static string ResolveSnapshotPath(
        string? jsonOutputPathOverride,
        string currentDirectory,
        DateTimeOffset capturedAtUtc)
    {
        if (!string.IsNullOrWhiteSpace(jsonOutputPathOverride))
        {
            return Path.GetFullPath(jsonOutputPathOverride);
        }

        var artifactRoot = ArtifactRootResolver.Resolve(currentDirectory);

        return ArtifactLayout.GetQueryStoreSnapshotPath(
            artifactRoot,
            capturedAtUtc);
    }
}
