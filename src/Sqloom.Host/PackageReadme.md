# sqloom

`sqloom` is a .NET tool for finding slow database work behind API requests in .NET applications. It can read Query Store from SQL Server or Azure SQL, replay API operations through app-specific harnesses, correlate captured SQL with Query Store, and generate tuning advice plus SQL proposal sidecars.

`tune` runs the full harness flow: `replay -> observe -> correlate -> advise`.

## Install from NuGet.org

```powershell
dotnet tool install --global sqloom
sqloom --help
```

To update an existing install:

```powershell
dotnet tool update --global sqloom
```

## Main commands

- `observe`: read recent Query Store data with an explicit `--read-only-connection-string <connection-string>`
- `replay`: replay API operations through an app-specific harness and capture SQL
- `correlate`: match replayed SQL back to a Query Store snapshot
- `advise`: send replay evidence, Query Store matches, and a schema file to OpenAI
- `tune`: run the full `replay -> observe -> correlate -> advise` flow

## Required inputs

`observe` and `correlate` require `--read-only-connection-string <connection-string>`. `tune` can use either `--read-only-connection-string` or a read-only connection string supplied by the harness session.

`advise` and `tune` use OpenAI for the advice step. Pass:

- `--model-provider openai`
- `--openai-api-key <key>`
- `--sqlserver-schema-file <path>` unless `tune` can use a schema path from the harness manifest

SQL Server-backed replay harnesses can provide default DACPAC, seed script, schema, replay profile, and Query Store profile values. CLI switches such as `--sqlserver-dacpac-file <path>`, `--sqlserver-seed-sql-file <path>`, `--sqlserver-schema-file <path>`, and `--read-only-connection-string <connection-string>` override harness defaults.

Use the global `--debug` switch when you want stage-owned diagnostics on `stderr`. In particular, `advise --debug` prints readable, redacted OpenAI request and response payloads, and `tune --debug` cascades the same debug mode through `observe`, `replay`, `correlate`, and `advise`.

## Example

```powershell
sqloom replay .\tests\MyApi.Harness\MyApi.Harness.csproj --target "GET /api/orders/{id}"
```

Harness projects reference the public `Sqloom.Testing` package and expose exactly one public non-abstract `ISqloomApplication` implementation for the app under test. The CLI accepts a harness project, harness assembly, solution, solution filter, or directory containing harness projects.

## Install from a local feed

Install the tool from a local folder feed:

```powershell
dotnet tool install --tool-path <tool-path> sqloom --add-source <local-feed-path> --ignore-failed-sources
```

The published `sqloom` tool package is sufficient for local folder-feed installs and public NuGet.org installs. App harness projects compile against the published `Sqloom.Testing` package.

See the repository README for the full end-to-end sample and maintainer workflow:

[Sqloom on GitHub](https://github.com/jgador/sqloom)
