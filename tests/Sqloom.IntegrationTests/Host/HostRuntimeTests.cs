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
    public async Task WithReplayProjectWithoutBuild_ReportsMissingQueryData()
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
                        CatalogScenario.OperationKey,
                    ],
                    state.CurrentDirectory)
                .ConfigureAwait(false);
        }, (ProjectPath: projectPath, CurrentDirectory: currentDirectory));

        AssertReplayRequiresPreparedQueryData(result);
    }

    [RequiresDockerFact]
    [Trait("Category", "Integration")]
    public async Task WithReplayDebug_ReportsMissingQueryData()
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
                        CatalogScenario.OperationKey,
                    ],
                    state.CurrentDirectory)
                .ConfigureAwait(false);
        }, (ProjectPath: projectPath, CurrentDirectory: currentDirectory));

        AssertReplayRequiresPreparedQueryData(result);
        Assert.Contains("[sqloom debug] [replay] resolved inputs", result.StdErr, StringComparison.Ordinal);
        Assert.Contains(
            $"target_filter={CatalogScenario.OperationKey}",
            result.StdErr,
            StringComparison.Ordinal);
    }

    [RequiresDockerFact]
    [Trait("Category", "Integration")]
    public async Task WithSqlServerDacpacFile_ReportsMissingQueryData()
    {
        var projectPath = SqloomTestAppPaths.GetProjectPath();
        var dacpacPath = SqloomTestAppPaths.GetDacpacPath();
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
                        CatalogScenario.OperationKey,
                    ],
                    state.CurrentDirectory)
                .ConfigureAwait(false);
        }, (ProjectPath: projectPath, DacpacPath: dacpacPath, CurrentDirectory: currentDirectory))
            .ConfigureAwait(false);

        AssertReplayRequiresPreparedQueryData(result);
    }

    [RequiresDockerFact]
    [Trait("Category", "Integration")]
    public async Task WithSqlSeedScript_ReportsMissingQueryData()
    {
        var projectPath = SqloomTestAppPaths.GetProjectPath();
        var dacpacPath = SqloomTestAppPaths.GetDacpacPath();
        var currentDirectory = Directory.GetCurrentDirectory();
        var tempDirectory = CreateTempDir();
        var seedScriptPath = Path.Combine(tempDirectory, "AdventureWorksLT2025.seed.sql");
        File.WriteAllText(
            seedScriptPath,
            SqloomTestAppSeedScripts.CreateCustomSeedScript());

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
                            CatalogScenario.OperationKey,
                        ],
                        state.CurrentDirectory)
                    .ConfigureAwait(false);
            }, (ProjectPath: projectPath, DacpacPath: dacpacPath, SeedScriptPath: seedScriptPath, CurrentDirectory: currentDirectory))
                .ConfigureAwait(false);

            AssertReplayRequiresPreparedQueryData(result);
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
    public async Task ObserveWithoutConnectionString_RequiresExplicitSwitch()
    {
        var currentDirectory = Directory.GetCurrentDirectory();

        var result = await CaptureConsoleAsync(static async state =>
        {
            return await HostRuntime
                .RunAsync(
                    new SampleApplication(),
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
    public async Task TuneWithoutConnectionString_RequiresExplicitSwitch()
    {
        var currentDirectory = Directory.GetCurrentDirectory();
        var dacpacPath = Path.Combine(CreateTempDir(), "schema-source.dacpac");
        File.WriteAllText(
            dacpacPath,
            "sqloom");

        var result = await CaptureConsoleAsync(static async state =>
        {
            return await HostRuntime
                .RunAsync(
                    new NoConnectionTestApplication(state.DacpacPath),
                    [
                        "tune",
                        "--model-provider",
                        "openai",
                        "--openai-api-key",
                        "openai-key",
                    ],
                    state.CurrentDirectory)
                .ConfigureAwait(false);
        }, (CurrentDirectory: currentDirectory, DacpacPath: dacpacPath));

        Assert.Equal(1, result.ExitCode);
        Assert.Contains(
            "Sqloom tune requires --read-only-connection-string or a read-only connection string from the harness session.",
            result.StdErr,
            StringComparison.Ordinal);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task OpenAIAdviceWithoutApiKey_RequiresExplicitApiKey()
    {
        var currentDirectory = Directory.GetCurrentDirectory();
        var replayArtifactDirectory = CreateTempDir();
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
                            state.ReplayArtifactDir,
                            "--model-provider",
                            "openai",
                            "--sqlserver-schema-file",
                            state.SchemaPath,
                        ],
                        state.CurrentDirectory)
                    .ConfigureAwait(false);
            }, (ReplayArtifactDir: replayArtifactDirectory, SchemaPath: schemaPath, CurrentDirectory: currentDirectory));

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
    public async Task OpenAIAdviceWithoutSchemaSource_RequiresSchemaSource()
    {
        var currentDirectory = Directory.GetCurrentDirectory();
        var replayArtifactDirectory = CreateTempDir();
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
                            state.ReplayArtifactDir,
                            "--model-provider",
                            "openai",
                            "--openai-api-key",
                            "openai-key",
                        ],
                        state.CurrentDirectory)
                    .ConfigureAwait(false);
            }, (ReplayArtifactDir: replayArtifactDirectory, CurrentDirectory: currentDirectory));

            Assert.Equal(1, result.ExitCode);
            Assert.Contains(
                "--sqlserver-schema-file",
                result.StdErr,
                StringComparison.Ordinal);
            Assert.Contains(
                "DACPAC",
                result.StdErr,
                StringComparison.OrdinalIgnoreCase);
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
    public async Task WithTuneWithoutSchemaSource_DefersSchemaResolutionUntilSessionConnection()
    {
        var currentDirectory = Directory.GetCurrentDirectory();

        var exception = await Assert.ThrowsAsync<NotSupportedException>(
            async () => await HostRuntime
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
                    currentDirectory)
                .ConfigureAwait(false));

        Assert.Contains(
            "Schema resolution should wait for the harness session read-only connection.",
            exception.Message,
            StringComparison.Ordinal);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task WithoutAppSelection_PrintsNoCommandHint()
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
    public async Task WithHelp_PrintsInitUsage()
    {
        var currentDirectory = Directory.GetCurrentDirectory();

        var result = await CaptureConsoleAsync(static async state =>
        {
            return await HostRuntime
                .RunAsync(
                    [
                        "--help",
                    ],
                    state)
                .ConfigureAwait(false);
        }, currentDirectory);

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("init [--agent codex|claude|copilot|all] [--overwrite]", result.StdOut, StringComparison.Ordinal);
        Assert.Equal(string.Empty, result.StdErr);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task WithInit_ScaffoldsSkillWithoutHarnessTarget()
    {
        var currentDirectory = CreateTempDir();
        Directory.CreateDirectory(Path.Combine(currentDirectory, ".git"));

        try
        {
            var result = await CaptureConsoleAsync(static async state =>
            {
                return await HostRuntime
                    .RunAsync(
                        [
                            "init",
                            "--agent",
                            "copilot",
                        ],
                        state)
                    .ConfigureAwait(false);
            }, currentDirectory);

            Assert.Equal(0, result.ExitCode);
            Assert.Contains(".github/skills/sqloom-harness/SKILL.md", result.StdOut, StringComparison.Ordinal);
            Assert.Equal(string.Empty, result.StdErr);
            Assert.True(File.Exists(Path.Combine(currentDirectory, ".github", "skills", "sqloom-harness", "SKILL.md")));
        }
        finally
        {
            Directory.Delete(currentDirectory, recursive: true);
        }
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task WithVersion_PrintsToolVersion()
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
    public async Task WithBoundAppIntegration_RejectsExplicitTargetPathSelection()
    {
        var projectPath = SqloomTestAppPaths.GetProjectPath();
        var currentDirectory = Directory.GetCurrentDirectory();

        var result = await CaptureConsoleAsync(static async state =>
        {
            return await HostRuntime
                .RunAsync(
                    new SampleApplication(),
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

    private static void AssertReplayRequiresPreparedQueryData(ConsoleCaptureResult result)
    {
        Assert.Equal(1, result.ExitCode);
        var output = result.StdOut + result.StdErr;
        Assert.Contains(
            "missing required query parameter 'categoryId'",
            output,
            StringComparison.Ordinal);
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
    private readonly string _dacpacPath;

    public NoConnectionTestApplication(string dacpacPath)
    {
        _dacpacPath = dacpacPath;
    }

    public SqloomApplicationManifest Describe(SqloomApplicationContext context)
    {
        return new SqloomApplicationManifest
        {
            Name = "No Connection Test App",
            OpenApiPath = SqloomTestAppPaths.GetOpenApiPath(),
            ReplayProfile = HostRuntimeTestHarnessProfiles.CreateReplayProfile(),
            SqlServerDacpacPath = _dacpacPath,
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
            OpenApiPath = SqloomTestAppPaths.GetOpenApiPath(),
            ReplayProfile = HostRuntimeTestHarnessProfiles.CreateReplayProfile(),
        };
    }

    public ValueTask<ISqloomApplicationSession> StartAsync(
        SqloomApplicationContext context,
        CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException("Schema resolution should wait for the harness session read-only connection.");
    }
}

internal sealed class NoConnectionSession : ISqloomApplicationSession
{
    public IReplayHost ReplayHost =>
        throw new NotSupportedException("Replay should not start when the harness does not supply a connection string.");

    public string? ReadOnlyConnection => null;

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
        return new ReplayProfile();
    }
}
