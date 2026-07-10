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
        Assert.False(replay.Options.Single(option => option.Name == "--openai-api-key").IsRequired);
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
