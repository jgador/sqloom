# Sqloom

`Sqloom.sln` is the standalone Sqloom workspace. The active user-facing runner is `Sqloom.Host` or the packaged `sqloom` tool: a host-first CLI that resolves target paths such as `Sqloom.TestApp` or `Talio.Api` through companion integration libraries such as `Sqloom.TestApp.IntegrationTests` or `Talio.Sqloom`. Talio-owned Sqloom integration code lives in the Talio repository, not in this standalone repo.

`Sqloom.TestApp` also supports an optional post-DACPAC seed script through `--sqlserver-seed-sql-file <path>` on both `replay` and `sqloom-local tune`, used together with `--sqlserver-dacpac-file <path>`. Generate that script from localhost with `pwsh .\tests\Sqloom.TestApp.IntegrationTests\Export-AdventureWorksLT2025SeedSql.ps1` when you want the Testcontainer to mirror exported `AdventureWorksLT2025` data instead of the built-in sample seed.

## Highlight

The fastest way to demo the full tuning flow today is the sample app plus `sqloom-local tune`. That path exercises `observe -> replay -> correlate -> advise` and emits both the advice JSON and the generated SQL proposal script:

```powershell
if (-not $env:OPENAI_API_KEY) { throw "OPENAI_API_KEY is required." }

sqloom-local tune .\tests\Sqloom.TestApp\Sqloom.TestApp.csproj `
  --no-build `
  --sqlserver-dacpac-file .\tests\Sqloom.TestApp.IntegrationTests\AdventureWorksLT2025.dacpac `
  --sqlserver-seed-sql-file .\tests\Sqloom.TestApp.IntegrationTests\AdventureWorksLT2025.seed.sql `
  --sqlserver-schema-file .\tests\Sqloom.TestApp.IntegrationTests\AdventureWorksLT2025.schema.sql `
  --read-only-connection-string "Server=localhost;Database=AdventureWorksLT2025;Integrated Security=True;TrustServerCertificate=True;MultipleActiveResultSets=True" `
  --target "GET /api/products/by-category" `
  --model-provider openai `
  --openai-api-key $env:OPENAI_API_KEY `
  --openai-model "gpt-5.5"
```

The run writes artifacts under `artifacts/sqloom/tune/tune-<timestamp>/`, including:

- `query-store-snapshot.json`
- `tune-summary.json`
- `replay/tuning-advice.json`
- `replay/sql-tuning-proposal.json`
- `replay/sql-tuning-proposal.sql`

Add `--debug` to any `sqloom` or `sqloom-local` command when you want stage-owned diagnostics on `stderr`. `advise --debug` prints readable, redacted OpenAI request and response payloads, and `tune --debug` cascades debug output through `observe`, `replay`, `correlate`, and `advise`.

```text
.
|-- Sqloom.sln
|-- Sqloom.UnitTests.slnf
|-- Sqloom.IntegrationTests.slnf
|-- README.md
|-- src/
|   |-- Sqloom.Core/
|   |-- Sqloom.QueryStore/
|   |-- Sqloom.AzureSql/
|   |-- Sqloom.AspNetCore/
|   `-- Sqloom.Host/
`-- tests/
    |-- Directory.Build.props
    |-- Sqloom.TestApp/
    |-- Sqloom.TestApp.IntegrationTests/
    |-- Sqloom.UnitTests/
    `-- Sqloom.IntegrationTests/
```

Talio-owned companion harnesses such as `Talio.Sqloom` remain in the Talio repository.

## Architecture Shape

- High-level flow: `observe -> replay -> capture -> correlate -> advise`
- Convenience front door: `tune` runs `observe -> replay -> correlate -> advise` as one typed workflow while still emitting the same JSON and SQL artifacts.
- `Sqloom.Host`: generic CLI, tool wrapper, and runtime for target resolution, library-harness loading, and pipeline execution. This is the primary user-facing runner.
- `Sqloom.TestApp`: minimal sample target project used to validate companion-project host behavior without Talio-specific bootstrap.
- `Sqloom.TestApp.IntegrationTests`: sample library harness companion loaded by the host for generic coverage, including optional SQL Server DACPAC bootstrap.
- `Talio.Sqloom`: Talio-specific library harness in Talio's `backend/tests`, loaded by the host for workload definitions, personas, `WebApplicationFactory` hosting, and replay bootstrap.
- `Sqloom.UnitTests` and `Sqloom.IntegrationTests`: the generic Sqloom unit and host-integration lanes under `tests/`.
- `Talio.Sqloom.Tests`: the Talio-specific replay and correlation lane in Talio's `backend/tests`.
- Replay, correlation, and advice artifacts carry explicit pipeline stage metadata so each artifact records where it belongs in the flow.

