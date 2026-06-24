# Sqloom Dependency Direction

This is the canonical project-graph and boundary-rules document for the standalone Sqloom repository.

## Production Graph

```text
Sqloom.Core
Sqloom.Testing -> Sqloom.Core
Sqloom.Host -> Sqloom.Core, Sqloom.Testing
```

## Test and Harness Graph

```text
Sqloom.TestApp
Sqloom.TestApp.Harness -> Sqloom.TestApp, Sqloom.Core, Sqloom.Testing
Sqloom.UnitTests -> Sqloom.Core, Sqloom.Testing, Sqloom.Host, Sqloom.TestApp.Harness
Sqloom.IntegrationTests -> Sqloom.Core, Sqloom.Testing, Sqloom.Host, Sqloom.TestApp, Sqloom.TestApp.Harness
```

## External Composition

- `Sqloom.Host` stays generic and loads app-owned harness assemblies through explicit target paths.
- Harness targets must contain exactly one public non-abstract `ISqloomApplication` implementation.
- App-specific replay harnesses belong with the apps they support, not in the core host composition root.

## Packaging and Publication

- This document describes project references first, then the public package surface.
- The public release artifact is the `sqloom` .NET tool produced from `Sqloom.Host`.
- The package-prep flow may emit `Sqloom.Core` and `Sqloom.Testing` packages into the local folder feed for verification, but they are not public upload targets for the tool-only release.
- SQL Server observation, DACPAC schema extraction, ASP.NET Core replay, Query Store correlation, and advice stage implementations live in `Sqloom.Host`.

## Boundary Rules

- No production project may reference anything under `tests/`.
- Keep `Sqloom.Core` free of ASP.NET Core, live SQL connectivity, Testcontainers, and CLI orchestration.
- Keep `Sqloom.Core` limited to provider-neutral contracts, persisted artifact schemas, replay evidence models, Query Store evidence models, correlation report models, and shared pure helpers.
- Keep `Sqloom.Testing` limited to harness contracts and harness-facing ASP.NET Core capture helpers.
- Keep CLI argument parsing, stage orchestration, ASP.NET Core replay, live SQL Server connectivity, DACPAC schema extraction, Query Store collection, correlation implementation, and advice implementation inside `Sqloom.Host`.
- Keep project references acyclic and minimal.
- If a capability must support multiple concrete providers or hosts, extract a dedicated abstraction or provider-specific project instead of broadening `Sqloom.Core`.
