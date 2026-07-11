using System;
using System.IO;
using System.Linq;
using Sqloom.Tests;
using Xunit;

namespace Sqloom.Host.Tests;

public sealed class CommandCatalogTests
{
    [Fact]
    public void CommandsHaveStableOrderAndCompleteKinds()
    {
        Assert.Equal(
            ["init", "observe", "tune", "replay", "correlate", "advise"],
            CommandCatalog.Commands.Select(command => command.Verb));
        Assert.Equal(
            Enum.GetValues<HostCommandKind>().Where(kind => kind is not HostCommandKind.None and not HostCommandKind.Help),
            CommandCatalog.Commands.Select(command => command.Kind).OrderBy(kind => kind));
        Assert.Equal(
            CommandCatalog.Commands.Count,
            CommandCatalog.Commands.Select(command => command.Verb).Distinct(StringComparer.OrdinalIgnoreCase).Count());
        Assert.All(
            CommandCatalog.Commands,
            command => Assert.Equal(
                command.Options.Count,
                command.Options.Select(option => option.Name).Distinct(StringComparer.OrdinalIgnoreCase).Count()));
    }

    [Fact]
    public void CatalogCapturesRepresentativeRuntimeSemantics()
    {
        var observe = CommandCatalog.GetRequired(HostCommandKind.Observe);
        Assert.Equal(CommandTargetKind.Optional, observe.TargetKind);
        Assert.True(observe.Options.Single(option => option.Name == "--read-only-connection-string").IsRequired);
        Assert.Equal("24", observe.Options.Single(option => option.Name == "--lookback-hours").DefaultValue);

        var replay = CommandCatalog.GetRequired(HostCommandKind.Replay);
        Assert.Equal(CommandTargetKind.Required, replay.TargetKind);
        Assert.True(replay.Options.Single(option => option.Name == "--max-operations").TakesValue);
        Assert.Equal("required", replay.Options.Single(option => option.Name == "--replay-data-agent").DefaultValue);
        Assert.False(replay.Options.Single(option => option.Name == "--openai-api-key").IsRequired);
    }

    [Fact]
    public void CommandUsageStaysConcise()
    {
        Assert.Equal("init [options]", CommandCatalog.GetRequired(HostCommandKind.Init).Usage);
        Assert.Equal(
            "observe [<path>] --read-only-connection-string <connection-string> [options]",
            CommandCatalog.GetRequired(HostCommandKind.Observe).Usage);
        Assert.Equal(
            "tune <path> --model-provider <openai> --openai-api-key <key> [options]",
            CommandCatalog.GetRequired(HostCommandKind.Tune).Usage);
        Assert.Equal("replay <path> [options]", CommandCatalog.GetRequired(HostCommandKind.Replay).Usage);
        Assert.Equal(
            "correlate --replay-artifact-dir <path> --query-store-snapshot-file <path> --read-only-connection-string <connection-string> [options]",
            CommandCatalog.GetRequired(HostCommandKind.Correlate).Usage);
    }

    [Fact]
    public void GeneratedReferenceUsesSectionedHelpShape()
    {
        var markdown = CommandReferenceMarkdown.Render();

        Assert.Contains("## Usage", markdown, StringComparison.Ordinal);
        Assert.Contains("| Command | Description |", markdown, StringComparison.Ordinal);
        Assert.Contains("### `replay`", markdown, StringComparison.Ordinal);
        Assert.Contains("#### Startup options", markdown, StringComparison.Ordinal);
        Assert.Contains("#### Notes", markdown, StringComparison.Ordinal);
        Assert.Contains("sqloom replay <path> [options]", markdown, StringComparison.Ordinal);
        Assert.DoesNotContain("sqloom [--debug] replay <path> [--dotnet-command", markdown, StringComparison.Ordinal);
    }

    [Fact]
    public void GeneratedReferenceMatchesCheckedInFile()
    {
        var repositoryRoot = RepositoryPaths.GetRepositoryRoot();
        var path = Path.Combine(
            repositoryRoot,
            CommandReferenceMarkdown.RelativePath.Replace('/', Path.DirectorySeparatorChar));

        var expected = NormalizeNewlines(CommandReferenceMarkdown.Render());
        var actual = NormalizeNewlines(File.ReadAllText(path));

        Assert.True(
            string.Equals(expected, actual, StringComparison.Ordinal),
            "The generated command reference is stale. Run: dotnet run --file .\\tools\\Sqloom.CommandDocs.cs -- --write");
    }

    private static string NormalizeNewlines(string value)
    {
        return value.Replace("\r\n", "\n", StringComparison.Ordinal);
    }
}
