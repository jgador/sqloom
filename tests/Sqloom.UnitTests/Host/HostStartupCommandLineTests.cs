using System;
using System.IO;
using Sqloom.TestApp.IntegrationTests;
using Sqloom.Tests;
using Xunit;

namespace Sqloom.Host.Tests;

/// <summary>
/// Exercises Sqloom host startup command line.
/// </summary>
public sealed class HostStartupCommandLineTests
{
    [Fact]
    public void Parse_WithProjectPathAfterReplayVerb_SelectsProjectAndRemovesItFromApplicationArguments()
    {
        HostStartupCommandLine commandLine = new();
        var currentDirectory = SqloomRepositoryPaths.GetRepositoryRoot();
        const string relativeProjectPath = @".\tests\Sqloom.TestApp\Sqloom.TestApp.csproj";

        var startupOptions = commandLine.Parse(
            [
                "replay",
                relativeProjectPath,
                "--no-build",
                "--target",
                TestAppProductCatalogScenario.OperationKey,
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
            item => Assert.Equal(TestAppProductCatalogScenario.OperationKey, item));
    }

    [Fact]
    public void Parse_WithProjectPathAfterTuneVerb_SelectsProjectAndRemovesItFromApplicationArguments()
    {
        HostStartupCommandLine commandLine = new();
        var currentDirectory = SqloomRepositoryPaths.GetRepositoryRoot();
        const string relativeProjectPath = @".\tests\Sqloom.TestApp\Sqloom.TestApp.csproj";

        var startupOptions = commandLine.Parse(
            [
                "tune",
                relativeProjectPath,
                "--read-only-connection-string",
                "Server=localhost;Database=Sqloom;Trusted_Connection=True;",
                "--target",
                TestAppProductCatalogScenario.OperationKey,
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
            item => Assert.Equal(TestAppProductCatalogScenario.OperationKey, item));
    }

    [Fact]
    public void Parse_WithSolutionPathAfterObserveVerb_SelectsTargetAndRemovesItFromApplicationArguments()
    {
        HostStartupCommandLine commandLine = new();
        var currentDirectory = SqloomRepositoryPaths.GetRepositoryRoot();
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
    public void Parse_WithDotNetCommandAfterReplayVerb_StoresExplicitCommandAndRemovesItFromApplicationArguments()
    {
        HostStartupCommandLine commandLine = new();
        var currentDirectory = SqloomRepositoryPaths.GetRepositoryRoot();
        const string relativeProjectPath = @".\tests\Sqloom.TestApp\Sqloom.TestApp.csproj";

        var startupOptions = commandLine.Parse(
            [
                "replay",
                relativeProjectPath,
                "--dotnet-command",
                "custom-dotnet",
                "--target",
                TestAppProductCatalogScenario.OperationKey,
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
            item => Assert.Equal(TestAppProductCatalogScenario.OperationKey, item));
    }

    [Fact]
    public void Parse_WithGlobalDebugSwitch_SetsDebugEnabledAndRemovesItFromApplicationArguments()
    {
        HostStartupCommandLine commandLine = new();
        var currentDirectory = SqloomRepositoryPaths.GetRepositoryRoot();
        const string relativeProjectPath = @".\tests\Sqloom.TestApp\Sqloom.TestApp.csproj";

        var startupOptions = commandLine.Parse(
            [
                "replay",
                relativeProjectPath,
                "--debug",
                "--target",
                TestAppProductCatalogScenario.OperationKey,
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
            item => Assert.Equal(TestAppProductCatalogScenario.OperationKey, item));
    }

    [Theory]
    [InlineData(@".\tests\Sqloom.TestApp\Sqloom.TestApp.csproj")]
    [InlineData(@".\tests\Sqloom.TestApp")]
    public void Parse_WithLeadingTargetPath_ThrowsWhenStageVerbIsMissing(string relativeTargetPath)
    {
        HostStartupCommandLine commandLine = new();
        var currentDirectory = SqloomRepositoryPaths.GetRepositoryRoot();

        var exception = Assert.Throws<ArgumentException>(
            () => commandLine.Parse(
                [
                    relativeTargetPath,
                    "--target",
                    TestAppProductCatalogScenario.OperationKey,
                ],
                currentDirectory));

        Assert.Contains("explicit stage verb", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("--app-assembly")]
    [InlineData("--app-assembly-file")]
    [InlineData("--project")]
    public void Parse_ThrowsWhenUnsupportedStartupSwitchIsUsed(string switchName)
    {
        HostStartupCommandLine commandLine = new();
        var currentDirectory = SqloomRepositoryPaths.GetRepositoryRoot();

        var exception = Assert.Throws<ArgumentException>(
            () => commandLine.Parse(
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
    public void Parse_ThrowsWhenDotNetCommandValueIsMissing()
    {
        HostStartupCommandLine commandLine = new();
        var currentDirectory = SqloomRepositoryPaths.GetRepositoryRoot();

        var exception = Assert.Throws<ArgumentException>(
            () => commandLine.Parse(
                [
                    "replay",
                    @".\tests\Sqloom.TestApp\Sqloom.TestApp.csproj",
                    "--dotnet-command",
                ],
                currentDirectory));

        Assert.Contains("--dotnet-command", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Parse_WithVersionSwitch_SetsShowVersionAndSkipsTargetSelection()
    {
        HostStartupCommandLine commandLine = new();
        var currentDirectory = SqloomRepositoryPaths.GetRepositoryRoot();

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
    public void Parse_ThrowsWhenUnknownLeadingCommandIsUsed()
    {
        HostStartupCommandLine commandLine = new();
        var currentDirectory = SqloomRepositoryPaths.GetRepositoryRoot();

        var exception = Assert.Throws<ArgumentException>(
            () => commandLine.Parse(
                [
                    "benchmark",
                    @".\tests\Sqloom.TestApp\Sqloom.TestApp.csproj",
                ],
                currentDirectory));

        Assert.Contains("Unknown Sqloom command", exception.Message, StringComparison.OrdinalIgnoreCase);
    }
}
