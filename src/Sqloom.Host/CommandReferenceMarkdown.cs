using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sqloom.Host;

/// <summary>
/// Renders the deterministic command reference from the Sqloom command catalog.
/// </summary>
public static class CommandReferenceMarkdown
{
    /// <summary>
    /// Gets the repository-relative path of the generated command reference.
    /// </summary>
    public const string RelativePath = ".agents/skills/sqloom/references/commands.md";

    /// <summary>
    /// Renders the complete command reference as Markdown.
    /// </summary>
    public static string Render()
    {
        StringBuilder builder = new();
        builder.AppendLine("# Sqloom Command Reference");
        builder.AppendLine();
        builder.AppendLine("Use this file as the concise Sqloom CLI reference for coding agents.");
        builder.AppendLine();
        builder.AppendLine("## Agent use");
        builder.AppendLine();
        builder.AppendLine("- Use this file for exact command syntax, required options, option names, allowed values, defaults, and command-specific notes.");
        builder.AppendLine("- Prefer `sqloom tune` for the full replay -> observe -> correlate -> advise workflow; use individual commands for focused stage work.");
        builder.AppendLine("- Treat `$env:OPENAI_API_KEY` examples as shell expansion into `--openai-api-key`; Sqloom does not automatically read that environment variable.");
        builder.AppendLine("- When validating an installed tool, run `sqloom help <command>` and compare the command surface against this reference.");
        builder.AppendLine();
        builder.AppendLine("## Usage");
        builder.AppendLine();
        builder.AppendLine("```text");
        builder.AppendLine("sqloom [tool-options]");
        builder.AppendLine("sqloom <command> [arguments] [options]");
        builder.AppendLine("sqloom help [command]");
        builder.AppendLine("```");
        builder.AppendLine();
        builder.AppendLine("## Tool options");
        builder.AppendLine();
        AppendOptionTable(builder, CommandCatalog.ToolOptions);
        builder.AppendLine("## Commands");
        builder.AppendLine();
        AppendTable(
            builder,
            "Command",
            "Description",
            CommandCatalog.Commands.Select(static command => ($"`{command.Verb}`", command.Description)));

        foreach (var command in CommandCatalog.Commands)
        {
            builder.Append("### `").Append(command.Verb).AppendLine("`");
            builder.AppendLine();
            builder.AppendLine(command.Description);
            builder.AppendLine();
            builder.AppendLine("```text");
            builder.Append("sqloom ").AppendLine(command.Usage);
            builder.AppendLine("```");
            builder.AppendLine();

            AppendTable(
                builder,
                "Argument",
                "Description",
                command.ArgumentRows.Select(static row => ($"`{row.Syntax}`", row.Description)));

            AppendOptionTable(
                builder,
                command.SupportedStartupOptions,
                heading: "#### Startup options");
            AppendOptionTable(
                builder,
                command.Options.Where(static option => option.IsRequired),
                heading: "#### Required options");
            AppendOptionTable(
                builder,
                command.Options.Where(static option => !option.IsRequired),
                heading: "#### Options");
            AppendNotes(builder, command.Notes);
        }

        return builder.ToString().Replace("\r\n", "\n", StringComparison.Ordinal);
    }

    private static void AppendOptionTable(
        StringBuilder builder,
        IEnumerable<CommandOptionSpec> options,
        string? heading = null)
    {
        AppendTable(
            builder,
            "Option",
            "Description",
            options.Select(static option => ($"`{EscapeMarkdown(option.Syntax)}`", FormatOptionDescription(option))),
            heading);
    }

    private static void AppendTable(
        StringBuilder builder,
        string leftHeading,
        string rightHeading,
        IEnumerable<(string Left, string Right)> rows,
        string? heading = null)
    {
        var materializedRows = rows.ToArray();
        if (materializedRows.Length == 0)
        {
            return;
        }

        if (heading is not null)
        {
            builder.AppendLine(heading);
            builder.AppendLine();
        }

        builder.Append("| ").Append(leftHeading).Append(" | ").Append(rightHeading).AppendLine(" |");
        builder.AppendLine("| --- | --- |");
        foreach (var row in materializedRows)
        {
            builder.Append("| ")
                .Append(row.Left)
                .Append(" | ")
                .Append(EscapeMarkdown(row.Right))
                .AppendLine(" |");
        }

        builder.AppendLine();
    }

    private static void AppendNotes(StringBuilder builder, IReadOnlyList<string> notes)
    {
        if (notes.Count == 0)
        {
            return;
        }

        builder.AppendLine("#### Agent notes");
        builder.AppendLine();
        foreach (var note in notes)
        {
            builder.Append("- ").AppendLine(EscapeMarkdown(note));
        }

        builder.AppendLine();
    }

    private static string FormatOptionDescription(CommandOptionSpec option)
    {
        var description = option.Description;
        if (option.IsRequired)
        {
            description += " Required.";
        }

        if (option.DefaultValue is not null)
        {
            description += $" Default: `{option.DefaultValue}`.";
        }

        return description;
    }

    private static string EscapeMarkdown(string value)
    {
        return value.Replace("|", "\\|", StringComparison.Ordinal);
    }
}