## Responsibilities

- `Sqloom.Core`: shared run options, workload definitions, replay host and profile contracts, artifact layout, pipeline and advice report models, and the merged Showplan plus OpenAI advice contracts. The moved types keep their existing `Sqloom.Showplan.Plans` and `Sqloom.OpenAI.Advice` namespaces.
- `Sqloom.QueryStore`: Query Store observation contracts, discovered-object catalogs, and discovery-first workload classification that stay independent of SQL connectivity.
- `Sqloom.AzureSql`: readonly connection, user-object discovery, Query Store collection, statement-handle resolution, replay, and statistics-capture entry points for Azure SQL.
- `Sqloom.AspNetCore`: OpenAPI discovery, replay planning, resolved request generation, in-process ASP.NET Core replay orchestration, SQL capture for EF Core plus request-scoped capture middleware hooks, legacy heuristic tuning helpers over correlation artifacts, and the merged Query Store correlation types. The moved correlator types keep their existing `Sqloom.Correlation.QueryStore` namespaces.
- `Sqloom.Host`: standalone CLI, tool wrapper, and generic runtime that owns `HostRuntime`, path resolution, library-harness loading, and the command pipeline without referencing any concrete app.
- `Sqloom.TestApp`: synthetic sample target project kept as the user-facing path for generic host coverage.
- `Sqloom.TestApp.IntegrationTests`: sample companion integration library that owns the generic replay harness, replay profile, and optional SQL Server DACPAC bootstrap used by `Sqloom.TestApp`, `Sqloom.UnitTests`, and `Sqloom.IntegrationTests`.
- `Talio.Sqloom`: Talio-specific library harness in Talio's `backend/tests` for operation overlays, managed local SQL Server bootstrap, deterministic personas and setup, `WebApplicationFactory` hosting of `Talio.Api`, and the advisor raw-SQL bridge over the existing controllers and read/query services.
- `Sqloom.UnitTests`: consolidated unit-test lane for classifier, collector, replay planning, replay request resolution, correlator behavior, and standalone app-resolution coverage across the generic Sqloom libraries.
- `Sqloom.IntegrationTests`: consolidated integration-test lane for the standalone generic host surface, including process-level `Sqloom.Host` coverage against the sample app integration.
- `Talio.Sqloom.Tests`: Talio-owned replay and end-to-end Query Store correlation coverage in Talio's `backend/tests` for the sample app integration over Sqloom.

The retired runtime project boundaries are merged into adjacent survivors: `Sqloom.Showplan -> Sqloom.Core`, `Sqloom.OpenAI -> Sqloom.Core`, and `Sqloom.Correlation -> Sqloom.AspNetCore`.

## Build and Test

Build from the repo root:

```powershell
dotnet restore .\Sqloom.sln
dotnet build .\Sqloom.sln --tl:off --nologo "-clp:ErrorsOnly;NoSummary"
```

Build `Sqloom.sln` before using either the standalone `sqloom` tool or the non-packed `Sqloom.Host` wrapper. When you run `replay <path>` or `observe <path>`, the host resolves that target path down to one or more distinct library harnesses, resolves each harness project's `TargetPath` through `dotnet msbuild`, and builds those harnesses automatically unless you add `--no-build`. Pass `--dotnet-command <command>` explicitly when that nested resolution should use a non-default dotnet executable. Supported target paths are project files, solution files, solution filters, and directories. Composite replay targets run every distinct integration they resolve to in order; when you pass `--artifact-dir` to a composite replay, Sqloom creates one child artifact directory per app under that root.

