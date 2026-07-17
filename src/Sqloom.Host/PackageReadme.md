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

Use `tune` for the full `replay -> observe -> correlate -> advise` flow. Use `endpoints` to list source-discovered controller operations without starting a harness. Use `replay`, `observe`, `correlate`, or `advise` when you need a focused stage, and `init` when you want to scaffold the bundled `sqloom` agent skill into a repository.

Use the Sqloom command reference bundled with the agent skill, or `sqloom help <command>` from an installed tool, for exact syntax, required options, allowed values, defaults, and command-specific notes.

SQL Server-backed replay harnesses can provide replay profile, Query Store profile, and app startup behavior. The Microsoft Agent Framework replay data agent fills HTTP replay path, query, header, and body values only; it does not generate DACPACs or seed SQL.

## Next steps

For a runnable end-to-end sample, use the repository README. For exact command syntax, use the command reference or `sqloom help <command>` from the installed tool.

Harnesses expose exactly one public non-abstract `ISqloomApplication` implementation for the app under test. The CLI accepts an explicit .NET 10 C# file-based harness, harness project, harness assembly, solution, solution filter, or directory containing harness projects. Sqloom always builds `.cs` targets and rejects `--no-build` for them.

For an app-owned file-based harness outside the Sqloom repository, generate it once as committed test-support source and pin `Sqloom.Testing` to the installed tool version:

```csharp
#:sdk Microsoft.NET.Sdk.Web
#:property TargetFramework=net10.0
#:property ManagePackageVersionsCentrally=false
#:package Sqloom.Testing@<sqloom-version>
#:project <relative-app-project.csproj>
```

Existing project-backed harnesses remain supported and use a normal package reference:

```powershell
dotnet add package Sqloom.Testing
```

`Sqloom.Testing` contains the shared `Sqloom.Pipeline.*` namespaces for replay, Query Store, artifact, and advice pipeline types.

## Install from a local feed

Install the tool from a local folder feed:

```powershell
dotnet tool install --tool-path <tool-path> sqloom --add-source <local-feed-path> --ignore-failed-sources
```

The published `sqloom` tool package is sufficient for CLI installs. File-based and project-backed harnesses still need the matching `Sqloom.Testing` library package at compile time.

See the repository README for the full end-to-end sample and maintainer workflow:

[Sqloom on GitHub](https://github.com/jgador/sqloom)
