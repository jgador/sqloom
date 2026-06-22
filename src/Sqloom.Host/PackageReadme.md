# sqloom

`sqloom` is a .NET tool for finding slow database work behind API requests in .NET applications. It can read Query Store from SQL Server or Azure SQL, replay API operations against app-specific test harnesses, correlate captured SQL with Query Store, and generate tuning advice plus SQL proposal sidecars.

`tune` runs the full flow: `observe -> replay -> correlate -> advise`.

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
- `replay`: replay API operations against an app-specific test harness and capture SQL
- `correlate`: match replayed SQL back to a Query Store snapshot
- `advise`: send replay evidence, Query Store matches, and a schema file to OpenAI
- `tune`: run the full `observe -> replay -> correlate -> advise` flow

## Required inputs

`observe` and `correlate` require `--read-only-connection-string <connection-string>`.

`advise` and `tune` use OpenAI for the advice step. Pass:

- `--model-provider openai`
- `--openai-api-key <key>`
- `--sqlserver-schema-file <path>`

SQL Server-backed replay harnesses can use a prebuilt DACPAC via `--sqlserver-dacpac-file <path>`. App-owned harnesses can also accept a post-DACPAC SQL seed script via `--sqlserver-seed-sql-file <path>` when they need to restore data into the fresh replay database after publish.

Use the global `--debug` switch when you want stage-owned diagnostics on `stderr`. In particular, `advise --debug` prints readable, redacted OpenAI request and response payloads, and `tune --debug` cascades the same debug mode through `observe`, `replay`, `correlate`, and `advise`.

## Example

```powershell
sqloom replay .\src\MyApi\MyApi.csproj --target "GET /api/orders/{id}"
```

## Install from a local feed

Install the tool from a local folder feed:

```powershell
dotnet tool install --tool-path <tool-path> sqloom --add-source <local-feed-path> --ignore-failed-sources
```

The published `sqloom` tool package is sufficient for both local folder-feed installs and public NuGet.org installs.

See the repository README for the full end-to-end sample and maintainer workflow:

[Sqloom on GitHub](https://github.com/jgador/sqloom)
