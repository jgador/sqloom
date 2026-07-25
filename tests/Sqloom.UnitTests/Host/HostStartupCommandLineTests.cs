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
        var currentDirectory = RepositoryPaths.GetRepositoryRoot();
        const string relativeProjectPath = @".\tests\Sqloom.TestApp\Sqloom.TestApp.csproj";

        var startupOptions = HostStartupCommandLine.Parse(
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
    public void ReplayCSharpFilePath_SelectsTargetAndRemovesApplicationArg()
    {
        var currentDirectory = RepositoryPaths.GetRepositoryRoot();
        const string relativeFilePath = @".\tests\Sqloom\Sqloom.TestApp\default\Harness.cs";

        var startupOptions = HostStartupCommandLine.Parse(
            [
                "replay",
                relativeFilePath,
                "--target",
                SampleCatalogReplayScenario.OperationKey,
            ],
            currentDirectory);

        Assert.Equal(
            Path.GetFullPath(relativeFilePath, currentDirectory),
            startupOptions.AppTargetPath,
            StringComparer.OrdinalIgnoreCase);
        Assert.True(startupOptions.HasTargetSelection);
        Assert.Collection(
            startupOptions.ApplicationArguments,
            item => Assert.Equal("replay", item),
            item => Assert.Equal("--target", item),
            item => Assert.Equal(SampleCatalogReplayScenario.OperationKey, item));
    }

    [Fact]
    public void EndpointsProjectPath_SelectsTargetAndRemovesApplicationArg()
    {
        var currentDirectory = RepositoryPaths.GetRepositoryRoot();
        const string relativeProjectPath = @".\tests\Sqloom.TestApp\Sqloom.TestApp.csproj";
        const string relativeOutputPath = @".\artifacts\sqloom\endpoints.json";

        var startupOptions = HostStartupCommandLine.Parse(
            [
                "endpoints",
                relativeProjectPath,
                "--json-output-file",
                relativeOutputPath,
            ],
            currentDirectory);

        Assert.Equal(
            Path.GetFullPath(relativeProjectPath, currentDirectory),
            startupOptions.AppTargetPath,
            StringComparer.OrdinalIgnoreCase);
        Assert.True(startupOptions.HasTargetSelection);
        Assert.Collection(
            startupOptions.ApplicationArguments,
            item => Assert.Equal("endpoints", item),
            item => Assert.Equal("--json-output-file", item),
            item => Assert.Equal(relativeOutputPath, item));
    }

    [Fact]
    public void TuneProjectPath_SelectsProjectAndRemovesApplicationArg()
    {
        var currentDirectory = RepositoryPaths.GetRepositoryRoot();
        const string relativeProjectPath = @".\tests\Sqloom.TestApp\Sqloom.TestApp.csproj";

        var startupOptions = HostStartupCommandLine.Parse(
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
        var currentDirectory = RepositoryPaths.GetRepositoryRoot();
        const string relativeSolutionPath = @".\Sqloom.slnx";

        var startupOptions = HostStartupCommandLine.Parse(
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
        var currentDirectory = RepositoryPaths.GetRepositoryRoot();
        const string relativeProjectPath = @".\tests\Sqloom.TestApp\Sqloom.TestApp.csproj";

        var startupOptions = HostStartupCommandLine.Parse(
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
        var currentDirectory = RepositoryPaths.GetRepositoryRoot();
        const string relativeProjectPath = @".\tests\Sqloom.TestApp\Sqloom.TestApp.csproj";

        var startupOptions = HostStartupCommandLine.Parse(
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
    [InlineData(@".\tests\Sqloom.TestApp\Sqloom.TestApp.csproj")]
    [InlineData(@".\tests\Sqloom.TestApp")]
    [InlineData(@".\tests\Sqloom\Sqloom.TestApp\default\Harness.cs")]
    public void WithLeadingTargetPath_ThrowsWhenStageVerbIsMissing(string relativeTargetPath)
    {
        var currentDirectory = RepositoryPaths.GetRepositoryRoot();

        var exception = Assert.Throws<ArgumentException>(
            () => HostStartupCommandLine.Parse(
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
        var currentDirectory = RepositoryPaths.GetRepositoryRoot();

        var exception = Assert.Throws<ArgumentException>(
            () => HostStartupCommandLine.Parse(
                [
                    "replay",
                    switchName,
                    @".\tests\Sqloom.TestApp\Sqloom.TestApp.csproj",
                ],
                currentDirectory));

        Assert.Contains("Unsupported switch", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(switchName, exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ThrowsWhenDotNetCommandValueIsMissing()
    {
        var currentDirectory = RepositoryPaths.GetRepositoryRoot();

        var exception = Assert.Throws<ArgumentException>(
            () => HostStartupCommandLine.Parse(
                [
                    "replay",
                    @".\tests\Sqloom.TestApp\Sqloom.TestApp.csproj",
                    "--dotnet-command",
                ],
                currentDirectory));

        Assert.Contains("--dotnet-command", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void WithVersionSwitch_SetsShowVersionAndSkipsTargetSelection()
    {
        var currentDirectory = RepositoryPaths.GetRepositoryRoot();

        var startupOptions = HostStartupCommandLine.Parse(
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
        var currentDirectory = RepositoryPaths.GetRepositoryRoot();

        var startupOptions = HostStartupCommandLine.Parse(
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
        var currentDirectory = RepositoryPaths.GetRepositoryRoot();

        var startupOptions = HostStartupCommandLine.Parse(
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
        var currentDirectory = RepositoryPaths.GetRepositoryRoot();

        var startupOptions = HostStartupCommandLine.Parse(
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
        var currentDirectory = RepositoryPaths.GetRepositoryRoot();

        var exception = Assert.Throws<ArgumentException>(
            () => HostStartupCommandLine.Parse(
                [
                    "benchmark",
                    @".\tests\Sqloom.TestApp\Sqloom.TestApp.csproj",
                ],
                currentDirectory));

        Assert.Contains("Unknown Sqloom command", exception.Message, StringComparison.OrdinalIgnoreCase);
    }
}
