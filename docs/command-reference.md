# Sqloom Command Reference

This is the detailed CLI reference for `sqloom`. For the shortest path, start with `sqloom tune` in [README.md](../README.md).

## Command Overview

```text
sqloom help
sqloom help <command>
sqloom --help
sqloom <command> --help
sqloom --version
sqloom init [options]
sqloom observe [<path>] --read-only-connection-string <connection-string> [options]
sqloom tune <path> --model-provider openai --openai-api-key <key> [options]
sqloom replay <path> [options]
sqloom correlate --replay-artifact-dir <path> --query-store-snapshot-file <path> --read-only-connection-string <connection-string> [options]
sqloom advise --replay-artifact-dir <path> --model-provider openai --openai-api-key <key> [options]
```

`<path>` can be a harness project, harness assembly, solution, solution filter, or directory containing harness projects. Sqloom builds harness projects unless `--no-build` is supplied, scans loadable assemblies for public non-abstract `ISqloomApplication` implementations, and requires exactly one match. `init` is target-independent and does not resolve a harness.

## Global And Startup Options

| Option | Applies to | Description |
| --- | --- | --- |
| `help` | tool | Prints usage. |
| `help <command>` | tool | Prints command-specific help. |
| `--help` | tool | Prints usage. |
| `<command> --help` | tool | Prints command-specific help without resolving a harness. |
| `--version` | tool | Prints the installed Sqloom tool version. |
| `--debug` | all commands | Prints stage diagnostics to `stderr`. For `advise`, this includes readable, redacted OpenAI request and response payloads. For `tune`, this cascades through `replay`, `observe`, `correlate`, and `advise`. |
| `--dotnet-command <command>` | commands that resolve harness projects | Uses a specific `dotnet` executable for nested project resolution and builds. |
| `--no-build` | commands that resolve harness projects | Skips building harness projects before scanning their outputs. |

## `init`

Use `init` from a Git repository root to scaffold Sqloom's embedded `sqloom` agent skill. The command requires the current directory to contain a `.git` directory or file.

```powershell
sqloom init
sqloom init --agent claude
sqloom init --agent copilot
sqloom init --agent all
```

Default agent: `codex`.

| Agent | Target root |
| --- | --- |
| `codex` | `.agents/skills/sqloom/` |
| `claude` | `.claude/skills/sqloom/` |
| `copilot` | `.github/skills/sqloom/` |
| `all` | all target roots |

`init` creates missing directories, leaves identical files unchanged, and refuses to overwrite changed existing files unless `--overwrite` is supplied.

## `tune`

Use `tune` for the normal end-to-end workflow. It starts the harness session, runs `replay -> observe -> correlate -> advise`, disposes the session, and writes workflow artifacts.

```powershell
sqloom tune .\tests\Sqloom.TestApp.Harness\Sqloom.TestApp.Harness.csproj `
  --target "GET /api/products/by-category" `
  --read-only-connection-string "Server=localhost;Database=AdventureWorksLT2025;Integrated Security=True;TrustServerCertificate=True;MultipleActiveResultSets=True" `
  --model-provider openai `
  --openai-api-key $env:OPENAI_API_KEY `
  --openai-model "gpt-5.5" `
  --debug
```

### Required Arguments

| Argument | Description |
| --- | --- |
| `<path>` | Harness project, harness assembly, solution, solution filter, or directory. |
| `--model-provider openai` | Selects the OpenAI-backed advice provider. |
| `--openai-api-key <key>` | API key used by the OpenAI advice step. The CLI does not read this from the environment automatically. |

`--read-only-connection-string <connection-string>` is optional for `tune` when the harness session supplies a read-only connection string. If supplied, it overrides the harness session connection string.

### Options

