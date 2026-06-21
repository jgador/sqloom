# Sqloom Dependency Direction

Sqloom keeps a small, directional project graph. New project references should preserve this shape.

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

## Rules for Future Changes

- No production project may reference anything under `tests/`.
- Do not add Azure SQL, SQL Server, EF Core host bootstrapping, or Testcontainers dependencies to `Sqloom.Core`.
- Do not add live connection management, app-host bootstrapping, or CLI orchestration to `Sqloom.QueryStore`.
- Do not add CLI argument parsing or command orchestration outside `Sqloom.Host`.
- Do not add provider-specific database concerns to `Sqloom.AspNetCore` unless they are required for replay orchestration at the ASP.NET Core boundary.
- Keep project references acyclic and minimal.
- If a capability must support multiple concrete providers or hosts, extract a dedicated abstraction or provider-specific project instead of routing everything through `Sqloom.Core`.
