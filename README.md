# Sqloom

Sqloom helps you find slow database work behind API requests in a .NET app. It can read Query Store from SQL Server or Azure SQL, replay API requests through an app-owned harness, match the SQL it saw back to Query Store, and write tuning advice plus suggested SQL changes.

`tune` runs the full harness flow: `replay -> observe -> correlate -> advise`. Most people will use the `sqloom` command. If you are changing Sqloom in this repo, use `sqloom-local` instead.

This repo includes a sample app and harness for `GET /api/products/by-category`. The harness starts a throwaway SQL Server database from `AdventureWorksLT2025.dacpac`, runs the included SQL seed script, and exposes the connection string and schema defaults to the Sqloom runner.

![Sqloom tuning pipeline diagram](docs/images/sqloom-diagram.png)

## Install

Install the public tool from NuGet.org:

```powershell
dotnet tool install --global sqloom
sqloom --help
```

To update an existing install:

```powershell
dotnet tool update --global sqloom
```

## Quick Start

If you want the quickest end-to-end demo, run `sqloom tune` against the sample harness and `--debug`. This runs the whole flow and writes both the advice report and the generated SQL proposal script:

```powershell
if (-not $env:OPENAI_API_KEY) { throw "OPENAI_API_KEY is required." }

sqloom tune .\tests\Sqloom.TestApp.Harness\Sqloom.TestApp.Harness.csproj `
  --target "GET /api/products/by-category" `
  --model-provider openai `
  --openai-api-key $env:OPENAI_API_KEY `
  --openai-model "gpt-5.5" `
  --debug
```

`Sqloom.TestApp.Harness` supplies defaults for the DACPAC, seed script, SQL Server schema file, replay profile, Query Store profile, and read-only replay database connection. CLI options such as `--sqlserver-dacpac-file`, `--sqlserver-seed-sql-file`, `--sqlserver-schema-file`, and `--read-only-connection-string` override those harness defaults when you need to point at different inputs. You can regenerate the seed script from `localhost` with `pwsh .\tests\Sqloom.TestApp.Harness\Export-AdventureWorksLT2025SeedSql.ps1`.

The run writes output files under `artifacts/sqloom/tune/tune-<timestamp>/`, including:

- `query-store-snapshot.json`
- `tune-summary.json`
- `replay/tuning-advice.json`
- `replay/sql-tuning-proposal.json`
- `replay/sql-tuning-proposal.sql`

Add `--debug` to any `sqloom` or `sqloom-local` command when you want step-by-step details on `stderr`. `advise --debug` prints readable, redacted OpenAI request and response data, and `tune --debug` turns on debug output for `replay`, `observe`, `correlate`, and `advise`.

```text
.
|-- Sqloom.slnx
|-- Sqloom.UnitTests.slnf
|-- Sqloom.IntegrationTests.slnf
|-- README.md
|-- src/
|   |-- Sqloom.Core/
|   |-- Sqloom.QueryStore/
|   |-- Sqloom.SqlServer/
|   |-- Sqloom.AspNetCore/
|   |-- Sqloom.Testing/
|   `-- Sqloom.Host/
`-- tests/
    |-- Directory.Build.props
    |-- Sqloom.TestApp/
    |-- Sqloom.TestApp.Harness/
    |-- Sqloom.UnitTests/
    `-- Sqloom.IntegrationTests/
```

The app-specific harness projects live next to the apps they support.

## How Sqloom Works

Sqloom is built around one CLI plus app-specific harness projects:

- Main flow for users: `replay -> observe -> correlate -> advise`
- `tune` runs that full flow for you and writes its output under `artifacts/sqloom/`
- The `sqloom` command is the tool most people should use. `Sqloom.Host` is the project you can run directly while developing Sqloom itself.
- Replay finds API operations such as `GET /api/products/by-category` and runs them through an `ISqloomApplication` supplied by the harness
- `Sqloom.Testing` is the public contract package that external harnesses reference
- `Sqloom.TestApp.Harness` is the sample app-specific harness project in this repo
- More apps can follow the same harness pattern

For more detail about the repo layout, project ownership, and dependency graph, see:

