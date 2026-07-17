# Sqloom Command Reference

Use this file as the concise Sqloom CLI reference for coding agents.

## Agent use

- Use this file for exact command syntax, required options, option names, allowed values, defaults, and command-specific notes.
- Prefer `sqloom tune` for the full replay -> observe -> correlate -> advise workflow; use individual commands for focused stage work.
- Treat `$env:OPENAI_API_KEY` examples as shell expansion into `--openai-api-key`; Sqloom does not automatically read that environment variable.
- When validating an installed tool, run `sqloom help <command>` and compare the command surface against this reference.

## Usage

```text
sqloom [tool-options]
sqloom <command> [arguments] [options]
sqloom help [command]
```

## Tool options

| Option | Description |
| --- | --- |
| `--help` | Show command line help. |
| `--version` | Display Sqloom version. |

## Commands

| Command | Description |
| --- | --- |
| `init` | Scaffolds the sqloom agent skill in a Git repository. |
| `observe` | Collects SQL Server Query Store evidence and workload classification. |
| `endpoints` | Discovers ASP.NET Core controller endpoints from source. |
| `tune` | Runs replay, observe, correlate, and advise as one workflow. |
| `replay` | Starts the harness and replays selected endpoint operations. |
| `correlate` | Correlates replay SQL with a Query Store snapshot. |
| `advise` | Generates evidence-backed tuning advice and SQL proposals. |

### `init`

Scaffolds the sqloom agent skill in a Git repository.

```text
sqloom init [options]
```

#### Options

| Option | Description |
| --- | --- |
| `--agent <codex\|claude\|copilot\|all>` | Selects the agent skill location. Allowed values: codex, claude, copilot, all. Default: `codex`. |
| `--overwrite` | Replaces changed scaffolded skill files. |

#### Agent notes

- Run init from a Git repository root. Sqloom writes the selected agent skill under the matching agent-specific skill directory.

### `observe`

Collects SQL Server Query Store evidence and workload classification.

```text
sqloom observe [<path>] --read-only-connection-string <connection-string> [options]
```

| Argument | Description |
| --- | --- |
| `[<path>]` | Optional C# file-based harness, harness project, harness assembly, solution, solution filter, or directory. |

#### Startup options

| Option | Description |
| --- | --- |
| `--debug` | Prints per-stage diagnostics to stderr. |
| `--dotnet-command <command>` | Uses a specific dotnet executable for project resolution and C# file-based harness builds. Default: `dotnet`. |
| `--no-build` | Skips building harness projects before scanning their outputs. Sqloom always builds .cs harness targets and rejects --no-build for them. |

#### Required options

| Option | Description |
| --- | --- |
| `--read-only-connection-string <connection-string>` | SQL Server or Azure SQL connection string used to read Query Store and metadata. Required. |

#### Options

| Option | Description |
| --- | --- |
| `--lookback-hours <hours>` | Query Store lookback window in hours. Default: `24`. |
| `--max-plans <count>` | Maximum Query Store plans to capture before console filtering. Default: `100`. |
| `--max-waits <count>` | Maximum Query Store waits to capture. Default: `10`. |
| `--command-timeout-seconds <seconds>` | SQL command timeout for Query Store reads. Default: `30`. |
| `--json-output-file <path>` | Writes the Query Store snapshot to a specific JSON path. |
| `--app-only` | Filters the console view to App-classified entries and implies --show-classification. |
| `--show-classification` | Prints classification details for displayed plans and waits. |

#### Agent notes

- --app-only implies classification display and filters the console view to App-classified queries when the selected harness supplies Query Store profile data.
- When a target path is supplied, Sqloom resolves it, builds harness projects unless --no-build is supplied, always builds C# file-based harnesses, and requires exactly one public non-abstract ISqloomApplication implementation.

### `endpoints`

Discovers ASP.NET Core controller endpoints from source.

```text
sqloom endpoints <path> [options]
```

| Argument | Description |
| --- | --- |
| `<path>` | C# file-based harness, harness project, harness assembly, solution, solution filter, or directory. |

#### Startup options

| Option | Description |
| --- | --- |
| `--debug` | Prints per-stage diagnostics to stderr. |

#### Options

| Option | Description |
| --- | --- |
| `--app-project <path>` | Overrides the inferred ASP.NET Core source project used for endpoint discovery. |
| `--json-output-file <path>` | Writes discovered endpoint operations to a specific JSON path. |

#### Agent notes

- Endpoints does not start the app harness or replay requests; it only loads source metadata with Roslyn.
- File-based harness targets infer the source project from exactly one Web SDK #:project directive unless --app-project is supplied.
- A direct ASP.NET Core project target can be used to list endpoints before a Sqloom harness exists.

### `tune`

Runs replay, observe, correlate, and advise as one workflow.

```text
sqloom tune <path> --model-provider <openai> --openai-api-key <key> [options]
```

| Argument | Description |
| --- | --- |
| `<path>` | C# file-based harness, harness project, harness assembly, solution, solution filter, or directory. |

