using System;
using System.IO;
using Sqloom.Tests;
using Xunit;

namespace Sqloom.Host.Tests;

/// <summary>
/// Exercises Sqloom host startup command line.
/// </summary>
public sealed class HostStartupCommandLineTests
{
    [Fact]
    public void ReplayProjectPath_SelectsProjectAndRemovesApplicationArg()
    {
        HostStartupCommandLine commandLine = new();
        var currentDirectory = RepositoryPaths.GetRepositoryRoot();
        const string relativeProjectPath = @".\tests\Sqloom.TestApp.Harness\Sqloom.TestApp.Harness.csproj";

        var startupOptions = commandLine.Parse(
            [
                "replay",
                relativeProjectPath,
                "--no-build",
                "--target",
                SampleCatalogReplayScenario.OperationKey,
            ],
            currentDirectory);

        Assert.Equal(
            Path.GetFullPath(relativeProjectPath, currentDirectory),
            startupOptions.AppTargetPath,
            StringComparer.OrdinalIgnoreCase);
        Assert.True(startupOptions.NoBuild);
        Assert.True(startupOptions.HasTargetSelection);
        Assert.Collection(
            startupOptions.ApplicationArguments,
            item => Assert.Equal("replay", item),
            item => Assert.Equal("--target", item),
            item => Assert.Equal(SampleCatalogReplayScenario.OperationKey, item));
    }

    [Fact]
    public void TuneProjectPath_SelectsProjectAndRemovesApplicationArg()
    {
        HostStartupCommandLine commandLine = new();
        var currentDirectory = RepositoryPaths.GetRepositoryRoot();
        const string relativeProjectPath = @".\tests\Sqloom.TestApp.Harness\Sqloom.TestApp.Harness.csproj";

        var startupOptions = commandLine.Parse(
            [
                "tune",
                relativeProjectPath,
                "--read-only-connection-string",
                "Server=localhost;Database=Sqloom;Trusted_Connection=True;",
                "--target",
                SampleCatalogReplayScenario.OperationKey,
            ],
            currentDirectory);

        Assert.Equal(
            Path.GetFullPath(relativeProjectPath, currentDirectory),
            startupOptions.AppTargetPath,
            StringComparer.OrdinalIgnoreCase);
        Assert.True(startupOptions.HasTargetSelection);
        Assert.Collection(
            startupOptions.ApplicationArguments,
            item => Assert.Equal("tune", item),
            item => Assert.Equal("--read-only-connection-string", item),
            item => Assert.Equal("Server=localhost;Database=Sqloom;Trusted_Connection=True;", item),
            item => Assert.Equal("--target", item),
            item => Assert.Equal(SampleCatalogReplayScenario.OperationKey, item));
    }

    [Fact]
    public void ObserveSolutionPath_SelectsTargetAndRemovesApplicationArg()
    {
        HostStartupCommandLine commandLine = new();
        var currentDirectory = RepositoryPaths.GetRepositoryRoot();
        const string relativeSolutionPath = @".\Sqloom.slnx";

        var startupOptions = commandLine.Parse(
            [
                "observe",
                relativeSolutionPath,
                "--show-classification",
            ],
            currentDirectory);

        Assert.Equal(
            Path.GetFullPath(relativeSolutionPath, currentDirectory),
            startupOptions.AppTargetPath,
            StringComparer.OrdinalIgnoreCase);
        Assert.True(startupOptions.HasTargetSelection);
        Assert.Collection(
            startupOptions.ApplicationArguments,
            item => Assert.Equal("observe", item),
            item => Assert.Equal("--show-classification", item));
    }

    [Fact]
    public void DotNetCommandAfterReplay_StoresCommandAndRemovesArg()
    {
        HostStartupCommandLine commandLine = new();
        var currentDirectory = RepositoryPaths.GetRepositoryRoot();
        const string relativeProjectPath = @".\tests\Sqloom.TestApp.Harness\Sqloom.TestApp.Harness.csproj";

        var startupOptions = commandLine.Parse(
            [
                "replay",
                relativeProjectPath,
                "--dotnet-command",
                "custom-dotnet",
                "--target",
                SampleCatalogReplayScenario.OperationKey,
            ],
            currentDirectory);

        Assert.Equal("custom-dotnet", startupOptions.DotNetCommand);
        Assert.Equal(
            Path.GetFullPath(relativeProjectPath, currentDirectory),
            startupOptions.AppTargetPath,
            StringComparer.OrdinalIgnoreCase);
        Assert.Collection(
            startupOptions.ApplicationArguments,
            item => Assert.Equal("replay", item),
            item => Assert.Equal("--target", item),
            item => Assert.Equal(SampleCatalogReplayScenario.OperationKey, item));
    }

    [Fact]
    public void GlobalDebugSwitch_SetsDebugAndRemovesApplicationArg()
    {
        HostStartupCommandLine commandLine = new();
        var currentDirectory = RepositoryPaths.GetRepositoryRoot();
        const string relativeProjectPath = @".\tests\Sqloom.TestApp.Harness\Sqloom.TestApp.Harness.csproj";

        var startupOptions = commandLine.Parse(
            [
                "replay",
                relativeProjectPath,
                "--debug",
                "--target",
                SampleCatalogReplayScenario.OperationKey,
            ],
            currentDirectory);

        Assert.True(startupOptions.DebugEnabled);
        Assert.Equal(
            Path.GetFullPath(relativeProjectPath, currentDirectory),
            startupOptions.AppTargetPath,
            StringComparer.OrdinalIgnoreCase);
        Assert.Collection(
            startupOptions.ApplicationArguments,
            item => Assert.Equal("replay", item),
            item => Assert.Equal("--target", item),
            item => Assert.Equal(SampleCatalogReplayScenario.OperationKey, item));
    }