- `docs/architecture/overview.md`
- `docs/architecture/dependencies.md`

## Build and Test

Build from the repo root:

```powershell
dotnet restore .\Sqloom.slnx
dotnet build .\Sqloom.slnx --tl:off --nologo "-clp:ErrorsOnly;NoSummary"
```

Build `Sqloom.slnx` before using the `sqloom` tool or running `Sqloom.Host` directly. When you run `tune <path>`, `replay <path>`, or `observe <path>`, Sqloom treats the path as a harness project, harness assembly, solution, solution filter, or directory containing harness projects. It builds projects unless you add `--no-build`, scans the resulting assemblies for public non-abstract `ISqloomApplication` implementations, and requires exactly one implementation. Use `--dotnet-command <command>` if you need Sqloom to call a specific `dotnet` executable.

`Sqloom.slnx` is the repo's main workspace solution. The repo-root `global.json` makes these test commands use Microsoft Testing Platform, so run the Sqloom xUnit lanes from the repo root:

```powershell
dotnet test --solution .\Sqloom.UnitTests.slnf
dotnet test --solution .\Sqloom.IntegrationTests.slnf
```

## Using `sqloom` and `sqloom-local`

Use `sqloom` when you want to use Sqloom. Use `sqloom-local` only when you are working on Sqloom inside this repo and want a separate local install.

From the repo root, use the repo-local deployment script when you want a fast dev install that stays separate from an installed `sqloom` command:

```powershell
pwsh .\scripts\deploy-sqloom-local.ps1
```

That script rebuilds the local packages, reinstalls the repo-local tool into `.\.tools\sqloom-local`, and recreates the wrapper at `.\.tools\bin\sqloom-local.cmd`. Add that folder to `PATH` if you want to call the local tool as `sqloom-local`:

```text
.\.tools\bin
```

The deploy script also updates the current PowerShell session PATH and the user PATH for new terminals, so you can run `sqloom-local --version` right away in the same shell.

The local wrapper accepts the same commands as the packaged `sqloom` tool. SQL Server replays can use harness defaults or CLI-supplied DACPAC and seed script paths.

```powershell
if (-not $env:OPENAI_API_KEY) { throw "OPENAI_API_KEY is required." }

sqloom-local tune .\tests\Sqloom.TestApp.Harness\Sqloom.TestApp.Harness.csproj `
  --no-build `
  --target "GET /api/products/by-category" `
  --model-provider openai `
  --openai-api-key $env:OPENAI_API_KEY `
  --openai-model "gpt-5.5" `
  --debug
```

This uses the same full `tune` flow as the main example, but it uses your local build. It is the best option when you are changing this repo and want a real `sql-tuning-proposal.sql` output with minimal setup.

When you want a clean package-prep pass before a manual `nuget.org` push, use:

```powershell
pwsh .\scripts\prepare-sqloom-packages.ps1
```

That script restores and builds `.\Sqloom.slnx`, rebuilds the local package folder under `.\artifacts\packages\sqloom`, checks that the tool installs cleanly from that folder, and prints the exact `dotnet nuget push` commands for the public packages without pushing anything.

For the full maintainer runbook, including manual NuGet.org upload order, see [`docs/dotnet-tool-release.md`](docs/dotnet-tool-release.md).

While developing, you can also run the host project directly:

```powershell
dotnet run --project .\src\Sqloom.Host\Sqloom.Host.csproj -- replay .\tests\Sqloom.TestApp.Harness\Sqloom.TestApp.Harness.csproj --target "GET /api/products/by-category"
```

## Quick Replay Check

Use this when you want a quick check that replay works before trying the full tune flow. The sample harness defaults to the committed AdventureWorks DACPAC and seed script.

```powershell
dotnet run --project .\src\Sqloom.Host\Sqloom.Host.csproj -- replay .\tests\Sqloom.TestApp.Harness\Sqloom.TestApp.Harness.csproj --target "GET /api/products/by-category"
```

If you want to override the harness DACPAC or seed script, pass explicit paths:

```powershell
dotnet run --project .\src\Sqloom.Host\Sqloom.Host.csproj -- replay .\tests\Sqloom.TestApp.Harness\Sqloom.TestApp.Harness.csproj --sqlserver-dacpac-file .\tests\Sqloom.TestApp.Harness\AdventureWorksLT2025.dacpac --sqlserver-seed-sql-file .\tests\Sqloom.TestApp.Harness\AdventureWorksLT2025.seed.sql --target "GET /api/products/by-category"
```

This path loads AdventureWorks rows into a throwaway SQL Server container so the sample can show the missing-index tuning case. Use it when you want the sample app to produce a real `SalesLT.Product` SQL proposal from Query Store data.

If you want to load your own exported data instead of the committed seed script, pass a different `--sqlserver-seed-sql-file <path>` with the DACPAC:

```powershell
dotnet run --project .\src\Sqloom.Host\Sqloom.Host.csproj -- replay .\tests\Sqloom.TestApp.Harness\Sqloom.TestApp.Harness.csproj --sqlserver-dacpac-file .\tests\Sqloom.TestApp.Harness\AdventureWorksLT2025.dacpac --sqlserver-seed-sql-file .\tests\Sqloom.TestApp.Harness\AdventureWorksLT2025.seed.sql --target "GET /api/products/by-category"
```

For `Sqloom.TestApp.Harness`, that script is optional override input. The default harness path uses the committed seed script next to the DACPAC.

When `tests/Sqloom.TestApp.Harness/AdventureWorksLT2025.dacpac` changes, regenerate the single-file schema dump next to it with `pwsh .\tests\Sqloom.TestApp.Harness\Export-AdventureWorksLT2025Schema.ps1`. The script applies the DACPAC to a temporary database on `localhost`, writes `AdventureWorksLT2025.schema.sql`, and then removes the temporary database.

To export seed data from a local `AdventureWorksLT2025` database on `localhost` into a post-DACPAC replay script, run from the repo root:

```powershell
pwsh .\tests\Sqloom.TestApp.Harness\Export-AdventureWorksLT2025SeedSql.ps1
```

By default, that script reads `Server=localhost;Database=AdventureWorksLT2025;Integrated Security=True;TrustServerCertificate=True;MultipleActiveResultSets=True` and writes `tests/Sqloom.TestApp.Harness/AdventureWorksLT2025.seed.sql`. Override `-ConnectionString` or `-OutputPath` when you want a different source database or output path.

If you need to regenerate the EF Core model for the sample app from a local `AdventureWorksLT2025` database on `localhost`, run this from the repo root:

```powershell
pwsh .\tests\Sqloom.TestApp\Scaffold-AdventureWorksLT2025Ef.ps1
```

The script writes raw scaffold output under `artifacts/ef-scaffold/Sqloom.TestApp/`, installs a temporary local `dotnet-ef` tool if needed, and then copies the generated DbContext and entities into `tests/Sqloom.TestApp/Generated/`. `Sqloom.TestApp.Harness` references `Sqloom.TestApp` for those generated types. By default, the script scaffolds the AdventureWorksLT2025 tables used by the sample app: `dbo.BuildVersion`, `dbo.ErrorLog`, `SalesLT.Address`, `SalesLT.Customer`, `SalesLT.CustomerAddress`, `SalesLT.Product`, `SalesLT.ProductCategory`, `SalesLT.ProductDescription`, `SalesLT.ProductModel`, `SalesLT.ProductModelProductDescription`, `SalesLT.SalesOrderDetail`, and `SalesLT.SalesOrderHeader`. Pass `-Tables SalesLT.Product,SalesLT.ProductCategory` if you want to work on a smaller set of tables.

## Read Query Store

Use `observe` to read recent Query Store data from SQL Server or Azure SQL. Query Store is SQL Server's built-in history of query performance.

From the repo root, run this command with a read-only connection string and the sample harness path:

```powershell
dotnet run --project .\src\Sqloom.Host\Sqloom.Host.csproj -- observe .\tests\Sqloom.TestApp.Harness\Sqloom.TestApp.Harness.csproj --read-only-connection-string "Server=localhost;Database=AdventureWorksLT2025;Integrated Security=True;TrustServerCertificate=True;MultipleActiveResultSets=True" --lookback-hours 24 --max-plans 100 --max-waits 10 --json-output-file ".\artifacts\query-store-snapshot.json" --app-only --show-classification
```