#### Startup options

| Option | Description |
| --- | --- |
| `--debug` | Prints per-stage diagnostics to stderr. |
| `--dotnet-command <command>` | Uses a specific dotnet executable for project resolution and C# file-based harness builds. Default: `dotnet`. |
| `--no-build` | Skips building harness projects before scanning their outputs. Sqloom always builds .cs harness targets and rejects --no-build for them. |

#### Required options

| Option | Description |
| --- | --- |
| `--model-provider <openai>` | Selects the advice provider. The supported value is openai. Required. |
| `--openai-api-key <key>` | Supplies the OpenAI API key used by enabled agent workflows. Required. |

#### Options

| Option | Description |
| --- | --- |
| `--read-only-connection-string <connection-string>` | Overrides the harness session connection used for Query Store and schema export. |
| `--lookback-hours <hours>` | Query Store lookback window in hours. Default: `24`. |
| `--max-plans <count>` | Maximum Query Store plans to capture before console filtering. Default: `100`. |
| `--max-waits <count>` | Maximum Query Store waits to capture. Default: `10`. |
| `--command-timeout-seconds <seconds>` | SQL command timeout for Query Store reads. Default: `30`. |
| `--app-only` | Filters the console view to App-classified entries and implies --show-classification. |
| `--show-classification` | Prints classification details for displayed plans and waits. |
| `--app-project <path>` | Overrides the inferred ASP.NET Core source project used for endpoint discovery. |
| `--sqlserver-dacpac-file <path>` | Overrides the DACPAC schema source used by replay launch options and advice extraction. |
| `--sqlserver-seed-sql-file <path>` | Passes a SQL seed script path to custom harness replay launch options. |
| `--artifact-dir <path>` | Uses a custom tune workflow root. Default: `artifacts/sqloom/tune/tune-<timestamp>`. |
| `--max-operations <count>` | Caps replayed operations after filtering. Default: `25`. |
| `--target <METHOD /path/template>` | Replays one exact operation in the form METHOD /path/template. |
| `--replay-data-agent <off\|auto\|required>` | Controls replay request data preparation. Allowed values: off, auto, required. Default: `required`. |
| `--replay-data-agent-model <id>` | Selects the replay data agent model. Default: `gpt-5.4-mini`. |
| `--sqlserver-schema-file <path>` | Uses manually supplied schema SQL instead of DACPAC extraction. |
| `--openai-model <id>` | Selects the OpenAI advice model. Default: `gpt-5.4-mini`. |
| `--openai-base-url <url>` | Sets the OpenAI base URL. Default: `https://api.openai.com`. |

#### Agent notes

- Tune starts the harness session, runs replay -> observe -> correlate -> advise in one command, and disposes the session.
- Tune writes query-store-snapshot.json and tune-summary.json at the workflow root, then replay, correlation, and advice artifacts under the workflow replay/ directory.
- Tune discovers replay operations from the ASP.NET Core source project inferred from the harness target or --app-project.
- Tune uses --read-only-connection-string when supplied, otherwise it uses the harness session connection string.
- When no DACPAC override or harness manifest DACPAC is available, tune exports a DACPAC from the command-line read-only connection before replay and reuses it for advice schema extraction.
- By default, Microsoft Agent Framework fills missing replay path, query, header, and body values; pass --replay-data-agent off to opt out.
- --sqlserver-dacpac-file and --sqlserver-seed-sql-file are harness replay launch overrides; the replay data agent does not generate DACPACs or seed SQL.
- When omitted, --artifact-dir defaults to artifacts/sqloom/tune/tune-<timestamp>. With tune, --artifact-dir means the workflow root, not a replay-only directory.

### `replay`

Starts the harness and replays selected endpoint operations.

```text
sqloom replay <path> [options]
```

| Argument | Description |
| --- | --- |
| `<path>` | C# file-based harness, harness project, harness assembly, solution, solution filter, or directory. |

#### Startup options

| Option | Description |
| --- | --- |
| `--debug` | Prints per-stage diagnostics to stderr. |
| `--dotnet-command <command>` | Uses a specific dotnet executable for project resolution and C# file-based harness builds. Default: `dotnet`. |
| `--no-build` | Skips building harness projects before scanning their outputs. Sqloom always builds .cs harness targets and rejects --no-build for them. |

#### Options

| Option | Description |
| --- | --- |
| `--app-project <path>` | Overrides the inferred ASP.NET Core source project used for endpoint discovery. |
| `--sqlserver-dacpac-file <path>` | Passes a DACPAC path to harness replay launch options. |
| `--sqlserver-seed-sql-file <path>` | Passes a SQL seed script path to harness replay launch options. |
| `--artifact-dir <path>` | Uses a custom replay output directory. Default: `artifacts/sqloom/replay/<timestamp>`. |
| `--max-operations <count>` | Caps replayed operations after filtering. Default: `25`. |
| `--target <METHOD /path/template>` | Replays one exact operation in the form METHOD /path/template. |
| `--replay-data-agent <off\|auto\|required>` | Controls replay request data preparation. Allowed values: off, auto, required. Default: `required`. |
| `--replay-data-agent-model <id>` | Selects the replay data agent model. Default: `gpt-5.4-mini`. |
| `--openai-base-url <url>` | Sets the OpenAI base URL for the replay data agent. Default: `https://api.openai.com`. |
| `--openai-api-key <key>` | Supplies the OpenAI API key used by the replay data agent. |

