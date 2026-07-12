using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Sqloom.Core.Execution;
using Sqloom.Testing;
using Xunit;

namespace Sqloom.Host.Tests;

/// <summary>
/// Exercises Sqloom tune command argument resolution before the workflow starts.
/// </summary>
public sealed class TuneCommandTests
{
    [Fact]
    public async Task ResolveReplayLaunchOptionsAsync_ExportsDacpacFromCommandLineReadOnlyConnection()
    {
        var currentDirectory = CreateTempDir();
        var replayArtifactDirectory = Path.Combine(currentDirectory, "replay");
        var exportedDacpacPath = Path.Combine(replayArtifactDirectory, "sqlserver-schema-source.dacpac");
        const string readOnlyConnectionString =
            "Server=localhost;Database=Sqloom;Trusted_Connection=True;";
        string? capturedConnectionString = null;
        string? capturedReplayArtifactDirectory = null;
        var command = CreateCommand(
            new FakeSqlServerDacpacExporter(
                exportedDacpacPath,
                (connectionString, artifactDirectory) =>
                {
                    capturedConnectionString = connectionString;
                    capturedReplayArtifactDirectory = artifactDirectory;
                }));

        var launchOptions = await command.ResolveReplayLaunchOptionsAsync(
            new ReplayLaunchOptions(),
            ManifestFactory.CreateManifest(),
            currentDirectory,
            replayArtifactDirectory,
            readOnlyConnectionString);

        Assert.Equal(exportedDacpacPath, launchOptions.DacpacPath, StringComparer.OrdinalIgnoreCase);
        Assert.Null(launchOptions.SeedSqlPath);
        Assert.Equal(readOnlyConnectionString, capturedConnectionString);
        Assert.Equal(replayArtifactDirectory, capturedReplayArtifactDirectory, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ResolveReplayLaunchOptionsAsync_PreservesSeedSqlWithExportedDacpac()
    {
        var currentDirectory = CreateTempDir();
        var replayArtifactDirectory = Path.Combine(currentDirectory, "replay");
        var seedSqlPath = Path.Combine(currentDirectory, "seed.sql");
        var exportedDacpacPath = Path.Combine(replayArtifactDirectory, "sqlserver-schema-source.dacpac");
        File.WriteAllText(seedSqlPath, "SELECT 1;");
        var command = CreateCommand(new FakeSqlServerDacpacExporter(exportedDacpacPath));

        var launchOptions = await command.ResolveReplayLaunchOptionsAsync(
            new ReplayLaunchOptions
            {
                SeedSqlPath = seedSqlPath,
            },
            ManifestFactory.CreateManifest(),
            currentDirectory,
            replayArtifactDirectory,
            "Server=localhost;Database=Sqloom;Trusted_Connection=True;");

        Assert.Equal(exportedDacpacPath, launchOptions.DacpacPath, StringComparer.OrdinalIgnoreCase);
        Assert.Equal(seedSqlPath, launchOptions.SeedSqlPath, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ResolveReplayLaunchOptionsAsync_PrefersManifestDacpacOverExport()
    {
        var currentDirectory = CreateTempDir();
        var manifestDacpacPath = Path.Combine(currentDirectory, "manifest.dacpac");
        File.WriteAllText(manifestDacpacPath, "sqloom");
        var exportCount = 0;
        var command = CreateCommand(
            new FakeSqlServerDacpacExporter(
                Path.Combine(currentDirectory, "exported.dacpac"),
                (_, _) => exportCount++));
        var manifest = new SqloomApplicationManifest
        {
            Name = "Sqloom Test Harness",
            OpenApiPath = Sqloom.Tests.RepositoryPaths.GetTestAppOpenApiPath(),
            ReplayProfile = ManifestFactory.CreateReplayProfile(),
            SqlServerDacpacPath = manifestDacpacPath,
        };

        var launchOptions = await command.ResolveReplayLaunchOptionsAsync(
            new ReplayLaunchOptions(),
            manifest,
            currentDirectory,
            Path.Combine(currentDirectory, "replay"),
            "Server=localhost;Database=Sqloom;Trusted_Connection=True;");

        Assert.Equal(manifestDacpacPath, launchOptions.DacpacPath, StringComparer.OrdinalIgnoreCase);
        Assert.Equal(0, exportCount);
    }

    [Fact]
    public async Task ResolveReplayLaunchOptionsAsync_PrefersCommandLineDacpacOverManifestAndExport()
    {
        var currentDirectory = CreateTempDir();
        var commandLineDacpacPath = Path.Combine(currentDirectory, "command-line.dacpac");
        var manifestDacpacPath = Path.Combine(currentDirectory, "manifest.dacpac");
        File.WriteAllText(commandLineDacpacPath, "sqloom");
        File.WriteAllText(manifestDacpacPath, "sqloom");
        var exportCount = 0;
        var command = CreateCommand(
            new FakeSqlServerDacpacExporter(
                Path.Combine(currentDirectory, "exported.dacpac"),
                (_, _) => exportCount++));
        var manifest = new SqloomApplicationManifest
        {
            Name = "Sqloom Test Harness",
            OpenApiPath = Sqloom.Tests.RepositoryPaths.GetTestAppOpenApiPath(),
            ReplayProfile = ManifestFactory.CreateReplayProfile(),
            SqlServerDacpacPath = manifestDacpacPath,
        };

        var launchOptions = await command.ResolveReplayLaunchOptionsAsync(
            new ReplayLaunchOptions
            {
                DacpacPath = commandLineDacpacPath,
            },
            manifest,
            currentDirectory,
            Path.Combine(currentDirectory, "replay"),
            "Server=localhost;Database=Sqloom;Trusted_Connection=True;");

        Assert.Equal(commandLineDacpacPath, launchOptions.DacpacPath, StringComparer.OrdinalIgnoreCase);
        Assert.Equal(0, exportCount);
    }

    [Fact]
    public async Task ResolveReplayLaunchOptionsAsync_RejectsSeedSqlWithoutEffectiveDacpac()
    {
        var currentDirectory = CreateTempDir();
        var seedSqlPath = Path.Combine(currentDirectory, "seed.sql");
        File.WriteAllText(seedSqlPath, "SELECT 1;");
        var command = CreateCommand(new FakeSqlServerDacpacExporter(Path.Combine(currentDirectory, "unused.dacpac")));

        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => command.ResolveReplayLaunchOptionsAsync(
                new ReplayLaunchOptions
                {
                    SeedSqlPath = seedSqlPath,
                },
                ManifestFactory.CreateManifest(),
                currentDirectory,
                Path.Combine(currentDirectory, "replay"),
                commandLineReadOnlyConnectionString: null));

        Assert.Contains("--sqlserver-dacpac-file", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("--read-only-connection-string", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static TuneCommand CreateCommand(ISqlServerDacpacExporter dacpacExporter)
    {
        return new TuneCommand(
            new TuneArgumentParser(),
            new TuneWorkflowRunner(),
            dacpacExporter);
    }

    private static string CreateTempDir()
    {
        var directory = Path.Combine(
            Path.GetTempPath(),
            "sqloom-tune-command-tests",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        return directory;
    }

    private sealed class FakeSqlServerDacpacExporter(
        string exportedDacpacPath,
        Action<string, string>? captureExport = null) : ISqlServerDacpacExporter
    {
        public Task<string> ExportAsync(
            string readOnlyConnectionString,
            string replayArtifactDirectory,
            CancellationToken cancellationToken = default)
        {
            captureExport?.Invoke(readOnlyConnectionString, replayArtifactDirectory);
            return Task.FromResult(exportedDacpacPath);
        }
    }
}
