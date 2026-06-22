using System;
using System.IO;
using Sqloom.Core.Execution;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Sqloom.TestApp.Harness;
using Sqloom.Testing;
using Xunit;

namespace Sqloom.Host.Tests;

/// <summary>
/// Exercises the Sqloom host runtime in a console-oriented integration path.
/// </summary>
[Collection("ConsoleHostRuntime")]
public sealed class HostRuntimeTests
{
    [RequiresDockerFact]
    [Trait("Category", "Integration")]
    public async Task RunAsync_WithExplicitReplayProjectWithoutBuild_ReplaysProductCatalogWorkload()
    {
        var projectPath = SqloomTestAppPaths.GetProjectPath();
        var currentDirectory = Directory.GetCurrentDirectory();

        var result = await CaptureConsoleAsync(static async state =>
        {
            return await HostRuntime
                .RunAsync(
                    [
                        "replay",
                        state.ProjectPath,
                        "--dotnet-command",
                        "dotnet",
                        "--no-build",
                        "--target",
                        TestAppProductCatalogScenario.OperationKey,
                    ],
                    state.CurrentDirectory)
                .ConfigureAwait(false);
        }, (ProjectPath: projectPath, CurrentDirectory: currentDirectory));

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("Replay summary:", result.StdOut, StringComparison.Ordinal);
        Assert.Contains("App: Sqloom Test App", result.StdOut, StringComparison.Ordinal);
        Assert.Contains(
            $"{TestAppProductCatalogScenario.OperationKey}: status=replayed, http=200",
            result.StdOut,
            StringComparison.Ordinal);
    }

    [RequiresDockerFact]
    [Trait("Category", "Integration")]
    public async Task RunAsync_WithReplayDebug_PrintsStageDiagnosticsToStandardError()
    {
        var projectPath = SqloomTestAppPaths.GetProjectPath();
        var currentDirectory = Directory.GetCurrentDirectory();

        var result = await CaptureConsoleAsync(static async state =>
        {
            return await HostRuntime
                .RunAsync(
                    [
                        "replay",
                        state.ProjectPath,
                        "--debug",
                        "--dotnet-command",
                        "dotnet",
                        "--no-build",
                        "--target",
                        TestAppProductCatalogScenario.OperationKey,
                    ],
                    state.CurrentDirectory)
                .ConfigureAwait(false);
        }, (ProjectPath: projectPath, CurrentDirectory: currentDirectory));

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("Replay summary:", result.StdOut, StringComparison.Ordinal);
        Assert.Contains("[sqloom debug] [replay] resolved inputs", result.StdErr, StringComparison.Ordinal);
        Assert.Contains(
            $"target_filter={TestAppProductCatalogScenario.OperationKey}",
            result.StdErr,
            StringComparison.Ordinal);
    }

    [RequiresDockerFact]
    [Trait("Category", "Integration")]
    public async Task RunAsync_WithExplicitSqlServerDacpacFile_ReplaysProductCatalogWorkloadAndPrintsBootstrap()
    {
        var projectPath = SqloomTestAppPaths.GetProjectPath();
        var dacpacPath = SqloomTestAppPaths.GetSqlServerDacpacPath();
        var currentDirectory = Directory.GetCurrentDirectory();

        var result = await CaptureConsoleAsync(static async state =>
        {
            return await HostRuntime
                .RunAsync(
                    [
                        "replay",
                        state.ProjectPath,
                        "--dotnet-command",
                        "dotnet",
                        "--no-build",
                        "--sqlserver-dacpac-file",
                        state.DacpacPath,
                        "--target",
                        TestAppProductCatalogScenario.OperationKey,
                    ],
                    state.CurrentDirectory)
                .ConfigureAwait(false);
        }, (ProjectPath: projectPath, DacpacPath: dacpacPath, CurrentDirectory: currentDirectory))
            .ConfigureAwait(false);

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("Replay summary:", result.StdOut, StringComparison.Ordinal);
        Assert.Contains("App: Sqloom Test App", result.StdOut, StringComparison.Ordinal);
        Assert.Contains("SQL Server DACPAC: AdventureWorksLT2025.dacpac", result.StdOut, StringComparison.Ordinal);
        Assert.Contains($"DACPAC path: {dacpacPath}", result.StdOut, StringComparison.Ordinal);
        Assert.Contains(
            $"{TestAppProductCatalogScenario.OperationKey}: status=replayed, http=200",
            result.StdOut,
            StringComparison.Ordinal);
    }