Talio's workspace is configured so `Talio.sln`, `src\Talio.Api\`, and `src\Talio.Api\Talio.Api.csproj` resolve to the Talio harness behind the scenes.

`Sqloom.sln` stays the main Sqloom workspace solution. The repo-root `global.json` opts this workspace into the Microsoft Testing Platform runner, so run the generic Sqloom xUnit lanes from the repo root:

```powershell
dotnet test --solution .\Sqloom.UnitTests.slnf
dotnet test --solution .\Sqloom.IntegrationTests.slnf
```

Talio-specific Sqloom test lanes remain in the Talio repository.

## Tool Front Door

From the repo root, use the repo-local deployment script when you want a fast dev install that stays separate from the published global `sqloom` command:

```powershell
pwsh .\scripts\deploy-sqloom-local.ps1
```

That script repacks the full `Sqloom.*` package set into `.\artifacts\packages\sqloom`, reinstalls the local dev tool into `..\.tools\sqloom-local`, and regenerates the wrapper command at `..\.tools\bin\sqloom-local.cmd`. Add the repo-local wrapper directory to `PATH` if you want to call the dev tool as `sqloom-local`:

```text
..\.tools\bin
```

The deploy script now also ensures that wrapper directory is on the current PowerShell session PATH and on the user PATH for new terminals, so a direct invocation like `.\scripts\deploy-sqloom-local.ps1` is followed immediately by `sqloom-local --version` in the same shell.

The local wrapper accepts the same explicit stage-verb model as `Sqloom.Host` itself. SQL Server-backed replay harnesses can require an app-owned DACPAC path; Talio examples below use the repo-local `.\artifacts\Talio.dacpac` artifact.

```powershell
if (-not $env:OPENAI_API_KEY) { throw "OPENAI_API_KEY is required." }

sqloom-local tune .\tests\Sqloom.TestApp\Sqloom.TestApp.csproj `
  --no-build `
  --sqlserver-dacpac-file .\tests\Sqloom.TestApp.IntegrationTests\AdventureWorksLT2025.dacpac `
  --sqlserver-schema-file .\tests\Sqloom.TestApp.IntegrationTests\AdventureWorksLT2025.schema.sql `
  --read-only-connection-string "Server=localhost;Database=AdventureWorksLT2025;Integrated Security=True;TrustServerCertificate=True;MultipleActiveResultSets=True" `
  --target "GET /api/products/by-category" `
  --model-provider openai `
  --openai-api-key $env:OPENAI_API_KEY `
  --openai-model "gpt-5.5"
```

For the aggregated workflow, `tune` takes the same target-selection and replay knobs plus the observe connection string, required `--model-provider openai`, and the OpenAI settings. The sample-app command above is the recommended first run because it produces a concrete `sql-tuning-proposal.sql` artifact with the least setup.

```powershell
sqloom-local tune .\Talio.sln --no-build --read-only-connection-string "<connection-string>" --sqlserver-dacpac-file .\artifacts\Talio.dacpac --sqlserver-schema-file ".\artifacts\Talio.schema.sql" --target "GET /api/expenses/dashboard" --model-provider openai --openai-api-key $env:OPENAI_API_KEY
```

When you want a clean package-prep pass before a manual `nuget.org` push, use:

```powershell
pwsh .\scripts\prepare-sqloom-packages.ps1
```

That script restores and builds `.\Sqloom.sln`, repacks the full `Sqloom.*` feed under `.\artifacts\packages\sqloom`, verifies a clean tool-path install from that folder feed, and prints the exact `dotnet nuget push` commands without pushing anything.

During development, the equivalent non-packed host command is:

```powershell
dotnet run --project .\src\Sqloom.Host\Sqloom.Host.csproj -- replay .\tests\Sqloom.TestApp\Sqloom.TestApp.csproj --target "GET /api/products/by-category"
```

## Replay Smoke Check

From the repo root, run one focused replay through the generic host against the sample app. Sqloom consumes the prebuilt DACPAC you give it; it does not build the DACPAC for you.