`observe` and `correlate` both require `--read-only-connection-string <connection-string>`. This command path does not read that value from environment variables.

If you pass a harness path, `observe` uses the harness manifest's Query Store profile to decide which queries belong to the app. Sqloom also reads the live database objects from the same read-only connection instead of relying on a hard-coded list in the repo.

If you do not pass `--json-output-file`, Sqloom writes the full `QueryStoreSnapshot` to `artifacts/sqloom/query-store/` with a timestamped file name.

If you do not pass `--max-plans`, Sqloom captures the top 100 Query Store plans before it filters the console output. `--max-waits` still defaults to 10.

The console summary shows fields such as `query_hash`, `object`, `last_exec_utc`, and a shortened one-line SQL preview so you can spot the query before moving on.

Before it labels queries, Sqloom also reads user-defined database objects through the same read-only connection. Tables and views are included by default. It also tries to read stored procedures and functions if the user has `VIEW DEFINITION`. That object list is saved in the JSON file and printed in the console summary.

`--app-only` keeps the console output focused on queries Sqloom thinks belong to the app, while the JSON file still keeps the full snapshot. `--app-only` also turns on `--show-classification`. `--show-classification` prints the chosen kind, confidence, and reasons for each displayed plan and wait. Wait entries use the same label as the matching `(query_id, plan_id)` plan.

Sqloom decides what belongs to the app by looking at the live database, not by checking a repo-owned list of tables or query hashes.

## Run the Full Tune Flow

Use `tune` when you want Sqloom to do the full run in one command: start the harness, replay requests, read Query Store, match the SQL, and generate advice.

From the repo root, run:

Recommended first run:

```powershell
if (-not $env:OPENAI_API_KEY) { throw "OPENAI_API_KEY is required." }

sqloom tune .\tests\Sqloom.TestApp.Harness\Sqloom.TestApp.Harness.csproj `
  --target "GET /api/products/by-category" `
  --model-provider openai `
  --openai-api-key $env:OPENAI_API_KEY `
  --openai-model "gpt-5.5" `
  --debug
```

This is the easiest way to get both an advice report and a SQL proposal script from the sample AdventureWorks product query.

If you are iterating on this repository itself, the repo-local equivalent is the `sqloom-local tune` example in the `CLI Entry Points` section above.

`tune` accepts the main behavior-changing options from `observe`, `replay`, and `advise`:

- `--debug`: print step-by-step details to `stderr` for `replay`, `observe`, `correlate`, and `advise`
- `--read-only-connection-string <connection-string>`: Query Store connection for `observe` and `correlate`; in `tune`, this overrides the harness session connection string
- `--lookback-hours <hours>`, `--max-plans <count>`, `--max-waits <count>`, `--command-timeout-seconds <seconds>`, `--app-only`, `--show-classification`: same behavior as `observe`.
- `--openapi-file <path>`, `--sqlserver-dacpac-file <path>`, `--sqlserver-seed-sql-file <path>`, `--max-operations <count>`, `--target "METHOD /path/template"`, `--dotnet-command <command>`, `--no-build`: same behavior as `replay`.
- `--model-provider openai`, `--openai-api-key <key>`, `--sqlserver-schema-file <path>`, `--openai-base-url <url>`, `--openai-model <id>`: same behavior as `advise`; in `tune`, `--sqlserver-schema-file` overrides the harness manifest schema path
- `--artifact-dir <path>`: choose a different output folder instead of the default timestamped tune folder

If you do not pass `--artifact-dir`, `tune` writes under `artifacts/sqloom/tune/tune-<timestamp>/`. Each run writes:

- `query-store-snapshot.json`
- `tune-summary.json`
- `replay/`

The `replay/` directory then contains the same replay, match, advice, and SQL proposal files you get when you run `replay`, `correlate`, and `advise` separately.

## Replay Requests

Use `replay` when you want to run one or more API operations and capture the SQL they cause.

From the repo root, run:

```powershell
dotnet run --project .\src\Sqloom.Host\Sqloom.Host.csproj -- replay .\tests\Sqloom.TestApp.Harness\Sqloom.TestApp.Harness.csproj --target "GET /api/products/by-category"
```

The `Sqloom.Host` executable uses the same explicit form, for example `replay .\tests\Sqloom.TestApp.Harness\Sqloom.TestApp.Harness.csproj --target "GET /api/products/by-category"`.

Common replay switches:

- `--openapi-file <path>`: use a different OpenAPI document instead of the app's default
- `--max-operations <count>`: cap the number of replayed operations after filtering.
- `--target "METHOD /path/template"`: replay one exact operation such as `GET /api/products/by-category`
- `--dotnet-command <command>`: choose the `dotnet` executable Sqloom should use for nested builds
- `--sqlserver-dacpac-file <path>`: override the harness DACPAC for a SQL Server replay. Sqloom does not build the DACPAC for you.
- `--sqlserver-seed-sql-file <path>`: override the harness SQL seed script after the DACPAC is applied.
- `--artifact-dir <path>`: choose a different replay output folder

Sqloom plays it safe by default. Authenticated `GET` requests can be replayed automatically. Non-`GET` requests must be allowed by the app through `AllowNonGetReplay`.

`--target` is strict. It must use the exact form `METHOD /path/template` with an uppercase HTTP method, one space, a leading `/`, and no trailing `/` or repeated `//`. If you mistype the shape, Sqloom fails fast and suggests the corrected form when it can.

