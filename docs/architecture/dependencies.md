# Sqloom Dependency Direction

This is the canonical project-graph and boundary-rules document for the standalone Sqloom repository.

## Production Graph

```text
Sqloom.Core
Sqloom.QueryStore -> Sqloom.Core
Sqloom.AzureSql -> Sqloom.Core, Sqloom.QueryStore
Sqloom.AspNetCore -> Sqloom.Core, Sqloom.QueryStore
Sqloom.Host -> Sqloom.Core, Sqloom.QueryStore, Sqloom.AzureSql, Sqloom.AspNetCore
```

## Test and Harness Graph

```text
Sqloom.TestApp
Sqloom.TestApp.IntegrationTests -> Sqloom.TestApp, Sqloom.Core, Sqloom.QueryStore, Sqloom.AspNetCore
Sqloom.UnitTests -> production projects, Sqloom.TestApp.IntegrationTests
Sqloom.IntegrationTests -> production projects, Sqloom.TestApp, Sqloom.TestApp.IntegrationTests
```

## External Composition

- `Sqloom.Host` stays generic and loads app-owned companion harnesses through project discovery and `SqloomTargetProject` mappings.
- App-specific replay harnesses belong with the apps they support, not in the core `Sqloom.*` production graph.

## Packaging and Publication

- This document describes internal project references, not a public NuGet package split.
- The public release artifact is the `sqloom` .NET tool produced from `Sqloom.Host`.
- `Sqloom.Core`, `Sqloom.QueryStore`, `Sqloom.AzureSql`, and `Sqloom.AspNetCore` remain internal production projects in this repository and are not part of the normal public NuGet publish flow as separate packages.

## Boundary Rules

- No production project may reference anything under `tests/`.
- Keep `Sqloom.Core` free of ASP.NET Core, live SQL connectivity, Testcontainers, and CLI orchestration.
- Keep `Sqloom.QueryStore` free of connection management, app-host bootstrapping, and CLI orchestration.
- Keep CLI argument parsing and stage orchestration inside `Sqloom.Host`.
- Keep provider-specific database concerns out of `Sqloom.AspNetCore` unless they are required at the ASP.NET Core replay boundary.
- Keep project references acyclic and minimal.
- If a capability must support multiple concrete providers or hosts, extract a dedicated abstraction or provider-specific project instead of broadening `Sqloom.Core`.