```powershell
dotnet run --project .\src\Sqloom.Host\Sqloom.Host.csproj -- replay .\tests\Sqloom.TestApp\Sqloom.TestApp.csproj --target "GET /api/products/by-category"
dotnet run --project .\src\Sqloom.Host\Sqloom.Host.csproj -- replay .\tests\Sqloom.TestApp\Sqloom.TestApp.csproj --sqlserver-dacpac-file .\tests\Sqloom.TestApp.IntegrationTests\AdventureWorksLT2025.dacpac --target "GET /api/products/by-category"
```

The generic sample app uses the same host-first surface. Without `--sqlserver-dacpac-file`, this route replays against the in-memory EF Core fallback:

```powershell
dotnet run --project .\src\Sqloom.Host\Sqloom.Host.csproj -- replay .\tests\Sqloom.TestApp\Sqloom.TestApp.csproj --target "GET /api/products/by-category"
```

When you want the sample harness to mirror Talio's SQL Server-backed replay path, pass the committed AdventureWorks DACPAC explicitly:

```powershell
dotnet run --project .\src\Sqloom.Host\Sqloom.Host.csproj -- replay .\tests\Sqloom.TestApp\Sqloom.TestApp.csproj --sqlserver-dacpac-file .\tests\Sqloom.TestApp.IntegrationTests\AdventureWorksLT2025.dacpac --target "GET /api/products/by-category"
```

That SQL-backed sample harness seeds `SalesLT.ProductCategory` and `SalesLT.Product` rows into a SQL Server 2025 Testcontainer so the replay can drive the missing-index tuning scenario. Use that route when you want the sample app to produce a concrete `SalesLT.Product` SQL proposal from Query Store correlation and `advise --model-provider openai --sqlserver-schema-file <path>`.

If you want the sample harness to restore app-owned data after DACPAC publish instead of using its built-in hot-product seed, pass `--sqlserver-seed-sql-file <path>` alongside the DACPAC:

```powershell
dotnet run --project .\src\Sqloom.Host\Sqloom.Host.csproj -- replay .\tests\Sqloom.TestApp\Sqloom.TestApp.csproj --sqlserver-dacpac-file .\tests\Sqloom.TestApp.IntegrationTests\AdventureWorksLT2025.dacpac --sqlserver-seed-sql-file .\tests\Sqloom.TestApp.IntegrationTests\AdventureWorksLT2025.seed.sql --target "GET /api/products/by-category"
```

For `Sqloom.TestApp`, that post-DACPAC seed script is app-owned input. When you supply it, the sample harness executes the script against the fresh Testcontainer database and skips the built-in `Sqloom Hot Product` seeding.

When `tests/Sqloom.TestApp.IntegrationTests/AdventureWorksLT2025.dacpac` changes, regenerate the Codex-friendly single-file schema dump beside it with `pwsh .\tests\Sqloom.TestApp.IntegrationTests\Export-AdventureWorksLT2025Schema.ps1`. The script publishes the DACPAC to a scratch database on `localhost`, extracts `AdventureWorksLT2025.schema.sql`, and drops the scratch database afterward.

To export seed data from a local `AdventureWorksLT2025` database on `localhost` into a post-DACPAC replay script, run from `backend/`:

```powershell
pwsh .\tests\Sqloom.TestApp.IntegrationTests\Export-AdventureWorksLT2025SeedSql.ps1
```

By default that script reads `Server=localhost;Database=AdventureWorksLT2025;Integrated Security=True;TrustServerCertificate=True;MultipleActiveResultSets=True` and writes `tests/Sqloom.TestApp.IntegrationTests/AdventureWorksLT2025.seed.sql`. Override `-ConnectionString` or `-OutputPath` when you want to export a different localhost source database or stage the script somewhere else.

To stage a fresh reverse-engineered EF Core snapshot for the sample harness from a local `AdventureWorksLT2025` database on `localhost`, run from `backend/`:

```powershell
pwsh .\tests\Sqloom.TestApp\Scaffold-AdventureWorksLT2025Ef.ps1
```