    [Theory]
    [InlineData(@".\tests\Sqloom.TestApp.Harness\Sqloom.TestApp.Harness.csproj")]
    [InlineData(@".\tests\Sqloom.TestApp.Harness")]
    public void WithLeadingTargetPath_ThrowsWhenStageVerbIsMissing(string relativeTargetPath)
    {
        HostStartupCommandLine commandLine = new();
        var currentDirectory = RepositoryPaths.GetRepositoryRoot();

        var exception = Assert.Throws<ArgumentException>(
            () => commandLine.Parse(
                [
                    relativeTargetPath,
                    "--target",
                    SampleCatalogReplayScenario.OperationKey,
                ],
                currentDirectory));

        Assert.Contains("explicit stage verb", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("--app-assembly")]
    [InlineData("--app-assembly-file")]
    [InlineData("--project")]
    public void ThrowsWhenUnsupportedStartupSwitchIsUsed(string switchName)
    {
        HostStartupCommandLine commandLine = new();
        var currentDirectory = RepositoryPaths.GetRepositoryRoot();

        var exception = Assert.Throws<ArgumentException>(
            () => commandLine.Parse(
                [
                    "replay",
                    switchName,
                    @".\tests\Sqloom.TestApp.Harness\Sqloom.TestApp.Harness.csproj",
                ],
                currentDirectory));

        Assert.Contains("Unsupported switch", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(switchName, exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ThrowsWhenDotNetCommandValueIsMissing()
    {
        HostStartupCommandLine commandLine = new();
        var currentDirectory = RepositoryPaths.GetRepositoryRoot();

        var exception = Assert.Throws<ArgumentException>(
            () => commandLine.Parse(
                [
                    "replay",
                    @".\tests\Sqloom.TestApp.Harness\Sqloom.TestApp.Harness.csproj",
                    "--dotnet-command",
                ],
                currentDirectory));

        Assert.Contains("--dotnet-command", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void WithVersionSwitch_SetsShowVersionAndSkipsTargetSelection()
    {
        HostStartupCommandLine commandLine = new();
        var currentDirectory = RepositoryPaths.GetRepositoryRoot();

        var startupOptions = commandLine.Parse(
            [
                "--version",
            ],
            currentDirectory);

        Assert.True(startupOptions.ShowVersion);
        Assert.False(startupOptions.ShowHelp);
        Assert.False(startupOptions.HasTargetSelection);
        Assert.Null(startupOptions.AppTargetPath);
        Assert.Empty(startupOptions.ApplicationArguments);
    }

    [Fact]
    public void WithHelpVerb_KeepsVerbArgumentsAndSkipsTargetSelection()
    {
        HostStartupCommandLine commandLine = new();
        var currentDirectory = RepositoryPaths.GetRepositoryRoot();

        var startupOptions = commandLine.Parse(
            [
                "help",
                "replay",
            ],
            currentDirectory);

        Assert.False(startupOptions.ShowHelp);
        Assert.False(startupOptions.HasTargetSelection);
        Assert.Null(startupOptions.AppTargetPath);
        Assert.Collection(
            startupOptions.ApplicationArguments,
            item => Assert.Equal("help", item),
            item => Assert.Equal("replay", item));
    }

    [Fact]
    public void WithInitVerb_KeepsArgumentsAndSkipsTargetSelection()
    {
        HostStartupCommandLine commandLine = new();
        var currentDirectory = RepositoryPaths.GetRepositoryRoot();

        var startupOptions = commandLine.Parse(
            [
                "init",
                "--agent",
                "codex",
            ],
            currentDirectory);

        Assert.False(startupOptions.HasTargetSelection);
        Assert.Null(startupOptions.AppTargetPath);
        Assert.Collection(
            startupOptions.ApplicationArguments,
            item => Assert.Equal("init", item),
            item => Assert.Equal("--agent", item),
            item => Assert.Equal("codex", item));
    }

    [Fact]
    public void WithInitAndPathLikeArgument_DoesNotSelectHarnessTarget()
    {
        HostStartupCommandLine commandLine = new();
        var currentDirectory = RepositoryPaths.GetRepositoryRoot();

        var startupOptions = commandLine.Parse(
            [
                "init",
                ".",
            ],
            currentDirectory);

        Assert.False(startupOptions.HasTargetSelection);
        Assert.Null(startupOptions.AppTargetPath);
        Assert.Collection(
            startupOptions.ApplicationArguments,
            item => Assert.Equal("init", item),
            item => Assert.Equal(".", item));
    }

    [Fact]
    public void ThrowsWhenUnknownLeadingCommandIsUsed()
    {
        HostStartupCommandLine commandLine = new();
        var currentDirectory = RepositoryPaths.GetRepositoryRoot();

        var exception = Assert.Throws<ArgumentException>(
            () => commandLine.Parse(
                [
                    "benchmark",
                    @".\tests\Sqloom.TestApp.Harness\Sqloom.TestApp.Harness.csproj",
                ],
                currentDirectory));

        Assert.Contains("Unknown Sqloom command", exception.Message, StringComparison.OrdinalIgnoreCase);
    }
}