#### Agent notes

- Standalone replay requires an explicit target path after the replay verb. Supported target paths are C# file-based harnesses, harness project files, harness assemblies, solution files, solution filters, and directories.
- Replay discovers endpoint operations from the ASP.NET Core source project inferred from the harness target or --app-project.
- Sqloom resolves that target, builds harness projects unless --no-build is supplied, always builds C# file-based harnesses, and requires exactly one public non-abstract ISqloomApplication implementation.
- Pass --dotnet-command <command> when Sqloom should use a non-default dotnet executable for nested project resolution and C# file-based harness builds.
- C# file-based harness targets require .NET SDK 10 or later and reject --no-build.
- If a solution, solution filter, or directory resolves to zero or multiple ISqloomApplication implementations, Sqloom fails and asks for a narrower target.
- SQL Server-backed replay harnesses can consume app-owned DACPAC and seed launch options when they implement that setup.
- The replay data agent fills HTTP replay inputs only; it does not generate DACPACs or seed SQL.
- The replay data agent is replay-only. It defaults to required, uses Microsoft Agent Framework, requires --openai-api-key unless --replay-data-agent off is supplied, and writes replay-data-prep.json.
- Replay targets must use the exact form 'METHOD /path/template', for example --target "GET /api/expenses/dashboard".
- Replay defaults to authenticated GET operations plus any app overlays enabled by default. Opt-in operations such as POST /api/advisor/query require explicit --target selection.

### `correlate`

Correlates replay SQL with a Query Store snapshot.

```text
sqloom correlate --replay-artifact-dir <path> --query-store-snapshot-file <path> --read-only-connection-string <connection-string> [options]
```

#### Startup options

| Option | Description |
| --- | --- |
| `--debug` | Prints per-stage diagnostics to stderr. |

#### Required options

| Option | Description |
| --- | --- |
| `--replay-artifact-dir <path>` | Replay output directory containing operation artifacts. Required. |
| `--query-store-snapshot-file <path>` | Query Store snapshot JSON produced by observe. Required. |
| `--read-only-connection-string <connection-string>` | Connection string used for statement handle resolution. Required. |

#### Options

| Option | Description |
| --- | --- |
| `--json-output-file <path>` | Writes correlation to a specific JSON path. |

#### Agent notes

- Correlation resolves statement_sql_handle against captured replay SQL, then writes query-store-correlation.json under the replay artifact directory by default.

### `advise`

Generates evidence-backed tuning advice and SQL proposals.

```text
sqloom advise --replay-artifact-dir <path> --model-provider <openai> --openai-api-key <key> [options]
```

#### Startup options

| Option | Description |
| --- | --- |
| `--debug` | Prints per-stage diagnostics to stderr. |

#### Required options

| Option | Description |
| --- | --- |
| `--replay-artifact-dir <path>` | Replay output directory containing correlation evidence. Required. |
| `--model-provider <openai>` | Selects the advice provider. The supported value is openai. Required. |
| `--openai-api-key <key>` | Supplies the OpenAI API key used by advice generation. Required. |

#### Options

| Option | Description |
| --- | --- |
| `--query-store-correlation-file <path>` | Uses a correlation file outside the replay output directory. |
| `--read-only-connection-string <connection-string>` | Exports a schema-source DACPAC when no schema file or DACPAC is supplied. |
| `--sqlserver-schema-file <path>` | Uses manually supplied schema SQL instead of DACPAC extraction. |
| `--sqlserver-dacpac-file <path>` | Extracts SQL Server schema from a DACPAC. |
| `--json-output-file <path>` | Writes advice to a specific JSON path. |
| `--openai-model <id>` | Selects the OpenAI advice model. Default: `gpt-5.4-mini`. |
| `--openai-base-url <url>` | Sets the OpenAI base URL. Default: `https://api.openai.com`. |

#### Agent notes

- Advice derives operation-level tuning guidance from query-store-correlation.json plus SQL Server schema extracted from a DACPAC.
- Advice writes tuning-advice.json, sql-tuning-proposal.json, and sql-tuning-proposal.sql under the replay artifact directory by default.
- OpenAI advice requires --model-provider openai, --openai-api-key, and a schema source: --sqlserver-schema-file, --sqlserver-dacpac-file, or --read-only-connection-string.
- Use --debug to print per-stage diagnostics to stderr. With advise, debug prints the redacted OpenAI request and response payloads.