The script stages raw scaffold output under `artifacts/ef-scaffold/Sqloom.TestApp/`, installs a temporary local `dotnet-ef` tool if needed, and then syncs the generated DbContext plus entities into `tests/Sqloom.TestApp/Generated/` so `Sqloom.TestApp` owns the reverse-engineered EF model. `Sqloom.TestApp.IntegrationTests` now references `Sqloom.TestApp` for those app-owned types. By default the script scaffolds the curated AdventureWorksLT2025 table set used by the sample harness: `dbo.BuildVersion`, `dbo.ErrorLog`, `SalesLT.Address`, `SalesLT.Customer`, `SalesLT.CustomerAddress`, `SalesLT.Product`, `SalesLT.ProductCategory`, `SalesLT.ProductDescription`, `SalesLT.ProductModel`, `SalesLT.ProductModelProductDescription`, `SalesLT.SalesOrderDetail`, and `SalesLT.SalesOrderHeader`. Pass `-Tables SalesLT.Product,SalesLT.ProductCategory` if you want to narrow scaffolding to a smaller slice while you iterate.

## Query Store Capture

From `backend/`, capture the hottest Query Store plans and waits from a readonly Azure SQL connection through the generic host against the Talio solution path:

```powershell
dotnet run --project .\src\Sqloom.Host\Sqloom.Host.csproj -- observe .\Talio.sln --read-only-connection-string "<connection-string>" --lookback-hours 24 --max-plans 100 --max-waits 10 --json-output-file ".\artifacts\query-store-snapshot.json" --app-only --show-classification
```

`observe` and `correlate` require `--read-only-connection-string <connection-string>`. The active host surface does not use environment variables for that input.

When an app integration is supplied, `observe` applies that app's workload profile. Talio currently contributes only the snapshot label, while app classification itself stays discovery-first: the host captures a discovered-object catalog from the same readonly connection and uses that live database evidence to decide which plans belong to the app.

If `--json-output-file` is omitted, the host writes the full `QueryStoreSnapshot` to the default artifact location under `artifacts/sqloom/query-store/` with a timestamped file name.

When `--max-plans` is omitted, the host now captures the top 100 Query Store plans before classification and `--app-only` console filtering. `--max-waits` still defaults to 10.

The console summary includes `query_hash`, `object`, `last_exec_utc`, and a truncated one-line SQL text preview for every top Query Store plan so you can identify the query before deeper replay and correlation analysis.

Before classification, the host also discovers user-defined database objects with the same readonly connection. Tables and views are discovered by default, and module discovery is attempted when the principal also has `VIEW DEFINITION`. The discovered-object catalog is attached to the classified snapshot JSON and printed in the console summary.

`--app-only` keeps the console output focused on queries classified as `App`, while the JSON artifact still preserves the full captured snapshot. `--app-only` also implies `--show-classification`. `--show-classification` prints the deterministic workload kind, confidence, and matching reasons for each displayed plan and wait. Wait entries inherit their classification from the matching `(query_id, plan_id)` plan record.

The workload classifier is intentionally discovery-first. The Azure SQL Query Store collector stays broad and reusable, while Talio's workload profile now only contributes the snapshot label. App classification comes from discovered objects captured from the live database rather than repo-maintained table lists, query hashes, or evidence modes.

## Tune Workflow

From `backend/`, run the full observe, replay, correlate, and advise flow in one command:

Recommended first run:

```powershell
if (-not $env:OPENAI_API_KEY) { throw "OPENAI_API_KEY is required." }

sqloom-local tune .\tests\Sqloom.TestApp\Sqloom.TestApp.csproj `
  --no-build `
  --sqlserver-dacpac-file .\tests\Sqloom.TestApp.IntegrationTests\AdventureWorksLT2025.dacpac `
  --sqlserver-schema-file .\tests\Sqloom.TestApp.IntegrationTests\AdventureWorksLT2025.schema.sql `
  --read-only-connection-string "Server=localhost;Database=AdventureWorksLT2025;Integrated Security=True;TrustServerCertificate=True;MultipleActiveResultSets=True" `
  --target "GET /api/products/by-category" `
  --model-provider openai `
  --openai-api-key $env:OPENAI_API_KEY `
  --openai-model "gpt-5.5"
```

That command is the easiest way to see the tuning pipeline produce both an advice report and a SQL proposal script from the sample AdventureWorks product query.

