using System;
using System.Collections.Generic;
using System.Linq;

namespace Sqloom.Host;

internal enum CommandTargetKind
{
    None,
    Optional,
    Required,
}

internal sealed record CommandOptionSpec(
    string Name,
    string Description,
    string? ValueName = null,
    bool IsRequired = false,
    string? DefaultValue = null)
{
    public bool TakesValue => ValueName is not null;

    public string Syntax => TakesValue
        ? $"{Name} <{ValueName}>"
        : Name;
}

internal sealed record CommandSpec(
    HostCommandKind Kind,
    string Verb,
    string Description,
    CommandTargetKind TargetKind,
    bool SupportsDebug,
    bool SupportsHarnessOptions,
    IReadOnlyList<CommandOptionSpec> Options)
{
    public string Usage
    {
        get
        {
            List<string> parts = [];
            if (SupportsDebug)
            {
                parts.Add("[--debug]");
            }

            parts.Add(Verb);
            if (TargetKind == CommandTargetKind.Optional)
            {
                parts.Add("[<path>]");
            }
            else if (TargetKind == CommandTargetKind.Required)
            {
                parts.Add("<path>");
            }

            if (SupportsHarnessOptions)
            {
                parts.Add("[--dotnet-command <command>]");
                parts.Add("[--no-build]");
            }

            parts.AddRange(Options.Select(option => option.IsRequired
                ? option.Syntax
                : $"[{option.Syntax}]"));
            return string.Join(' ', parts);
        }
    }
}

internal static class CommandCatalog
{
    public static IReadOnlyList<CommandOptionSpec> ToolOptions { get; } =
    [
        new("--help", "Prints Sqloom command usage."),
        new("--version", "Prints the installed Sqloom tool version."),
    ];

    public static IReadOnlyList<CommandOptionSpec> StartupOptions { get; } =
    [
        new("--debug", "Prints per-stage diagnostics to stderr."),
        new("--dotnet-command", "Uses a specific dotnet executable for project resolution and builds.", "command", DefaultValue: "dotnet"),
        new("--no-build", "Skips building harness projects before scanning their outputs."),
    ];

