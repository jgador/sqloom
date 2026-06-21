# sqloom

`sqloom` is a .NET CLI for Sqloom replay, Query Store correlation, and tuning-advice workflows. Advice now requires `--model-provider openai`, `--openai-*`, and `--sqlserver-schema-file <path>`. The `advise` stage reads `query-store-correlation.json`, sends replay and Query Store evidence plus the supplied schema file to OpenAI without preloading a local fix, then writes `tuning-advice.json` and replay-scoped `sql-tuning-proposal.json` and `sql-tuning-proposal.sql` sidecars.

Query Store capture and correlation use `--read-only-connection-string <connection-string>` explicitly. OpenAI advice uses explicit `--openai-*` arguments, runtime target resolution can override its nested dotnet executable with `--dotnet-command <command>`, and the active tool surface does not use environment-variable fallbacks for those inputs.

Use the global `--debug` switch when you want stage-owned diagnostics on `stderr`. In particular, `advise --debug` prints readable, redacted OpenAI request and response payloads, and `tune --debug` cascades the same debug mode through `observe`, `replay`, `correlate`, and `advise`.

SQL Server-backed replay harnesses can require a prebuilt DACPAC via `--sqlserver-dacpac-file <path>`. Sqloom consumes that DACPAC as app-owned input and does not build it. App-owned harnesses can also accept a post-DACPAC SQL seed script via `--sqlserver-seed-sql-file <path>` when they need to restore data into the fresh replay database after publish.

In phase 1, the proposal sidecars are SQL Server-oriented review artifacts derived from replay and Query Store evidence already captured for the run plus the supplied schema file. OpenAI proposal kinds are preserved as model-provided free-form strings, and every model proposal that deserializes successfully is persisted into `tuning-advice.json`, `sql-tuning-proposal.json`, and `sql-tuning-proposal.sql`. Rollback SQL is recommended but optional: if the model omits `rollbackSqlScript`, Sqloom keeps the proposal, records a warning, and renders a placeholder rollback note in the `.sql` sidecar. Sqloom no longer synthesizes deterministic local SQL proposals from Query Store correlation alone.

## Local install

Pack the required `Sqloom.*` packages into one local folder feed, then install the tool from that feed:

```powershell
dotnet tool install --tool-path <tool-path> sqloom --add-source <local-feed-path> --ignore-failed-sources
```

The tool package depends on `Sqloom.Core`, `Sqloom.QueryStore`, `Sqloom.AzureSql`, and `Sqloom.AspNetCore`, so those packages must be present in the same feed for local installs. The same dependency set must be published to any public feed before `sqloom` can be installed from it.

## Example

```powershell
& "<tool-path>\\sqloom.exe" replay .\Talio.sln --no-build --sqlserver-dacpac-file .\artifacts\Talio.dacpac --target "GET /api/expenses/dashboard"
```

Repository metadata currently points at the future standalone Sqloom project:

`https://github.com/jgador/sqloom`
