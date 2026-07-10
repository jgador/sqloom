# Sqloom

Sqloom helps you find slow database work behind API requests in a .NET app. It runs a selected API request inside your app's test harness, captures the SQL that request executes, reads SQL Server or Azure SQL Query Store, matches the captured SQL back to Query Store evidence, and writes tuning advice plus SQL proposal files.

Most users start with `sqloom tune`. It runs the full workflow:

```text
replay -> observe -> correlate -> advise
```

This repo includes a sample app and harness for `GET /api/products/by-category`. The quick start uses the sample harness DACPAC and seed defaults, then points Sqloom at a read-only Query Store connection so the Query Store source is explicit.

![Sqloom tuning pipeline diagram](docs/images/sqloom-diagram.png)

## Install

Install the public tool from NuGet.org:

```powershell
dotnet tool install --global sqloom
sqloom --help
```

Update an existing install with:

```powershell
dotnet tool update --global sqloom
```

## Quick Start

Set `OPENAI_API_KEY`, then run the sample `tune` workflow from the repo root. The sample harness supplies replay DACPAC and seed defaults, so the command only needs the harness project, target operation, read-only Query Store connection, replay-data agent mode, and OpenAI settings.

```powershell
sqloom-local tune .\tests\Sqloom.TestApp.Harness\Sqloom.TestApp.Harness.csproj `
 --target "GET /api/products/by-category" `
 --read-only-connection-string "Server=localhost;Database=AdventureWorksLT2025;Integrated Security=True;TrustServerCertificate=True;MultipleActiveResultSets=True" `
 --replay-data-agent required `
 --model-provider openai `
 --openai-api-key $env:OPENAI_API_KEY `
 --openai-model "gpt-5.4-mini" `
 --debug
```

That command starts the sample harness, uses the harness DACPAC and seed SQL defaults to prepare the replay database, runs the selected API request, captures the SQL it caused, reads Query Store through the supplied read-only connection string, correlates the captured SQL to Query Store rows, and asks OpenAI for operation-level tuning advice. If `tune` receives a command-line read-only connection string and no `--sqlserver-dacpac-file` or harness manifest DACPAC is available, Sqloom exports `sqlserver-schema-source.dacpac` before replay and reuses it for advice schema extraction. `--sqlserver-dacpac-file` and `--sqlserver-seed-sql-file` remain replay bootstrap overrides, and `--sqlserver-schema-file` remains the expert advice-only schema SQL override. `--debug` prints stage details to `stderr`, including redacted OpenAI request and response details during the advice step.

The run writes a timestamped folder under `artifacts/sqloom/tune/`, including:

- `query-store-snapshot.json`
- `tune-summary.json`
- `replay/replay-data-prep.json` when `--replay-data-agent` is enabled
- `replay/query-store-correlation.json`
- `replay/sqlserver-schema-source.dacpac` when Sqloom exports the schema source from the read-only connection
- `replay/sqlserver-dacpac-extract/model.sql` when schema is extracted from a DACPAC
- `replay/sqlserver-schema.sql`
- `replay/tuning-advice.json`
- `replay/sql-tuning-proposal.json`
- `replay/sql-tuning-proposal.sql`

The important review artifact is usually `replay/sql-tuning-proposal.sql`, with the JSON files available when you want the full evidence chain.

If the harness does not already provide enough path, query, header, or body values for replay, add `--replay-data-agent auto` with `--openai-api-key`. Sqloom uses Microsoft Agent Framework with OpenAI to fill missing replay inputs before the replay stage and keeps the generated values in `replay/replay-data-prep.json`. `off` is the only non-agent mode.

## Commands

Sqloom has one setup command, one common front door, and four lower-level stages:

- `init`: scaffold the `sqloom` agent skill into this repository.
- `tune`: run `replay -> observe -> correlate -> advise` in one command.
- `replay`: run API operations through an `ISqloomApplication` harness and capture SQL.
- `observe`: read recent Query Store data from SQL Server or Azure SQL.
- `correlate`: match replay-captured SQL back to a Query Store snapshot.
- `advise`: turn replay, correlation, and schema evidence into tuning advice and SQL proposal files.

See [docs/command-reference.md](docs/command-reference.md) for the exhaustive command syntax, arguments, defaults, outputs, and additional options.

## How It Fits Into An App

Sqloom stays generic. Your app supplies a small harness project that exposes exactly one public non-abstract `ISqloomApplication`. The harness tells Sqloom where the app-owned OpenAPI document lives, how to start the app for replay, and which replay defaults are safe for that app.

In this repo:

- [src/Sqloom.Core](src/Sqloom.Core) owns shared contracts and persisted artifact models.
- [src/Sqloom.Testing](src/Sqloom.Testing) owns harness contracts and ASP.NET Core capture helpers.
- [src/Sqloom.Host](src/Sqloom.Host) owns the CLI, harness loading, replay, Query Store collection, correlation, schema extraction, and advice generation.
- [tests/Sqloom.TestApp.Harness](tests/Sqloom.TestApp.Harness) is the sample app-specific harness used by the quick start.

For more detail about repo layout and boundaries, see [docs/architecture/overview.md](docs/architecture/overview.md) and [docs/architecture/dependencies.md](docs/architecture/dependencies.md).

## Build And Test

Build from the repo root:

```powershell
dotnet restore .\Sqloom.slnx
dotnet build .\Sqloom.slnx --tl:off --nologo "-clp:ErrorsOnly;NoSummary"
```

Run the test lanes:

```powershell
dotnet test --solution .\Sqloom.UnitTests.slnf
dotnet test --solution .\Sqloom.IntegrationTests.slnf
```

Use `sqloom-local` only when you are changing Sqloom itself and want a local tool install separate from the public `sqloom` command:

```powershell
pwsh .\scripts\deploy-sqloom-local.ps1
sqloom-local --version
```

For package preparation and release workflow, see [docs/dotnet-tool-release.md](docs/dotnet-tool-release.md).