    public static IReadOnlyList<CommandSpec> Commands { get; } =
    [
        new(
            HostCommandKind.Init,
            "init",
            "Scaffolds the embedded sqloom agent skill from a Git repository root.",
            CommandTargetKind.None,
            SupportsDebug: false,
            SupportsHarnessOptions: false,
            [
                new("--agent", "Selects the agent skill location. Allowed values: codex, claude, copilot, all.", "codex|claude|copilot|all", DefaultValue: "codex"),
                new("--overwrite", "Replaces changed scaffolded skill files."),
            ]),
        new(
            HostCommandKind.Observe,
            "observe",
            "Collects SQL Server Query Store evidence and workload classification.",
            CommandTargetKind.Optional,
            SupportsDebug: true,
            SupportsHarnessOptions: true,
            [
                new("--read-only-connection-string", "SQL Server or Azure SQL connection string used to read Query Store and metadata.", "connection-string", IsRequired: true),
                new("--lookback-hours", "Query Store lookback window in hours.", "hours", DefaultValue: "24"),
                new("--max-plans", "Maximum Query Store plans to capture before console filtering.", "count", DefaultValue: "100"),
                new("--max-waits", "Maximum Query Store waits to capture.", "count", DefaultValue: "10"),
                new("--command-timeout-seconds", "SQL command timeout for Query Store reads.", "seconds", DefaultValue: "30"),
                new("--json-output-file", "Writes the Query Store snapshot to a specific JSON path.", "path"),
                new("--app-only", "Filters the console view to App-classified entries and implies --show-classification."),
                new("--show-classification", "Prints classification details for displayed plans and waits."),
            ]),
        new(
            HostCommandKind.Tune,
            "tune",
            "Runs replay, observe, correlate, and advise as one workflow.",
            CommandTargetKind.Required,
            SupportsDebug: true,
            SupportsHarnessOptions: true,
            [
                new("--read-only-connection-string", "Overrides the harness session connection used for Query Store and schema export.", "connection-string"),
                new("--lookback-hours", "Query Store lookback window in hours.", "hours", DefaultValue: "24"),
                new("--max-plans", "Maximum Query Store plans to capture before console filtering.", "count", DefaultValue: "100"),
                new("--max-waits", "Maximum Query Store waits to capture.", "count", DefaultValue: "10"),
                new("--command-timeout-seconds", "SQL command timeout for Query Store reads.", "seconds", DefaultValue: "30"),
                new("--app-only", "Filters the console view to App-classified entries and implies --show-classification."),
                new("--show-classification", "Prints classification details for displayed plans and waits."),
                new("--openapi-file", "Overrides the app-owned OpenAPI document.", "path"),
                new("--sqlserver-dacpac-file", "Overrides the harness DACPAC for replay and advice schema extraction.", "path"),
                new("--sqlserver-seed-sql-file", "Overrides the SQL seed script applied after the effective DACPAC.", "path"),
                new("--artifact-dir", "Uses a custom tune workflow root.", "path", DefaultValue: "artifacts/sqloom/tune/tune-<timestamp>"),
                new("--max-operations", "Caps replayed operations after filtering.", "count", DefaultValue: "25"),
                new("--target", "Replays one exact operation in the form METHOD /path/template.", "METHOD /path/template"),
                new("--replay-data-agent", "Controls replay request data preparation. Allowed values: off, auto, required.", "off|auto|required", DefaultValue: "off"),
                new("--replay-data-agent-model", "Selects the replay data agent model.", "id", DefaultValue: "gpt-5.4-mini"),
                new("--model-provider", "Selects the advice provider. The supported value is openai.", "openai", IsRequired: true),
                new("--sqlserver-schema-file", "Uses manually supplied schema SQL instead of DACPAC extraction.", "path"),
                new("--openai-model", "Selects the OpenAI advice model.", "id", DefaultValue: "gpt-5.4-mini"),
                new("--openai-base-url", "Sets the OpenAI base URL.", "url", DefaultValue: "https://api.openai.com"),
                new("--openai-api-key", "Supplies the OpenAI API key used by enabled agent workflows.", "key", IsRequired: true),
            ]),
        new(
            HostCommandKind.Replay,
            "replay",
            "Starts the harness and replays selected OpenAPI operations.",
            CommandTargetKind.Required,
            SupportsDebug: true,
            SupportsHarnessOptions: true,
            [
                new("--openapi-file", "Overrides the app-owned OpenAPI document.", "path"),
                new("--sqlserver-dacpac-file", "Overrides the harness DACPAC for SQL Server replay.", "path"),
                new("--sqlserver-seed-sql-file", "Overrides the SQL seed script applied after the DACPAC.", "path"),
                new("--artifact-dir", "Uses a custom replay output directory.", "path", DefaultValue: "artifacts/sqloom/replay/<timestamp>"),
                new("--max-operations", "Caps replayed operations after filtering.", "count", DefaultValue: "25"),
                new("--target", "Replays one exact operation in the form METHOD /path/template.", "METHOD /path/template"),
                new("--replay-data-agent", "Controls replay request data preparation. Allowed values: off, auto, required.", "off|auto|required", DefaultValue: "off"),
                new("--replay-data-agent-model", "Selects the replay data agent model.", "id", DefaultValue: "gpt-5.4-mini"),
                new("--openai-base-url", "Sets the OpenAI base URL for the replay data agent.", "url", DefaultValue: "https://api.openai.com"),
                new("--openai-api-key", "Supplies the OpenAI API key used by the replay data agent.", "key"),
            ]),
        new(
            HostCommandKind.Correlate,
            "correlate",
            "Correlates replay SQL with a Query Store snapshot.",
            CommandTargetKind.None,
            SupportsDebug: true,
            SupportsHarnessOptions: false,
            [
                new("--replay-artifact-dir", "Replay output directory containing operation artifacts.", "path", IsRequired: true),
                new("--query-store-snapshot-file", "Query Store snapshot JSON produced by observe.", "path", IsRequired: true),
                new("--read-only-connection-string", "Connection string used for statement handle resolution.", "connection-string", IsRequired: true),
                new("--json-output-file", "Writes correlation to a specific JSON path.", "path"),
            ]),
        new(
            HostCommandKind.Advise,
            "advise",
            "Generates evidence-backed tuning advice and SQL proposals.",
            CommandTargetKind.None,
            SupportsDebug: true,
            SupportsHarnessOptions: false,
            [
                new("--replay-artifact-dir", "Replay output directory containing correlation evidence.", "path", IsRequired: true),
                new("--query-store-correlation-file", "Uses a correlation file outside the replay output directory.", "path"),
                new("--read-only-connection-string", "Exports a schema-source DACPAC when no schema file or DACPAC is supplied.", "connection-string"),
                new("--sqlserver-schema-file", "Uses manually supplied schema SQL instead of DACPAC extraction.", "path"),
                new("--sqlserver-dacpac-file", "Extracts SQL Server schema from a DACPAC.", "path"),
                new("--json-output-file", "Writes advice to a specific JSON path.", "path"),
                new("--model-provider", "Selects the advice provider. The supported value is openai.", "openai", IsRequired: true),
                new("--openai-model", "Selects the OpenAI advice model.", "id", DefaultValue: "gpt-5.4-mini"),
                new("--openai-base-url", "Sets the OpenAI base URL.", "url", DefaultValue: "https://api.openai.com"),
                new("--openai-api-key", "Supplies the OpenAI API key used by advice generation.", "key", IsRequired: true),
            ]),
    ];

    public static CommandSpec GetRequired(HostCommandKind kind)
    {
        return Commands.FirstOrDefault(command => command.Kind == kind)
            ?? throw new ArgumentOutOfRangeException(nameof(kind), kind, null);
    }

    public static CommandSpec? Find(string verb)
    {
        return Commands.FirstOrDefault(command =>
            string.Equals(command.Verb, verb, StringComparison.OrdinalIgnoreCase));
    }
}
