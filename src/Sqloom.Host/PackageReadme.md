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

## Command reference

Use `tune` for the full `replay -> observe -> correlate -> advise` flow. Use `replay`, `observe`, `correlate`, or `advise` when you need a focused stage, and `init` when you want to scaffold the bundled `sqloom` agent skill into a repository.

Use the Sqloom command reference bundled with the agent skill, or `sqloom help <command>` from an installed tool, for exact syntax, required options, allowed values, defaults, and command-specific notes.

SQL Server-backed replay harnesses can provide replay profile, Query Store profile, and app startup behavior. The Microsoft Agent Framework replay data agent fills HTTP replay path, query, header, and body values only; it does not generate DACPACs or seed SQL.

## Next steps

For a runnable end-to-end sample, use the repository README. For exact command syntax, use the command reference or `sqloom help <command>` from the installed tool.

Harness projects expose exactly one public non-abstract `ISqloomApplication` implementation for the app under test. The CLI accepts a harness project, harness assembly, solution, solution filter, or directory containing harness projects.

## Install from a local feed

Install the tool from a local folder feed:

```powershell
dotnet tool install --tool-path <tool-path> sqloom --add-source <local-feed-path> --ignore-failed-sources
```

The published `sqloom` tool package is sufficient for local folder-feed installs and public NuGet.org installs.

See the repository README for the full end-to-end sample and maintainer workflow:

[Sqloom on GitHub](https://github.com/jgador/sqloom)