```powershell
dotnet run --project .\src\Sqloom.Host\Sqloom.Host.csproj -- tune .\Talio.sln --no-build --read-only-connection-string "<connection-string>" --sqlserver-dacpac-file .\artifacts\Talio.dacpac --sqlserver-schema-file ".\artifacts\Talio.schema.sql" --target "GET /api/expenses/dashboard" --model-provider openai --openai-api-key "<api-key>"
```

Supported `tune` switches are the union of the main observe, replay, and advice knobs that affect behavior rather than file locations:

- `--debug`: global host switch that prints per-stage diagnostics to `stderr`. With `tune`, the same debug setting flows into the nested `observe`, `replay`, `correlate`, and `advise` stages.
- `--read-only-connection-string <connection-string>`: required Query Store connection used by the observe and correlate stages.
- `--lookback-hours <hours>`, `--max-plans <count>`, `--max-waits <count>`, `--command-timeout-seconds <seconds>`, `--app-only`, `--show-classification`: same behavior as `observe`.
- `--openapi-file <path>`, `--sqlserver-dacpac-file <path>`, `--sqlserver-seed-sql-file <path>`, `--max-operations <count>`, `--target "METHOD /path/template"`, `--dotnet-command <command>`, `--no-build`: same behavior as `replay`.
- `--model-provider openai`, `--openai-api-key <key>`, `--sqlserver-schema-file <path>`, `--openai-base-url <url>`, `--openai-model <id>`: same behavior as `advise`.
- `--artifact-dir <path>`: override the workflow root instead of using the default timestamped tune artifact directory.

If `--artifact-dir` is omitted, `tune` writes under `artifacts/sqloom/tune/tune-<timestamp>/`. Each run writes:

- `query-store-snapshot.json`
- `tune-summary.json`
- `replay/`

The `replay/` directory then contains the same replay, correlation, advice, and SQL proposal artifacts that the standalone `replay`, `correlate`, and `advise` commands already emit.

## Replay Mode

From `backend/`, replay OpenAPI operations through the generic host against the Talio solution path:

```powershell
dotnet run --project .\src\Sqloom.Host\Sqloom.Host.csproj -- replay .\Talio.sln --sqlserver-dacpac-file .\artifacts\Talio.dacpac --target "GET /api/expenses/dashboard"
dotnet run --project .\src\Sqloom.Host\Sqloom.Host.csproj -- replay .\Talio.sln --sqlserver-dacpac-file .\artifacts\Talio.dacpac --target "POST /api/advisor/query"
```

The standalone `Sqloom.Host` executable uses the same explicit form, for example `replay .\Talio.sln --sqlserver-dacpac-file .\artifacts\Talio.dacpac --target "GET /api/expenses/dashboard"`, `replay .\src\Talio.Api --sqlserver-dacpac-file .\artifacts\Talio.dacpac --target "GET /api/expenses/dashboard"`, or `replay .\src\Talio.Api\Talio.Api.csproj --sqlserver-dacpac-file .\artifacts\Talio.dacpac --target "GET /api/expenses/dashboard"`. Talio's workspace maps those user-facing paths to the Talio integration behind the scenes.

Supported replay switches:

- `--openapi-file <path>`: override the OpenAPI document. Talio defaults to `shared/generated/openapi/talio-api-v1.json`.
- `--max-operations <count>`: cap the number of replayed operations after filtering.
- `--target "METHOD /path/template"`: replay one exact discovered operation key such as `GET /api/expenses/dashboard` or `POST /api/advisor/query`.
- `--dotnet-command <command>`: override the dotnet executable Sqloom uses for nested project resolution and builds.
- `--sqlserver-dacpac-file <path>`: pass a prebuilt DACPAC to a SQL Server-backed replay harness. Sqloom treats this as app-owned input and does not build the DACPAC.
- `--sqlserver-seed-sql-file <path>`: apply an app-owned SQL seed script after DACPAC publish. `Sqloom.TestApp` uses this to restore exported AdventureWorks data and skips its built-in hot-product seed when the script is supplied.
- `--artifact-dir <path>`: override the replay artifact directory.

