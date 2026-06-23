using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Sqloom.Core.Execution;
using Sqloom.TestApp.Harness;
using Xunit;
using SqloomTestApp = global::Sqloom.TestApp;

namespace Sqloom.Host.Tests;

/// <summary>
/// Exercises post-DACPAC SQL seed-script flows for the Sqloom sample harness.
/// </summary>
public sealed class HostSeedScriptTests
{
    private const string DefaultConnectionKey = "ConnectionStrings:DefaultConnection";

    [RequiresDockerFact]
    [Trait("Category", "Integration")]
    public async Task CreateAsync_WithCustomSeedScript_SkipsBuiltInSeeder()
    {
        var tempDirectory = CreateTempDir();
        var dacpacPath = SqloomTestAppPaths.GetDacpacPath();
        var seedScriptPath = Path.Combine(tempDirectory, "AdventureWorksLT2025.seed.sql");
        File.WriteAllText(
            seedScriptPath,
            SqloomTestAppSeedScripts.CreateCustomSeedScript());

        try
        {
            ReplayHostFactory replayHostFactory = new();
            var replayHost = await replayHostFactory
                .CreateAsync(
                    new ReplayLaunchOptions
                    {
                        DacpacPath = dacpacPath,
                        SeedSqlPath = seedScriptPath,
                    })
                .ConfigureAwait(false);

            await using (replayHost.ConfigureAwait(false))
            {
                var seedArtifact = Assert.IsType<SqlServerSeedSqlArtifact>(replayHost.Bootstrap.SqlServerSeedSql);
                Assert.Equal(Path.GetFullPath(seedScriptPath), seedArtifact.SourcePath);
                Assert.Equal(Path.GetFileName(seedScriptPath), seedArtifact.FileName);

                var products = await ReadProductsByCategoryAsync(replayHost).ConfigureAwait(false);

                Assert.Equal(2, products.Count);
                Assert.All(
                    products,
                    product => Assert.StartsWith("SEED-", product.ProductNumber, StringComparison.Ordinal));
                Assert.DoesNotContain(
                    products,
                    product => product.ProductNumber.StartsWith("HOT-", StringComparison.Ordinal));
            }
        }
        finally
        {
            DeleteDirectoryIfExists(tempDirectory);
        }
    }

    [RequiresDockerFact]
    [Trait("Category", "Integration")]
    public async Task CreateAsync_WithExportedSeedScript_RestoresHotSeedIntoFreshContainer()
    {
        var tempDirectory = CreateTempDir();
        var dacpacPath = SqloomTestAppPaths.GetDacpacPath();
        var exportScriptPath = SqloomTestAppPaths.GetSeedExportScriptPath();
        var exportedSeedScriptPath = Path.Combine(tempDirectory, "AdventureWorksLT2025.seed.sql");

        try
        {
            ReplayHostFactory sourceReplayHostFactory = new();
            var sourceReplayHost = await sourceReplayHostFactory
                .CreateAsync(
                    new ReplayLaunchOptions
                    {
                        DacpacPath = dacpacPath,
                    })
                .ConfigureAwait(false);

            await using (sourceReplayHost.ConfigureAwait(false))
            {
                var sourceProducts = await ReadProductsByCategoryAsync(sourceReplayHost).ConfigureAwait(false);
                AssertHotSeededProducts(sourceProducts);

                var configuration = sourceReplayHost.Services.GetRequiredService<IConfiguration>();
                var sourceConnectionString = configuration[DefaultConnectionKey]
                    ?? throw new InvalidOperationException("Missing sample replay application connection string.");

                var exportResult = await RunPowerShellAsync(
                        SqloomTestAppPaths.GetRepositoryRoot(),
                        [
                            "-NoProfile",
                            "-File",
                            exportScriptPath,
                            "-ConnectionString",
                            sourceConnectionString,
                            "-OutputPath",
                            exportedSeedScriptPath,
                        ])
                    .ConfigureAwait(false);
                AssertCommandSucceeded("export seed sql", exportResult);
            }

            Assert.True(
                File.Exists(exportedSeedScriptPath),
                $"Expected an exported SQL seed script at '{exportedSeedScriptPath}'.");
            var exportedSeedSql = await File.ReadAllTextAsync(exportedSeedScriptPath).ConfigureAwait(false);
            Assert.Contains("INSERT INTO [SalesLT].[Product]", exportedSeedSql, StringComparison.Ordinal);
            Assert.Contains("HOT-000001", exportedSeedSql, StringComparison.Ordinal);
            Assert.DoesNotContain("[dbo].[sysdiagrams]", exportedSeedSql, StringComparison.Ordinal);

            ReplayHostFactory restoredReplayHostFactory = new();
            var restoredReplayHost = await restoredReplayHostFactory
                .CreateAsync(
                    new ReplayLaunchOptions
                    {
                        DacpacPath = dacpacPath,
                        SeedSqlPath = exportedSeedScriptPath,
                    })
                .ConfigureAwait(false);

            await using (restoredReplayHost.ConfigureAwait(false))
            {
                var seedArtifact = Assert.IsType<SqlServerSeedSqlArtifact>(restoredReplayHost.Bootstrap.SqlServerSeedSql);
                Assert.Equal(Path.GetFullPath(exportedSeedScriptPath), seedArtifact.SourcePath);
                Assert.Equal(Path.GetFileName(exportedSeedScriptPath), seedArtifact.FileName);

                var restoredProducts = await ReadProductsByCategoryAsync(restoredReplayHost).ConfigureAwait(false);
                AssertHotSeededProducts(restoredProducts);
            }
        }
        finally
        {
            DeleteDirectoryIfExists(tempDirectory);
        }
    }