| Option | Description |
| --- | --- |
| `--read-only-connection-string <connection-string>` | Query Store connection for `observe` and `correlate`; overrides the harness session connection string. |
| `--lookback-hours <hours>` | Query Store lookback window. Default: `24`. |
| `--max-plans <count>` | Maximum Query Store plans to capture before console filtering. Default: `100`. |
| `--max-waits <count>` | Maximum Query Store waits to capture. Default: `10`. |
| `--command-timeout-seconds <seconds>` | SQL command timeout for Query Store reads. Default: `30`. |
| `--app-only` | Filters the console view to App-classified Query Store entries and implies `--show-classification`. |
| `--show-classification` | Prints classification kind, confidence, and reasons for displayed plans and waits. |
| `--openapi-file <path>` | Overrides the app-owned OpenAPI document for this run. |
| `--sqlserver-dacpac-file <path>` | Overrides the SQL Server DACPAC schema source when `--sqlserver-schema-file` is not supplied. If tune has no DACPAC override or harness manifest DACPAC and the command line supplies `--read-only-connection-string`, Sqloom exports a DACPAC from that connection before replay and reuses it for advice. |
| `--sqlserver-seed-sql-file <path>` | Passes a SQL seed script path to custom harnesses that implement post-DACPAC seed behavior. |
| `--artifact-dir <path>` | Uses a custom workflow root. Default: `artifacts/sqloom/tune/tune-<timestamp>`. |
| `--max-operations <count>` | Caps replayed operations after filtering. Default: `25`. |
| `--target "METHOD /path/template"` | Replays one exact operation. The method must be uppercase, with one space before a leading-slash route and no trailing slash. |
| `--replay-data-agent off\|auto\|required` | Controls replay-only HTTP request data preparation. Default: `required`. `auto` and `required` use Microsoft Agent Framework with OpenAI and require `--openai-api-key`. The agent fills path/query/header/body values; it does not generate DACPACs or seed SQL. `off` is the only non-agent mode. |
| `--replay-data-agent-model <id>` | Model id for the replay data agent. Default: `gpt-5.4-mini`. This is separate from the advice `--openai-model`. |
| `--sqlserver-schema-file <path>` | Expert override for manually supplied schema SQL. This wins over DACPAC extraction. |
| `--openai-model <id>` | OpenAI model id. Default: `gpt-5.4-mini`. |
| `--openai-base-url <url>` | OpenAI base URL. Default: `https://api.openai.com`. |

### Outputs

Without `--artifact-dir`, `tune` writes under `artifacts/sqloom/tune/tune-<timestamp>/`:

- `query-store-snapshot.json`
- `tune-summary.json`
- `replay/discovered-operations.json`
- `replay/replay-plan.json`
- `replay/replay-data-prep.json` unless `--replay-data-agent off` is supplied
- `replay/replay-summary.json`
- `replay/operations/<ordinal>-<operation>.json`
- `replay/query-store-correlation.json`
- `replay/sqlserver-schema-source.dacpac` when advice exports the schema source from the read-only connection
- `replay/sqlserver-dacpac-extract/model.sql` when schema is extracted from a DACPAC
- `replay/sqlserver-schema.sql`
- `replay/tuning-advice.json`
- `replay/sql-tuning-proposal.json`
- `replay/sql-tuning-proposal.sql`

## `replay`

Use `replay` when you only want to execute API operations through a harness and capture the SQL they cause.

```powershell
sqloom replay .\tests\Sqloom.TestApp.Harness\Sqloom.TestApp.Harness.csproj `
  --target "GET /api/products/by-category" `
  --openai-api-key $env:OPENAI_API_KEY
```

### Required Arguments

| Argument | Description |
| --- | --- |
| `<path>` | Harness project, harness assembly, solution, solution filter, or directory. |

### Options

| Option | Description |
| --- | --- |
| `--openapi-file <path>` | Uses a different OpenAPI document instead of the harness manifest default. |
| `--sqlserver-dacpac-file <path>` | Passes a DACPAC path to the harness replay launch options. Sqloom does not build the DACPAC during `replay`. |
| `--sqlserver-seed-sql-file <path>` | Passes a SQL seed script path to the harness replay launch options. Requires `--sqlserver-dacpac-file`. |
| `--artifact-dir <path>` | Uses a custom replay output folder. Default: `artifacts/sqloom/replay/<timestamp>`. |
| `--max-operations <count>` | Caps replayed operations after filtering. Default: `25`. |
| `--target "METHOD /path/template"` | Replays one exact operation such as `GET /api/products/by-category`. |
| `--replay-data-agent off\|auto\|required` | Controls replay-only request data preparation. Default: `required`. `auto` and `required` use Microsoft Agent Framework with OpenAI and require `--openai-api-key`. `off` is the only non-agent mode. |
| `--replay-data-agent-model <id>` | Model id for the replay data agent. Default: `gpt-5.4-mini`. |
| `--openai-api-key <key>` | API key used when the replay data agent calls Microsoft Agent Framework. Required unless `--replay-data-agent off` is supplied. |
| `--openai-base-url <url>` | OpenAI base URL for the replay data agent. Default: `https://api.openai.com`. |

