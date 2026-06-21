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

- `Talio.Sqloom` and `Talio.Sqloom.Tests` stay in the Talio repository.
- The standalone `Sqloom.Host` stays generic and loads app-owned companion harnesses without taking a direct Talio-specific dependency.

## Boundary Rules

- No production project may reference anything under `tests/`.
- Keep `Sqloom.Core` free of ASP.NET Core, live SQL connectivity, Testcontainers, and CLI orchestration.
- Keep `Sqloom.QueryStore` free of connection management, app-host bootstrapping, and CLI orchestration.
- Keep CLI argument parsing and stage orchestration inside `Sqloom.Host`.
- Keep provider-specific database concerns out of `Sqloom.AspNetCore` unless they are required at the ASP.NET Core replay boundary.
- Keep project references acyclic and minimal.
- If a capability must support multiple concrete providers or hosts, extract a dedicated abstraction or provider-specific project instead of broadening `Sqloom.Core`.
