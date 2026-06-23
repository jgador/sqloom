using System;
using Sqloom.Core.Execution;
using Sqloom.Tests;
using Xunit;

namespace Sqloom.Core.Tests.Execution;

/// <summary>
/// Exercises Sqloom run options.
/// </summary>
public sealed class RunOptionsTests
{
    [Fact]
    public void CreateDefault_UsesRepoArtifactRoot()
    {
        var currentDirectory = AppContext.BaseDirectory;

        var options = RunOptions.CreateDefault(
            currentDirectory,
            "Server=tcp:readonly;");

        Assert.Equal(
            SqloomRepositoryPaths.GetDefaultArtifactRoot(),
            options.ArtifactRoot);
        Assert.True(options.ObserveQueryStore);
        Assert.False(options.ReplayOperations);
        Assert.True(options.CaptureSqlDuringReplay);
        Assert.True(options.CorrelateReplay);
        Assert.True(options.GenerateTuningAdvice);
        Assert.Equal(25, options.MaxOperations);
    }
}