Sqloom replays authenticated `GET` operations by default plus app overlays enabled by default. Non-`GET` operations must be allowed by the app through replay overlays and selected when needed.

### Outputs

- `discovered-operations.json`
- `replay-plan.json`
- `replay-data-prep.json` unless `--replay-data-agent off` is supplied
- `replay-summary.json`
- `operations/<ordinal>-<operation>.json`

Each operation file records the request, response, captured SQL commands, normalized SQL text, fingerprints, and parameter values.

## `observe`

Use `observe` to read recent Query Store data from SQL Server or Azure SQL.

```powershell
sqloom observe .\tests\Sqloom.TestApp.Harness\Sqloom.TestApp.Harness.csproj `
  --read-only-connection-string "Server=localhost;Database=AdventureWorksLT2025;Integrated Security=True;TrustServerCertificate=True;MultipleActiveResultSets=True" `
  --lookback-hours 24 `
  --max-plans 100 `
  --max-waits 10 `
  --app-only `
  --show-classification
```

### Required Arguments

| Argument | Description |
| --- | --- |
| `--read-only-connection-string <connection-string>` | SQL Server or Azure SQL connection string used to read Query Store and metadata. |

`<path>` is optional. When supplied, Sqloom uses the harness manifest workload profile while classifying Query Store rows.

### Options

| Option | Description |
| --- | --- |
| `--lookback-hours <hours>` | Query Store lookback window. Default: `24`. |
| `--max-plans <count>` | Maximum Query Store plans to capture before console filtering. Default: `100`. |
| `--max-waits <count>` | Maximum Query Store waits to capture. Default: `10`. |
| `--command-timeout-seconds <seconds>` | SQL command timeout. Default: `30`. |
| `--json-output-file <path>` | Writes the snapshot to a specific JSON path. Default: timestamped file under `artifacts/sqloom/query-store/`. |
| `--app-only` | Filters the console view to App-classified entries and implies `--show-classification`. The JSON snapshot still keeps the full captured set. |
| `--show-classification` | Prints classification kind, confidence, and reasons for displayed plans and waits. |

Sqloom reads database objects through the same read-only connection. Tables and views are included by default; stored procedures and functions require `VIEW DEFINITION`.

## `correlate`

Use `correlate` after `replay` and `observe` when you want to match replay-captured SQL back to Query Store.

```powershell
sqloom correlate `
  --replay-artifact-dir ".\artifacts\sqloom\replay\replay-20260608T040506000Z" `
  --query-store-snapshot-file ".\artifacts\sqloom\query-store\query-store-20260608T041245123Z.json" `
  --read-only-connection-string "<connection-string>"
```

### Required Arguments

| Argument | Description |
| --- | --- |
| `--replay-artifact-dir <path>` | Replay output folder containing replay operation artifacts. |
| `--query-store-snapshot-file <path>` | Query Store snapshot JSON produced by `observe`. |
| `--read-only-connection-string <connection-string>` | SQL Server or Azure SQL connection string used for statement handle resolution. |

### Options

| Option | Description |
| --- | --- |
| `--json-output-file <path>` | Writes correlation to a specific JSON path. Default: `query-store-correlation.json` under the replay artifact directory. |

Sqloom ranks matches as:

- `StatementHandleExact`
- `QueryTextExact`
- `FingerprintFallback`
- `Unmatched`

## `advise`

Use `advise` after `replay` and `correlate` when you want operation-level tuning advice and SQL proposal files.

```powershell
sqloom advise `
  --replay-artifact-dir ".\artifacts\sqloom\replay\replay-20260608T040506000Z" `
  --model-provider openai `
  --openai-api-key "<api-key>" `
  --read-only-connection-string "<connection-string>"