    [RequiresDockerFact]
    [Trait("Category", "Integration")]
    public async Task RunAsync_WithExplicitSqlSeedScript_ReplaysProductCatalogWorkloadAndPrintsSeedBootstrap()
    {
        var projectPath = SqloomTestAppPaths.GetProjectPath();
        var dacpacPath = SqloomTestAppPaths.GetSqlServerDacpacPath();
        var currentDirectory = Directory.GetCurrentDirectory();
        var tempDirectory = CreateTempDirectory();
        var seedScriptPath = Path.Combine(tempDirectory, "AdventureWorksLT2025.seed.sql");
        File.WriteAllText(
            seedScriptPath,
            SqloomTestAppSeedScripts.CreateCustomProductCatalogSeedScript());

        try
        {
            var result = await CaptureConsoleAsync(static async state =>
            {
                return await HostRuntime
                    .RunAsync(
                        [
                            "replay",
                            state.ProjectPath,
                            "--dotnet-command",
                            "dotnet",
                            "--no-build",
                            "--sqlserver-dacpac-file",
                            state.DacpacPath,
                            "--sqlserver-seed-sql-file",
                            state.SeedScriptPath,
                            "--target",
                            TestAppProductCatalogScenario.OperationKey,
                        ],
                        state.CurrentDirectory)
                    .ConfigureAwait(false);
            }, (ProjectPath: projectPath, DacpacPath: dacpacPath, SeedScriptPath: seedScriptPath, CurrentDirectory: currentDirectory))
                .ConfigureAwait(false);

            Assert.Equal(0, result.ExitCode);
            Assert.Contains("Replay summary:", result.StdOut, StringComparison.Ordinal);
            Assert.Contains("App: Sqloom Test App", result.StdOut, StringComparison.Ordinal);
            Assert.Contains("SQL Server DACPAC: AdventureWorksLT2025.dacpac", result.StdOut, StringComparison.Ordinal);
            Assert.Contains("SQL seed script: AdventureWorksLT2025.seed.sql", result.StdOut, StringComparison.Ordinal);
            Assert.Contains($"Seed script path: {seedScriptPath}", result.StdOut, StringComparison.Ordinal);
            Assert.Contains(
                $"{TestAppProductCatalogScenario.OperationKey}: status=replayed, http=200",
                result.StdOut,
                StringComparison.Ordinal);
        }
        finally
        {
            if (Directory.Exists(tempDirectory))
            {
                Directory.Delete(tempDirectory, recursive: true);
            }
        }
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task RunAsync_WithObserveWithoutConnectionStringSwitch_StillRequiresExplicitConnectionStringSwitch()
    {
        var currentDirectory = Directory.GetCurrentDirectory();

        var result = await CaptureConsoleAsync(static async state =>
        {
            return await HostRuntime
                .RunAsync(
                    new TestAppApplication(),
                    [
                        "observe",
                    ],
                    state)
                .ConfigureAwait(false);
        }, currentDirectory);

        Assert.Equal(1, result.ExitCode);
        Assert.Contains(
            "Query Store capture requires --read-only-connection-string.",
            result.StdErr,
            StringComparison.Ordinal);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task RunAsync_WithTuneWithoutConnectionStringSwitch_StillRequiresExplicitConnectionStringSwitch()
    {
        var currentDirectory = Directory.GetCurrentDirectory();
        var schemaPath = Path.Combine(CreateTempDirectory(), "schema.sql");
        File.WriteAllText(
            schemaPath,
            """
            CREATE TABLE [dbo].[Product] (
                [Id] INT NOT NULL
            );
            GO
            """);

        var result = await CaptureConsoleAsync(static async state =>
        {
            return await HostRuntime
                .RunAsync(
                    new NoConnectionTestApplication(state.SchemaPath),
                    [
                        "tune",
                        "--model-provider",
                        "openai",
                        "--openai-api-key",
                        "openai-key",
                        "--sqlserver-schema-file",
                        state.SchemaPath,
                    ],
                    state.CurrentDirectory)
                .ConfigureAwait(false);
        }, (CurrentDirectory: currentDirectory, SchemaPath: schemaPath));

        Assert.Equal(1, result.ExitCode);
        Assert.Contains(
            "Sqloom tune requires --read-only-connection-string or a read-only connection string from the harness session.",
            result.StdErr,
            StringComparison.Ordinal);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task RunAsync_WithOpenAIAdviceWithoutApiKey_StillRequiresExplicitApiKey()
    {
        var currentDirectory = Directory.GetCurrentDirectory();
        var replayArtifactDirectory = CreateTempDirectory();
        var schemaPath = Path.Combine(replayArtifactDirectory, "schema.sql");
        File.WriteAllText(
            Path.Combine(replayArtifactDirectory, "query-store-correlation.json"),
            "{}");
        File.WriteAllText(
            schemaPath,
            """
            CREATE TABLE [dbo].[ExpenseRecord] (
                [Id] INT NOT NULL
            );
            GO
            """);

        try
        {
            var result = await CaptureConsoleAsync(static async state =>
            {
                return await HostRuntime
                    .RunAsync(
                        [
                            "advise",
                            "--replay-artifact-dir",
                            state.ReplayArtifactDirectory,
                            "--model-provider",
                            "openai",
                            "--sqlserver-schema-file",
                            state.SchemaPath,
                        ],
                        state.CurrentDirectory)
                    .ConfigureAwait(false);
            }, (ReplayArtifactDirectory: replayArtifactDirectory, SchemaPath: schemaPath, CurrentDirectory: currentDirectory));

            Assert.Equal(1, result.ExitCode);
            Assert.Contains(
                "Sqloom advice with --model-provider openai requires --openai-api-key.",
                result.StdErr,
                StringComparison.Ordinal);
        }
        finally
        {
            if (Directory.Exists(replayArtifactDirectory))
            {
                Directory.Delete(
                    replayArtifactDirectory,
                    recursive: true);
            }
        }
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task RunAsync_WithOpenAIAdviceWithoutSchemaFile_StillRequiresExplicitSchemaFile()
    {
        var currentDirectory = Directory.GetCurrentDirectory();
        var replayArtifactDirectory = CreateTempDirectory();
        File.WriteAllText(
            Path.Combine(replayArtifactDirectory, "query-store-correlation.json"),
            "{}");

        try
        {
            var result = await CaptureConsoleAsync(static async state =>
            {
                return await HostRuntime
                    .RunAsync(
                        [
                            "advise",
                            "--replay-artifact-dir",
                            state.ReplayArtifactDirectory,
                            "--model-provider",
                            "openai",
                            "--openai-api-key",
                            "openai-key",
                        ],
                        state.CurrentDirectory)
                    .ConfigureAwait(false);
            }, (ReplayArtifactDirectory: replayArtifactDirectory, CurrentDirectory: currentDirectory));

            Assert.Equal(1, result.ExitCode);
            Assert.Contains(
                "--sqlserver-schema-file",
                result.StdErr,
                StringComparison.Ordinal);
        }
        finally
        {
            if (Directory.Exists(replayArtifactDirectory))
            {
                Directory.Delete(
                    replayArtifactDirectory,
                    recursive: true);
            }
        }
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task RunAsync_WithTuneWithoutSchemaFile_StillRequiresExplicitSchemaFile()
    {
        var currentDirectory = Directory.GetCurrentDirectory();

        var result = await CaptureConsoleAsync(static async state =>
        {
            return await HostRuntime
                .RunAsync(
                    new NoSchemaTestApplication(),
                    [
                        "tune",
                        "--read-only-connection-string",
                        "Server=localhost;Database=Sqloom;Trusted_Connection=True;",
                        "--model-provider",
                        "openai",
                        "--openai-api-key",
                        "openai-key",
                    ],
                    state)
                .ConfigureAwait(false);
        }, currentDirectory);

        Assert.Equal(1, result.ExitCode);
        Assert.Contains(
            "--sqlserver-schema-file",
            result.StdErr,
            StringComparison.Ordinal);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task RunAsync_WithoutAppSelection_PrintsNoCommandHint()
    {
        var currentDirectory = Directory.GetCurrentDirectory();

        var result = await CaptureConsoleAsync(static async state =>
        {
            return await HostRuntime
                .RunAsync(
                    [],
                    state)
                .ConfigureAwait(false);
        }, currentDirectory);

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("Use --help to print the available host arguments.", result.StdOut, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task RunAsync_WithVersion_PrintsToolVersion()
    {
        var currentDirectory = Directory.GetCurrentDirectory();
        var expectedVersion = typeof(HostRuntime)
            .Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion
            ?? typeof(HostRuntime).Assembly.GetName().Version?.ToString()
            ?? "unknown";
        var buildMetadataIndex = expectedVersion.IndexOf('+', StringComparison.Ordinal);
        if (buildMetadataIndex >= 0)
        {
            expectedVersion = expectedVersion[..buildMetadataIndex];
        }

        var result = await CaptureConsoleAsync(static async state =>
        {
            return await HostRuntime
                .RunAsync(
                    [
                        "--version",
                    ],
                    state)
                .ConfigureAwait(false);
        }, currentDirectory);

        Assert.Equal(0, result.ExitCode);
        Assert.Contains(
            $"sqloom {expectedVersion}",
            result.StdOut,
            StringComparison.Ordinal);
        Assert.Equal(string.Empty, result.StdErr);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task RunAsync_WithBoundAppIntegration_RejectsExplicitTargetPathSelection()
    {
        var projectPath = SqloomTestAppPaths.GetProjectPath();
        var currentDirectory = Directory.GetCurrentDirectory();

        var result = await CaptureConsoleAsync(static async state =>
        {
            return await HostRuntime
                .RunAsync(
                    new TestAppApplication(),
                    [
                        "replay",
                        state.ProjectPath,
                    ],
                    state.CurrentDirectory)
                .ConfigureAwait(false);
        }, (ProjectPath: projectPath, CurrentDirectory: currentDirectory));

        Assert.Equal(1, result.ExitCode);
        Assert.Contains("already provides its harness", result.StdErr, StringComparison.OrdinalIgnoreCase);
    }

    private static async Task<ConsoleCaptureResult> CaptureConsoleAsync<TState>(
        Func<TState, Task<int>> action,
        TState state)
    {
        await ConsoleHostRuntimeCollection.Gate.WaitAsync().ConfigureAwait(false);
        var originalOut = Console.Out;
        var originalError = Console.Error;
        using var stdOut = new StringWriter();
        using var stdErr = new StringWriter();

        try
        {
            Console.SetOut(stdOut);
            Console.SetError(stdErr);
            var exitCode = await action(state).ConfigureAwait(false);
            return new ConsoleCaptureResult(exitCode, stdOut.ToString(), stdErr.ToString());
        }
        finally
        {
            Console.SetOut(originalOut);
            Console.SetError(originalError);
            ConsoleHostRuntimeCollection.Gate.Release();
        }
    }

    private sealed record ConsoleCaptureResult(
        int ExitCode,
        string StdOut,
        string StdErr);

    private static string CreateTempDirectory()
    {
        var directoryPath = Path.Combine(
            Path.GetTempPath(),
            "sqloom-tests",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directoryPath);
        return directoryPath;
    }

}

/// <summary>
/// Defines the xUnit collection for console host runtime integration tests.
/// </summary>
[CollectionDefinition("ConsoleHostRuntime", DisableParallelization = true)]
public sealed class ConsoleHostRuntimeCollection
{
    internal static SemaphoreSlim Gate { get; } = new(1, 1);
}

/// <summary>
/// Supplies a harness manifest without a session connection string for validation tests.
/// </summary>
internal sealed class NoConnectionTestApplication : ISqloomApplication
{
    private readonly string _schemaPath;

    public NoConnectionTestApplication(string schemaPath)
    {
        _schemaPath = schemaPath;
    }

    public SqloomApplicationManifest Describe(SqloomApplicationContext context)
    {
        return new SqloomApplicationManifest
        {
            Name = "No Connection Test App",
            ReplayProfile = HostRuntimeTestHarnessProfiles.CreateReplayProfile(),
            SqlServerSchemaPath = _schemaPath,
        };
    }

    public ValueTask<ISqloomApplicationSession> StartAsync(
        SqloomApplicationContext context,
        CancellationToken cancellationToken = default)
    {
        return ValueTask.FromResult<ISqloomApplicationSession>(new NoConnectionSession());
    }
}

/// <summary>
/// Supplies a harness manifest without a default schema for validation tests.
/// </summary>
internal sealed class NoSchemaTestApplication : ISqloomApplication
{
    public SqloomApplicationManifest Describe(SqloomApplicationContext context)
    {
        return new SqloomApplicationManifest
        {
            Name = "No Schema Test App",
            ReplayProfile = HostRuntimeTestHarnessProfiles.CreateReplayProfile(),
        };
    }

    public ValueTask<ISqloomApplicationSession> StartAsync(
        SqloomApplicationContext context,
        CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException("Schema validation should run before the harness session starts.");
    }
}

internal sealed class NoConnectionSession : ISqloomApplicationSession
{
    public IReplayHost ReplayHost =>
        throw new NotSupportedException("Replay should not start when the harness does not supply a connection string.");

    public string? ReadOnlyConnectionString => null;

    public ReplayBootstrapReport Bootstrap { get; } = new();

    public ValueTask DisposeAsync()
    {
        return ValueTask.CompletedTask;
    }
}

internal static class HostRuntimeTestHarnessProfiles
{
    public static ReplayProfile CreateReplayProfile()
    {
        return new ReplayProfile
        {
            DefaultOpenApiDocumentPath = "openapi.json",
        };
    }
}