Replay V1 is intentionally read-heavy. Authenticated `GET` operations are replay-safe by default, while non-`GET` operations require app-owned opt-in through `AllowNonGetReplay`. Talio marks `POST /api/advisor/query` as replayable but not defaulted, so it only runs when you select it explicitly with `--target "POST /api/advisor/query"`.

`--target` is strict. It must use the exact form `METHOD /path/template` with an uppercase HTTP method, one space, a leading `/`, and no trailing `/` or repeated `//`. If you mistype the shape, Sqloom fails fast and suggests the corrected form when it can.

Talio replay runs are self-managed. `Talio.Sqloom` boots `Talio.Api` in-process, provisions a disposable local SQL Server Testcontainer, publishes the supplied SQL Server DACPAC into that database, applies Talio-specific post-publish advisor read-only setup, installs deterministic test doubles for email, OpenAI, and semantic dependencies, and seeds only the setup state needed to make the selected replay runnable. A pre-running Docker Compose stack is not required.

If `--artifact-dir` is omitted, replay artifacts default under `artifacts/sqloom/replay/<timestamp>/`. When one composite target replays multiple app integrations, that timestamped directory becomes the parent folder and Sqloom writes one child directory per app such as `01-Sqloom.TestApp/` or `02-Talio.Sqloom/`. Each run writes:

- `discovered-operations.json`
- `replay-plan.json`
- `replay-summary.json`
- `operations/<ordinal>-<operation>.json`

`replay-summary.json` records the app name plus the first-class pipeline state so the next `correlate` and `advise` steps are explicit in the artifact itself. When a SQL Server DACPAC or post-DACPAC SQL seed script is supplied, the summary also records that artifact's source path, file name, and SHA-256 provenance.

Per-operation artifacts preserve the resolved HTTP request, response status and body, captured SQL commands, normalized SQL text, fingerprints, and parameter values so later Query Store correlation can start from concrete replay evidence. Attach-to-existing local database support is planned later and is not part of V1.

## Query Store Correlation

From `backend/`, correlate a captured replay run back to a previously captured Query Store snapshot:

```powershell
dotnet run --project .\src\Sqloom.Host\Sqloom.Host.csproj -- correlate --replay-artifact-dir ".\artifacts\sqloom\replay\replay-20260608T040506000Z" --query-store-snapshot-file ".\artifacts\sqloom\query-store\query-store-20260608T041245123Z.json" --read-only-connection-string "<connection-string>"
```

By default, the host writes `query-store-correlation.json` under the replay artifact directory. Each record keeps the replay operation key, route, captured SQL metadata, resolved `statement_sql_handle` candidates, match kind, confidence, and the matched Query Store rows. The summary groups results at the HTTP operation boundary so you can answer which stable API operation most likely owns a degrading database query without relying on controller method names.

`query-store-correlation.json` records the app name plus the first-class pipeline state so the downstream `advise` stage is explicit in the artifact.

Correlation uses this ranking:

- `StatementHandleExact`: exact `statement_sql_handle` match after resolving the captured SQL through `sys.fn_stmt_sql_handle_from_sql_stmt`.
- `QueryTextExact`: exact raw SQL text match after trimming only the same outer whitespace and comments Query Store ignores.
- `FingerprintFallback`: local normalized and fingerprint similarity for diagnostics only.
- `Unmatched`: no safe correlation.

## Advice Mode

From `backend/`, derive operation-level tuning guidance from a completed replay and correlation pair:

```powershell
dotnet run --project .\src\Sqloom.Host\Sqloom.Host.csproj -- advise --replay-artifact-dir ".\artifacts\sqloom\replay\replay-20260608T040506000Z" --model-provider openai --openai-api-key "<api-key>" --sqlserver-schema-file "<schema-file>"
```

Supported advice switches:

- `--debug`: global host switch that prints per-stage diagnostics to `stderr`. For `advise`, that includes readable, redacted OpenAI request and response payloads.
- `--replay-artifact-dir <path>`: required replay artifact directory that already contains `query-store-correlation.json`.
- `--query-store-correlation-file <path>`: override the correlation artifact path when it lives outside the replay directory default.
- `--json-output-file <path>`: override the advice artifact output path.
- `--model-provider openai`: required LLM model provider selector for the advice stage.
- `--openai-api-key <key>`: required API key for the OpenAI-backed advisor.
- `--sqlserver-schema-file <path>`: required SQL Server schema file that the advisor uses as table, column, and existing-index context.
- `--openai-base-url <url>`: optional override for the OpenAI base URL. The default is `https://api.openai.com`.
- `--openai-model <id>`: optional override for the OpenAI model id. The default is `gpt-5.4-mini`.