```

### Required Arguments

| Argument | Description |
| --- | --- |
| `--replay-artifact-dir <path>` | Replay output folder. By default this must contain `query-store-correlation.json`. |
| `--model-provider openai` | Selects the OpenAI-backed advice provider. |
| `--openai-api-key <key>` | API key used by the OpenAI advice step. |

Advice also needs a schema source. Supply `--sqlserver-schema-file <path>`, `--sqlserver-dacpac-file <path>`, or `--read-only-connection-string <connection-string>`. When only the read-only connection is supplied, Sqloom exports a DACPAC into the replay artifact directory and extracts schema from that generated package.

### Options

| Option | Description |
| --- | --- |
| `--query-store-correlation-file <path>` | Uses a correlation file outside the replay artifact directory. |
| `--json-output-file <path>` | Writes advice to a specific JSON path. Default: `tuning-advice.json` under the replay artifact directory. |
| `--sqlserver-dacpac-file <path>` | Extracts SQL Server schema SQL from a DACPAC, keeps the raw DacFx unpack under `sqlserver-dacpac-extract`, and writes `sqlserver-schema.sql` beside the advice artifacts. |
| `--read-only-connection-string <connection-string>` | Exports `sqlserver-schema-source.dacpac` from the target database when no schema file or DACPAC path is supplied, then extracts `sqlserver-schema.sql` from the exported DACPAC. |
| `--sqlserver-schema-file <path>` | Expert override for manually supplied schema SQL. This wins over DACPAC extraction. |
| `--openai-model <id>` | OpenAI model id. Default: `gpt-5.4-mini`. |
| `--openai-base-url <url>` | OpenAI base URL. Default: `https://api.openai.com`. |

### Outputs

By default, `advise` writes these files under the replay artifact directory:

- `tuning-advice.json`
- `sql-tuning-proposal.json`
- `sql-tuning-proposal.sql`
- `sqlserver-schema.sql` when schema is extracted from a DACPAC
- `sqlserver-dacpac-extract/model.sql` when schema is extracted from a DACPAC
- `sqlserver-schema-source.dacpac` when Sqloom exports a DACPAC from `--read-only-connection-string`

Rollback SQL is helpful but optional in the OpenAI path. If the model omits `rollbackSqlScript`, Sqloom keeps the proposal, records a warning, and writes a placeholder rollback note in the `.sql` file.

## Sample Harness

The sample harness at [tests/Sqloom.TestApp.Harness/Sqloom.TestApp.Harness.csproj](../tests/Sqloom.TestApp.Harness/Sqloom.TestApp.Harness.csproj) supplies:

- app name: `Sqloom Test App`
- OpenAPI document: [tests/Sqloom.TestApp/openapi.json](../tests/Sqloom.TestApp/openapi.json)
- sample target: `GET /api/products/by-category`

The repository does not check in an `AdventureWorksLT2025` DACPAC or seed SQL file. For the SQL-backed sample workflow, restore `AdventureWorksLT2025` on your local SQL Server and pass that database through `--read-only-connection-string`. Sqloom exports `sqlserver-schema-source.dacpac` and extracts `sqlserver-schema.sql` into the run artifacts.

```powershell
sqloom-local tune .\tests\Sqloom.TestApp.Harness\Sqloom.TestApp.Harness.csproj `
  --target "GET /api/products/by-category" `
  --read-only-connection-string "Server=localhost;Database=AdventureWorksLT2025;Integrated Security=True;TrustServerCertificate=True;MultipleActiveResultSets=True" `
  --model-provider openai `
  --openai-api-key $env:OPENAI_API_KEY
```

## SQL Server Permissions

Use a dedicated read-only database principal for Sqloom. It needs enough access to read Query Store, metadata, and showplans:

```sql
IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = N'sqloom_ro')
BEGIN
    CREATE USER [sqloom_ro] WITH PASSWORD = N'<strong-password>';
END;

IF NOT EXISTS (
    SELECT 1
    FROM sys.database_role_members AS drm
    INNER JOIN sys.database_principals AS roles
        ON roles.principal_id = drm.role_principal_id
    INNER JOIN sys.database_principals AS members
        ON members.principal_id = drm.member_principal_id
    WHERE roles.name = N'db_datareader'
        AND members.name = N'sqloom_ro')
BEGIN
    ALTER ROLE [db_datareader] ADD MEMBER [sqloom_ro];
END;

GRANT VIEW DATABASE PERFORMANCE STATE TO [sqloom_ro];
GRANT VIEW DEFINITION TO [sqloom_ro];
GRANT SHOWPLAN TO [sqloom_ro];
DENY INSERT TO [sqloom_ro];
DENY UPDATE TO [sqloom_ro];
DENY DELETE TO [sqloom_ro];
```

If the host reports that module discovery was skipped because `VIEW DEFINITION` is unavailable, grant that permission when you want stored procedures and functions included in app classification.

## Local Development Tool

Use `sqloom` for normal use. Use `sqloom-local` only when changing this repo:

```powershell
pwsh .\scripts\deploy-sqloom-local.ps1
sqloom-local --version
```

`sqloom-local` accepts the same commands and options as the packaged `sqloom` tool.