    [RequiresDockerFact]
    [Trait("Category", "Integration")]
    public async Task CreateAsync_WithCommittedSeedScript_BootstrapsAdventureWorksIntoFreshContainer()
    {
        var dacpacPath = SqloomTestAppPaths.GetDacpacPath();
        var seedScriptPath = SqloomTestAppPaths.GetSqlServerSeedScriptPath();
        var seedScriptSql = await File.ReadAllTextAsync(seedScriptPath).ConfigureAwait(false);
        Assert.DoesNotContain("[dbo].[sysdiagrams]", seedScriptSql, StringComparison.Ordinal);

        ReplayHostFactory replayHostFactory = new();
        var replayHost = await replayHostFactory
            .CreateAsync(
                new ReplayLaunchOptions
                {
                    DacpacPath = dacpacPath,
                    SeedSqlPath = seedScriptPath,
                })
            .ConfigureAwait(false);

        await using (replayHost.ConfigureAwait(false))
        {
            var seedArtifact = Assert.IsType<SqlServerSeedSqlArtifact>(replayHost.Bootstrap.SqlServerSeedSql);
            Assert.Equal(Path.GetFullPath(seedScriptPath), seedArtifact.SourcePath);
            Assert.Equal(Path.GetFileName(seedScriptPath), seedArtifact.FileName);

            var configuration = replayHost.Services.GetRequiredService<IConfiguration>();
            var connectionString = configuration[DefaultConnectionKey]
                ?? throw new InvalidOperationException("Missing sample replay application connection string.");
            var restoredProductCount = await CountProductsAsync(connectionString).ConfigureAwait(false);
            Assert.True(restoredProductCount > 0, "Expected the committed AdventureWorks seed script to restore SalesLT.Product rows.");

            var products = await ReadProductsByCategoryAsync(replayHost).ConfigureAwait(false);
            Assert.All(
                products,
                product => Assert.True(
                    product.ListPrice >= CatalogScenario.ReplayMinPrice,
                    $"Expected all list prices to be >= {CatalogScenario.ReplayMinPrice}, but found {product.ListPrice}."));
        }
    }

    private static void AssertCommandSucceeded(
        string stageName,
        CommandResult result)
    {
        Assert.True(
            result.ExitCode == 0,
            $"PowerShell {stageName} failed.{Environment.NewLine}ExitCode: {result.ExitCode}{Environment.NewLine}StdOut:{Environment.NewLine}{result.StandardOutput}{Environment.NewLine}StdErr:{Environment.NewLine}{result.StandardError}");
    }

    private static void AssertHotSeededProducts(
        IReadOnlyList<SqloomTestApp.ProductResponse> products)
    {
        Assert.True(products.Count > 300, "Expected the hot seeded product query to return a large filtered result set.");
        Assert.All(
            products,
            product => Assert.StartsWith("HOT-", product.ProductNumber, StringComparison.Ordinal));
        Assert.All(
            products,
            product => Assert.True(
                product.ListPrice >= CatalogScenario.ReplayMinPrice,
                $"Expected all list prices to be >= {CatalogScenario.ReplayMinPrice}, but found {product.ListPrice}."));
    }

    private static string CreateTempDir()
    {
        var directoryPath = Path.Combine(
            Path.GetTempPath(),
            "sqloom-tests",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directoryPath);
        return directoryPath;
    }

    private static void DeleteDirectoryIfExists(string path)
    {
        if (Directory.Exists(path))
        {
            Directory.Delete(
                path,
                recursive: true);
        }
    }

    private static async Task<IReadOnlyList<SqloomTestApp.ProductResponse>> ReadProductsByCategoryAsync(
        IReplayHost replayHost)
    {
        using var response = await replayHost.Client
            .GetAsync(CatalogScenario.CreateRequestPath())
            .ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var products = await response.Content
            .ReadFromJsonAsync<List<SqloomTestApp.ProductResponse>>()
            .ConfigureAwait(false);
        return Assert.IsType<List<SqloomTestApp.ProductResponse>>(products);
    }

    private static async Task<int> CountProductsAsync(
        string connectionString,
        CancellationToken cancellationToken = default)
    {
        SqlConnection connection = new(connectionString);
        await using (connection.ConfigureAwait(false))
        {
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
            var command = connection.CreateCommand();
            await using (command.ConfigureAwait(false))
            {
                command.CommandText = "SELECT COUNT(*) FROM [SalesLT].[Product];";
                var scalar = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
                return Convert.ToInt32(scalar, System.Globalization.CultureInfo.InvariantCulture);
            }
        }
    }

    private static async Task<CommandResult> RunPowerShellAsync(
        string workingDirectory,
        IReadOnlyList<string> arguments,
        CancellationToken cancellationToken = default)
    {
        ProcessStartInfo startInfo = new("pwsh")
        {
            CreateNoWindow = true,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false,
            WorkingDirectory = workingDirectory,
        };

        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using Process process = new()
        {
            StartInfo = startInfo,
        };

        if (!process.Start())
        {
            throw new InvalidOperationException("Failed to start PowerShell.");
        }

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromMinutes(5));

        var standardOutputTask = process.StandardOutput.ReadToEndAsync(timeout.Token);
        var standardErrorTask = process.StandardError.ReadToEndAsync(timeout.Token);

        try
        {
            await process.WaitForExitAsync(timeout.Token).ConfigureAwait(false);
            await Task.WhenAll(
                    standardOutputTask,
                    standardErrorTask)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }

            throw new Xunit.Sdk.XunitException("PowerShell timed out while exporting the AdventureWorks seed script.");
        }

        return new CommandResult(
            process.ExitCode,
            standardOutputTask.Result,
            standardErrorTask.Result);
    }

    private sealed record CommandResult(
        int ExitCode,
        string StandardOutput,
        string StandardError);
}
