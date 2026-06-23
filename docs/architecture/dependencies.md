# Sqloom Dependency Direction

This is the canonical project-graph and boundary-rules document for the standalone Sqloom repository.

## Production Graph

```text
Sqloom.Core
Sqloom.QueryStore -> Sqloom.Core
Sqloom.SqlServer -> Sqloom.Core, Sqloom.QueryStore
Sqloom.AspNetCore -> Sqloom.Core
Sqloom.Correlation -> Sqloom.Core, Sqloom.QueryStore
Sqloom.Testing -> Sqloom.Core, Sqloom.QueryStore
Sqloom.Host -> Sqloom.Core, Sqloom.QueryStore, Sqloom.SqlServer, Sqloom.AspNetCore, Sqloom.Correlation, Sqloom.Testing
```

## Test and Harness Graph

```text
Sqloom.TestApp
Sqloom.TestApp.Harness -> Sqloom.TestApp, Sqloom.AspNetCore, Sqloom.QueryStore, Sqloom.Testing
Sqloom.UnitTests -> production projects, Sqloom.TestApp.Harness
Sqloom.IntegrationTests -> production projects, Sqloom.TestApp, Sqloom.TestApp.Harness
```

## External Composition

- `Sqloom.Host` stays generic and loads app-owned harness assemblies through explicit target paths.
- Harness targets must contain exactly one public non-abstract `ISqloomApplication` implementation.
- App-specific replay harnesses belong with the apps they support, not in the core host composition root.

## Packaging and Publication

- This document describes project references first, then the public package surfaces.
- The public release artifacts are the `sqloom` .NET tool produced from `Sqloom.Host`, the `Sqloom.Testing` harness contract package, and the `Sqloom.Core` / `Sqloom.QueryStore` packages required by the `Sqloom.Testing` package dependency graph.
- `Sqloom.SqlServer`, `Sqloom.AspNetCore`, and `Sqloom.Correlation` remain repo production projects. Package automation may pack them as supporting local-feed artifacts, but external harnesses should compile against `Sqloom.Testing`.

## Boundary Rules

- No production project may reference anything under `tests/`.
- Keep `Sqloom.Core` free of ASP.NET Core, live SQL connectivity, Testcontainers, and CLI orchestration.
- Keep `Sqloom.QueryStore` free of connection management, app-host bootstrapping, and CLI orchestration.
- Keep `Sqloom.Correlation` focused on matching replay evidence to Query Store data; keep ASP.NET Core hosting, live SQL Server connection handling, and CLI orchestration out of it.
- Keep `Sqloom.Testing` limited to public harness contracts and shared runner types needed by external harness projects.
- Keep CLI argument parsing and stage orchestration inside `Sqloom.Host`.
- Keep provider-specific database concerns out of `Sqloom.AspNetCore` unless they are required at the ASP.NET Core replay boundary.
- Keep project references acyclic and minimal.
- If a capability must support multiple concrete providers or hosts, extract a dedicated abstraction or provider-specific project instead of broadening `Sqloom.Core`.