By default, the host reads `query-store-correlation.json` under the replay artifact directory and writes `tuning-advice.json` beside it. The `advise` stage also emits two replay-scoped proposal sidecars under that same artifact directory:

- `sql-tuning-proposal.json`
- `sql-tuning-proposal.sql`

Advice now always runs through OpenAI. Before sending the request, Sqloom packages the replay operation evidence, Query Store correlation evidence, any available snapshot subset, and the supplied SQL Server schema file into an evidence-only prompt envelope with no preloaded local tuning fix.

Use `--model-provider openai` and pass the OpenAI settings explicitly on the command line. The active host surface does not use environment-variable fallbacks for OpenAI configuration.

The proposal sidecars are phase-1, SQL Server-oriented review artifacts synthesized from the existing replay and Query Store evidence plus the supplied SQL Server schema file. OpenAI proposal kinds are now preserved as model-provided free-form strings, and every model proposal that passes JSON deserialization is written into `tuning-advice.json`, `sql-tuning-proposal.json`, and `sql-tuning-proposal.sql`.

Rollback SQL is recommended but optional in the OpenAI path. When the model omits `rollbackSqlScript`, Sqloom keeps the proposal, records a warning in the advice and proposal reports, and renders a placeholder rollback note in the `.sql` sidecar. Sqloom no longer synthesizes deterministic local SQL proposals from Query Store correlation alone.

To generate operation advice:

```powershell
dotnet run --project .\src\Sqloom.Host\Sqloom.Host.csproj -- advise --replay-artifact-dir ".\artifacts\sqloom\replay\replay-20260608T040506000Z" --model-provider openai --openai-api-key "<api-key>" --sqlserver-schema-file "<schema-file>" --openai-base-url "https://api.openai.com" --openai-model "gpt-5.4-mini"
```

The OpenAI advisor keeps the same correlation artifact input, sends the operation evidence bundle plus the supplied schema text to the Responses API with a strict JSON schema, preserves the model's free-form `proposalKind`, and writes the same `tuning-advice.json` artifact shape with `modelProvider=openai` and the selected model recorded in the report.

## Talio Composition

`Sqloom.Host` does not reference `Talio.Sqloom` or any other app under test. The standalone host stays generic and now requires explicit stage verbs plus verb-scoped target paths such as `observe <path>` and `replay <path>`, where the path can be a project, solution, solution filter, or directory. `Sqloom.Host` owns the generic target resolution, app loading, and command pipeline, while `Talio.Sqloom` under `backend/tests` keeps the Talio-specific replay bootstrap, personas, operation overlays, and `WebApplicationFactory` setup together as a host-loaded library harness. `Sqloom.TestApp` now follows the same companion-project pattern through `Sqloom.TestApp.IntegrationTests` for generic integration coverage.

## Azure SQL Principal

Create a dedicated contained database user for Sqloom. Do not reuse an application runtime principal. Run this while connected to the target Azure SQL database:

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

This keeps the Sqloom connection read-only while still allowing Query Store access, discovered-object classification, and targeted plan capture.

If the host prints this warning:

```text
Module discovery skipped because VIEW DEFINITION permission is unavailable.
```

grant `VIEW DEFINITION` to the Sqloom principal:

```sql
GRANT VIEW DEFINITION TO [sqloom_ro];
```

That permission is read-only metadata access. It lets Sqloom discover stored procedures and functions so module-backed workloads can classify as `App`, but it does not grant `INSERT`, `UPDATE`, or `DELETE`.

If you only need table and view discovery, `VIEW DEFINITION` can be omitted and the warning is expected. If you only need Query Store snapshots, `SHOWPLAN` is optional, but the full lab uses both Query Store and plan inspection.

Use the resulting connection string with `--read-only-connection-string` when running `observe` or `correlate`.
