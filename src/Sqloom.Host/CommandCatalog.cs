using System;
using System.Collections.Generic;
using System.Linq;

namespace Sqloom.Host;

/// <summary>
/// Defines how a command participates in harness target binding and usage generation.
/// </summary>
internal enum CommandTargetKind
{
    None,

    /// <summary>
    /// The command accepts a harness target path when one is supplied, but also has a no-harness mode.
    /// </summary>
    Optional,

    Required,
}

/// <summary>
/// Describes one command switch for help output, generated references, and shared switch validation.
/// Required/default values here document the CLI contract; command parsers still apply runtime defaults.
/// </summary>
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

/// <summary>
/// Describes one Sqloom verb across dispatch metadata, target binding, help output, and command docs.
/// </summary>
internal sealed record CommandSpec(
    HostCommandKind Kind,
    string Verb,
    string Description,
    CommandTargetKind TargetKind,
    bool SupportsDebug,
    bool SupportsHarnessOptions,
    IReadOnlyList<CommandOptionSpec> Options,
    IReadOnlyList<string> Notes)
{
    public string Usage
    {
        get
        {
            // Keep usage compact: show target shape and required switches, then collapse everything else.
            List<string> parts = [];
            parts.Add(Verb);
            if (TargetKind == CommandTargetKind.Optional)
            {
                parts.Add("[<path>]");
            }
            else if (TargetKind == CommandTargetKind.Required)
            {
                parts.Add("<path>");
            }

            parts.AddRange(Options
                .Where(static option => option.IsRequired)
                .Select(static option => option.Syntax));
            if (SupportsDebug
                || SupportsHarnessOptions
                || Options.Any(static option => !option.IsRequired))
            {
                parts.Add("[options]");
            }

            return string.Join(' ', parts);
        }
    }
}

