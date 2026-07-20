using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Sqloom.Host.Replay;
using Sqloom.Pipeline.Execution;
using Sqloom.Tests;
using Xunit;

namespace Sqloom.Host.Tests;

/// <summary>
/// Exercises endpoint catalog persistence.
/// </summary>
public sealed class EndpointsCommandTests
{
    [Fact]
    public async Task WithoutJsonOutputFile_DoesNotWriteEndpointCatalog()
    {
        var currentDirectory = CreateTempDir();

        try
        {
            await ExecuteAsync(currentDirectory, ["endpoints"]);

            Assert.False(Directory.Exists(Path.Combine(currentDirectory, "artifacts")));
        }
        finally
        {
            Directory.Delete(currentDirectory, recursive: true);
        }
    }

    [Fact]
    public async Task WithJsonOutputFile_WritesExplicitEndpointCatalog()
    {
        var currentDirectory = CreateTempDir();

        try
        {
            var outputPath = Path.Combine(currentDirectory, "custom", "endpoints.json");
            await ExecuteAsync(
                currentDirectory,
                [
                    "endpoints",
                    "--json-output-file",
                    Path.GetRelativePath(currentDirectory, outputPath),
                ]);

            Assert.True(File.Exists(outputPath));
            using var document = System.Text.Json.JsonDocument.Parse(
                await File.ReadAllTextAsync(outputPath));
            var root = document.RootElement;
            Assert.Equal(System.Text.Json.JsonValueKind.Array, root.ValueKind);
            Assert.Equal(1, root.GetArrayLength());
            var operation = root[0];
            Assert.Equal(
                "GET /api/items",
                operation.GetProperty("stableOperationKey").GetString());
            Assert.Equal(
                "GET",
                operation.GetProperty("httpMethod").GetString());
            Assert.Equal(
                "/api/items",
                operation.GetProperty("route").GetString());
            Assert.False(Directory.Exists(Path.Combine(currentDirectory, "artifacts")));
        }
        finally
        {
            Directory.Delete(currentDirectory, recursive: true);
        }
    }

    private static async Task ExecuteAsync(
        string currentDirectory,
        string[] arguments)
    {
        ReplayOperation operation = new()
        {
            StableOperationKey = "GET /api/items",
            HttpMethod = "GET",
            Route = "/api/items",
            RequiresAuthentication = false,
            HasJsonRequestBody = false,
            RequestBodyRequired = false,
            Parameters = [],
        };
        EndpointsCommand command = new(
            new StaticReplayOperationCatalogLoader(operation),
            new EndpointSourceProjectResolver());
        CommandExecutionContext context = new()
        {
            StartupOptions = new HostStartupOptions
            {
                AppTargetPath = RepositoryPaths.GetTestAppProjectPath(),
            },
            Arguments = arguments,
            CurrentDirectory = currentDirectory,
            ConsoleWriter = new HostConsoleWriter(),
            DebugWriter = HostDebugWriter.Disabled,
        };

        var exitCode = await command.ExecuteAsync(context);

        Assert.Equal(0, exitCode);
    }

    private static string CreateTempDir()
    {
        var directory = Path.Combine(
            Path.GetTempPath(),
            "sqloom-endpoints-command-tests",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        return directory;
    }

    private sealed class StaticReplayOperationCatalogLoader : IReplayOperationCatalogLoader
    {
        private readonly IReadOnlyList<ReplayOperation> _operations;

        public StaticReplayOperationCatalogLoader(params ReplayOperation[] operations)
        {
            _operations = operations;
        }

        public Task<IReadOnlyList<ReplayOperation>> LoadAsync(
            string sourceProjectPath,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_operations);
        }
    }
}
