using System;
using System.IO;
using Sqloom.Core.Execution;
using Xunit;

namespace Sqloom.Core.Tests.Execution;

/// <summary>
/// Exercises Sqloom run options.
/// </summary>
public sealed class RunOptionsTests
{
    [Fact]
    public void CreateDefault_UsesBackendArtifactRoot()
    {
        var currentDirectory = AppContext.BaseDirectory;
        var repositoryRoot = RepositoryRootLocator.TryFind(currentDirectory)
            ?? throw new InvalidOperationException("Expected to resolve the repository root for the test assembly.");

        var options = RunOptions.CreateDefault(
            currentDirectory,
            "Server=tcp:readonly;");

        Assert.Equal(
            Path.Combine(repositoryRoot, "backend", "artifacts", "sqloom"),
            options.ArtifactRoot);
        Assert.True(options.ObserveQueryStore);
        Assert.False(options.ReplayOperations);
        Assert.True(options.CaptureSqlDuringReplay);
        Assert.True(options.CorrelateReplayToQueryStore);
        Assert.True(options.GenerateTuningAdvice);
        Assert.Equal(25, options.MaxOperations);
    }
}