For the sample app, the SQL Server replay path is self-contained. `Sqloom.TestApp.Harness` starts the app in-process, starts a throwaway local SQL Server container, applies the AdventureWorks DACPAC, runs the seed script, and loads only the data needed for the replay. You do not need a pre-built Docker image or local SQL setup beyond the committed harness files.

The app under test owns its OpenAPI document. By default, the harness manifest points Sqloom at that app-owned `openapi.json` using an absolute `OpenApiDocumentPath`. Pass `--openapi-file` to use a different document for one run.

If you do not pass `--artifact-dir`, replay output goes under `artifacts/sqloom/replay/<timestamp>/`. Each run writes:

- `discovered-operations.json`
- `replay-plan.json`
- `replay-summary.json`
- `operations/<ordinal>-<operation>.json`

`replay-summary.json` keeps the app name with the run and makes the next `correlate` and `advise` steps clear. When you supply a DACPAC or seed script, the summary also records the source path, file name, and SHA-256 hash for that file.

Each operation file records the request, response status and body, captured SQL commands, normalized SQL text, fingerprints, and parameter values so later Query Store matching can start from real replay evidence. Support for attaching to an existing local database is planned later and is not part of V1.

## Match Replayed SQL to Query Store

Use `correlate` after replay when you want Sqloom to match the SQL it captured back to a Query Store snapshot:

```powershell
dotnet run --project .\src\Sqloom.Host\Sqloom.Host.csproj -- correlate --replay-artifact-dir ".\artifacts\sqloom\replay\replay-20260608T040506000Z" --query-store-snapshot-file ".\artifacts\sqloom\query-store\query-store-20260608T041245123Z.json" --read-only-connection-string "<connection-string>"
```

By default, Sqloom writes `query-store-correlation.json` under the replay output folder. Each record keeps the replay operation key, route, captured SQL details, possible `statement_sql_handle` values, match kind, confidence, and the matched Query Store rows. The summary groups results by API operation so you can tell which request most likely owns a slow database query.

`query-store-correlation.json` also keeps the app name with the run and makes the next `advise` step clear.

Sqloom uses this ranking:

- `StatementHandleExact`: exact match using `statement_sql_handle`
- `QueryTextExact`: exact match on SQL text after the same outer trimming Query Store uses
- `FingerprintFallback`: local similarity check for troubleshooting only
- `Unmatched`: no safe match found

## Generate Advice

Use `advise` after replay and correlation when you want Sqloom to turn the evidence into tuning suggestions:

```powershell
dotnet run --project .\src\Sqloom.Host\Sqloom.Host.csproj -- advise --replay-artifact-dir ".\artifacts\sqloom\replay\replay-20260608T040506000Z" --model-provider openai --openai-api-key "<api-key>" --sqlserver-schema-file "<schema-file>"
```

