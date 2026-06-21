using System;
using System.IO;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Sqloom.AzureSql.QueryStore;
using Sqloom.Core.Artifacts;
using Sqloom.QueryStore.QueryStore;

namespace Sqloom.Host;

/// <summary>
/// Runs the Sqloom observe stage against Query Store.
/// </summary>
internal sealed class ObserveCommand
    : ICommandHandler
{
    private readonly ObserveArgumentParser _argumentParser = new();

    public HostCommandKind CommandKind => HostCommandKind.Observe;

    public async Task<int> ExecuteAsync(CommandExecutionContext context)
    {
        var appIntegration = context.AppIntegration;
        context.ConsoleWriter.PrintBanner(
            appIntegration?.AppName,
            HostApplication.GetProjectNames(appIntegration));

        var readOnlyConnectionString = _argumentParser.GetQueryStoreConnectionString(context.Arguments);
        if (string.IsNullOrWhiteSpace(readOnlyConnectionString))
        {
            Console.Error.WriteLine(
                "Query Store capture requires --read-only-connection-string.");
            return 1;
        }

        var arguments = _argumentParser.Parse(
            context.Arguments,
            appIntegration,
            readOnlyConnectionString,
            context.CurrentDirectory);
        arguments.DebugWriter = context.DebugWriter;
        var result = await ExecuteAsync(arguments).ConfigureAwait(false);
        context.ConsoleWriter.PrintQueryStoreSnapshot(
            result.Snapshot,
            result.JsonOutputPath,
            result.AppOnly,
            result.ShowClassification);
        return 0;
    }

    public async Task<ObserveCommandResult> ExecuteAsync(
        ObserveArguments arguments,
        CancellationToken cancellationToken = default)
    {
        var discoveredObjectCatalog = await CaptureDiscoveredObjectCatalogAsync(
                arguments.ReadOnlyConnectionString,
                arguments.ObservationOptions)
            .ConfigureAwait(false);
        var workloadProfile = arguments.BaseWorkloadProfile.WithDiscoveredObjectCatalog(
            discoveredObjectCatalog);

        AzureSqlQueryStoreCollector collector = new();
        var rawSnapshot = await collector
            .CaptureAsync(
                arguments.ReadOnlyConnectionString,
                arguments.ObservationOptions)
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

        QueryStoreWorkloadClassifier classifier = new();
        var snapshot = classifier.ApplyClassification(
            snapshotWithDiscovery,
            workloadProfile);
        var jsonOutputPath = ResolveSnapshotPath(
            arguments.JsonOutputPathOverride,
            arguments.CurrentDirectory,
            snapshot.CapturedAtUtc);
        arguments.DebugWriter.PrintObserveRun(
            arguments,
            jsonOutputPath,
            snapshot);

        await JsonFileWriter.WriteAsync(
                jsonOutputPath,
                snapshot,
                static serializerOptions => serializerOptions.Converters.Add(new JsonStringEnumConverter()),
                cancellationToken)
            .ConfigureAwait(false);

        return new ObserveCommandResult
        {
            Snapshot = snapshot,
            JsonOutputPath = jsonOutputPath,
            AppOnly = arguments.AppOnly,
            ShowClassification = arguments.ShowClassification,
        };
    }

    private static async Task<DiscoveredDatabaseObjectCatalog> CaptureDiscoveredObjectCatalogAsync(
        string readOnlyConnectionString,
        QueryStoreObservationOptions options)
    {
        AzureSqlDiscoveredObjectCollector collector = new();
        DiscoveredDatabaseObjectObservationOptions discoveryOptions = new()
        {
            CommandTimeoutSeconds = options.CommandTimeoutSeconds,
        };

        try
        {
            return await collector
                .CaptureAsync(readOnlyConnectionString, discoveryOptions)
                .ConfigureAwait(false);
        }
        catch (SqlException sqlException)
        {
            return new DiscoveredDatabaseObjectCatalog
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

        return ArtifactLayout.GetDefaultQueryStoreSnapshotPath(
            artifactRoot,
            capturedAtUtc);
    }
}

/// <summary>
/// Carries the result of the Sqloom observe command.
/// </summary>
internal sealed class ObserveCommandResult
{
    public required QueryStoreSnapshot Snapshot { get; init; }

    public required string JsonOutputPath { get; init; }

    public bool AppOnly { get; init; }

    public bool ShowClassification { get; init; }
}