internal static class CommandCatalog
{
    public static IReadOnlyList<CommandOptionSpec> ToolOptions { get; } =
    [
        new("--help", "Show command line help."),
        new("--version", "Display Sqloom version."),
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
            "Scaffolds the sqloom agent skill in a Git repository.",
            CommandTargetKind.None,
            SupportsDebug: false,
            SupportsHarnessOptions: false,
            [
                new("--agent", "Selects the agent skill location. Allowed values: codex, claude, copilot, all.", "codex|claude|copilot|all", DefaultValue: "codex"),
                new("--overwrite", "Replaces changed scaffolded skill files."),
            ],
            [
                "Run init from a Git repository root. Sqloom writes the selected agent skill under the matching agent-specific skill directory.",
            ]),
        new(
            HostCommandKind.Observe,
            "observe",
            "Collects SQL Server Query Store evidence and workload classification.",
            // Observe can run against Query Store without a harness; a target adds manifest data for classification.
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
            ],
            [
                "--app-only implies classification display and filters the console view to App-classified queries when the selected harness supplies Query Store profile data.",
                "When a target path is supplied, Sqloom resolves it, builds harness projects unless --no-build is supplied, and requires exactly one public non-abstract ISqloomApplication implementation.",
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
                new("--sqlserver-dacpac-file", "Overrides the DACPAC schema source used by replay launch options and advice extraction.", "path"),
                new("--sqlserver-seed-sql-file", "Passes a SQL seed script path to custom harness replay launch options.", "path"),
                new("--artifact-dir", "Uses a custom tune workflow root.", "path", DefaultValue: "artifacts/sqloom/tune/tune-<timestamp>"),
                new("--max-operations", "Caps replayed operations after filtering.", "count", DefaultValue: "25"),
                new("--target", "Replays one exact operation in the form METHOD /path/template.", "METHOD /path/template"),
                new("--replay-data-agent", "Controls replay request data preparation. Allowed values: off, auto, required.", "off|auto|required", DefaultValue: "required"),
                new("--replay-data-agent-model", "Selects the replay data agent model.", "id", DefaultValue: "gpt-5.4-mini"),
                new("--model-provider", "Selects the advice provider. The supported value is openai.", "openai", IsRequired: true),
                new("--sqlserver-schema-file", "Uses manually supplied schema SQL instead of DACPAC extraction.", "path"),
                new("--openai-model", "Selects the OpenAI advice model.", "id", DefaultValue: "gpt-5.4-mini"),
                new("--openai-base-url", "Sets the OpenAI base URL.", "url", DefaultValue: "https://api.openai.com"),
                new("--openai-api-key", "Supplies the OpenAI API key used by enabled agent workflows.", "key", IsRequired: true),
            ],
            [
                "Tune starts the harness session, runs replay -> observe -> correlate -> advise in one command, and disposes the session.",
                "Tune writes query-store-snapshot.json and tune-summary.json at the workflow root, then replay, correlation, and advice artifacts under the workflow replay/ directory.",
                "Tune uses --read-only-connection-string when supplied, otherwise it uses the harness session connection string.",
                "When no DACPAC override or harness manifest DACPAC is available, tune exports a DACPAC from the command-line read-only connection before replay and reuses it for advice schema extraction.",
                "By default, Microsoft Agent Framework fills missing replay path, query, header, and body values; pass --replay-data-agent off to opt out.",
                "--sqlserver-dacpac-file and --sqlserver-seed-sql-file are harness replay launch overrides; the replay data agent does not generate DACPACs or seed SQL.",
                "When omitted, --artifact-dir defaults to artifacts/sqloom/tune/tune-<timestamp>. With tune, --artifact-dir means the workflow root, not a replay-only directory.",
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
                new("--sqlserver-dacpac-file", "Passes a DACPAC path to harness replay launch options.", "path"),
                new("--sqlserver-seed-sql-file", "Passes a SQL seed script path to harness replay launch options.", "path"),
                new("--artifact-dir", "Uses a custom replay output directory.", "path", DefaultValue: "artifacts/sqloom/replay/<timestamp>"),
                new("--max-operations", "Caps replayed operations after filtering.", "count", DefaultValue: "25"),
                new("--target", "Replays one exact operation in the form METHOD /path/template.", "METHOD /path/template"),
                new("--replay-data-agent", "Controls replay request data preparation. Allowed values: off, auto, required.", "off|auto|required", DefaultValue: "required"),
                new("--replay-data-agent-model", "Selects the replay data agent model.", "id", DefaultValue: "gpt-5.4-mini"),
                new("--openai-base-url", "Sets the OpenAI base URL for the replay data agent.", "url", DefaultValue: "https://api.openai.com"),
                new("--openai-api-key", "Supplies the OpenAI API key used by the replay data agent.", "key"),
            ],
            [
                "Standalone replay requires an explicit target path after the replay verb. Supported target paths are harness project files, harness assemblies, solution files, solution filters, and directories.",
                "Sqloom resolves that target, builds harness projects unless --no-build is supplied, and requires exactly one public non-abstract ISqloomApplication implementation.",
                "Pass --dotnet-command <command> when Sqloom should use a non-default dotnet executable for nested project resolution and builds.",
                "If a solution, solution filter, or directory resolves to zero or multiple ISqloomApplication implementations, Sqloom fails and asks for a narrower target.",
                "SQL Server-backed replay harnesses can consume app-owned DACPAC and seed launch options when they implement that setup.",
                "The replay data agent fills HTTP replay inputs only; it does not generate DACPACs or seed SQL.",
                "The replay data agent is replay-only. It defaults to required, uses Microsoft Agent Framework, requires --openai-api-key unless --replay-data-agent off is supplied, and writes replay-data-prep.json.",
                "Replay targets must use the exact form 'METHOD /path/template', for example --target \"GET /api/expenses/dashboard\".",
                "Replay defaults to authenticated GET operations plus any app overlays enabled by default. Opt-in operations such as POST /api/advisor/query require explicit --target selection.",
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
            ],
            [
                "Correlation resolves statement_sql_handle against captured replay SQL, then writes query-store-correlation.json under the replay artifact directory by default.",
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
            ],
            [
                "Advice derives operation-level tuning guidance from query-store-correlation.json plus SQL Server schema extracted from a DACPAC.",
                "Advice writes tuning-advice.json, sql-tuning-proposal.json, and sql-tuning-proposal.sql under the replay artifact directory by default.",
                "OpenAI advice requires --model-provider openai, --openai-api-key, and a schema source: --sqlserver-schema-file, --sqlserver-dacpac-file, or --read-only-connection-string.",
                "Use --debug to print per-stage diagnostics to stderr. With advise, debug prints the redacted OpenAI request and response payloads.",
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