Common advice switches:

- `--debug`: print readable, redacted OpenAI request and response data to `stderr`
- `--replay-artifact-dir <path>`: required replay output folder that already contains `query-store-correlation.json`
- `--query-store-correlation-file <path>`: use a different correlation file path
- `--json-output-file <path>`: choose a different advice output file
- `--model-provider openai`: required model provider for this step
- `--openai-api-key <key>`: required API key for the OpenAI-backed advice step
- `--sqlserver-schema-file <path>`: required schema file used as table, column, and existing-index context
- `--openai-base-url <url>`: optional override for the OpenAI base URL. The default is `https://api.openai.com`.
- `--openai-model <id>`: optional override for the OpenAI model id. The default is `gpt-5.4-mini`.

By default, Sqloom reads `query-store-correlation.json` from the replay output folder and writes `tuning-advice.json` next to it. It also writes two SQL-focused review files in that same folder:

- `sql-tuning-proposal.json`
- `sql-tuning-proposal.sql`

Sqloom now uses OpenAI for this step. Before it sends the request, it packages the replay evidence, Query Store match data, any matching snapshot data, and the SQL Server schema file into a single evidence bundle.

Use `--model-provider openai` and pass the OpenAI settings on the command line. This CLI path does not read OpenAI settings from environment variables.

The proposal files are SQL-focused review output built from the replay evidence, the Query Store match data, and the SQL Server schema file. Sqloom keeps the proposal kind returned by the model, and every valid model proposal is written into `tuning-advice.json`, `sql-tuning-proposal.json`, and `sql-tuning-proposal.sql`.

Rollback SQL is helpful but optional in the OpenAI path. If the model leaves out `rollbackSqlScript`, Sqloom still keeps the proposal, records a warning in the advice files, and writes a placeholder rollback note in the `.sql` file. Sqloom no longer invents local SQL proposals from Query Store data alone.

To generate operation advice:

```powershell
dotnet run --project .\src\Sqloom.Host\Sqloom.Host.csproj -- advise --replay-artifact-dir ".\artifacts\sqloom\replay\replay-20260608T040506000Z" --model-provider openai --openai-api-key "<api-key>" --sqlserver-schema-file "<schema-file>" --openai-base-url "https://api.openai.com" --openai-model "gpt-5.4-mini"
```

This uses the same correlation file, sends the evidence bundle and schema text to the Responses API with a strict JSON schema, keeps the model's `proposalKind`, and writes the selected model name into the report.

## How App-Specific Code Fits In

`Sqloom.Host` stays generic and does not reference app code directly. You give it a harness project, harness assembly, solution, solution filter, or directory, and it scans the loadable assembly output for one public non-abstract `ISqloomApplication` implementation.

External harness projects reference `Sqloom.Testing` for the public runner contracts. In this repo, the sample harness project is `Sqloom.TestApp.Harness`. It contains the startup code, replay settings, Query Store profile, DACPAC and seed defaults, and test setup for the sample app. Other apps can follow the same pattern without changing `Sqloom.Host`.

## Set Up a SQL Server or Azure SQL User for Sqloom

Create a dedicated database user for Sqloom. Do not reuse the app's normal database user. Run this while connected to the target SQL Server or Azure SQL database:

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

This keeps the Sqloom connection read-only while still allowing it to read Query Store, inspect metadata, and view query plans.

If the host prints this warning:

```text
Module discovery skipped because VIEW DEFINITION permission is unavailable.
```

grant `VIEW DEFINITION` to the Sqloom principal:

```sql
GRANT VIEW DEFINITION TO [sqloom_ro];
```

That permission only grants read-only metadata access. It lets Sqloom discover stored procedures and functions so those queries can still be recognized as part of the app, but it does not grant `INSERT`, `UPDATE`, or `DELETE`.

If you only need tables and views, you can skip `VIEW DEFINITION` and ignore the warning. If you only need Query Store snapshots, `SHOWPLAN` is optional, but the full Sqloom flow uses both Query Store and plan inspection.

Use the resulting connection string with `--read-only-connection-string` when running `observe` or `correlate`.
