# Sqloom Dependency Direction

This is the canonical project-graph and boundary-rules document for the standalone Sqloom repository.

## Production Graph

```text
Sqloom.Testing
Sqloom.Host -> Sqloom.Testing
```

## Test and Harness Graph

```text
Sqloom.TestApp
Sqloom.TestApp.Harness -> Sqloom.TestApp, Sqloom.Testing
Sqloom.UnitTests -> Sqloom.Testing, Sqloom.Host, Sqloom.TestApp.Harness
Sqloom.IntegrationTests -> Sqloom.Testing, Sqloom.Host, Sqloom.TestApp, Sqloom.TestApp.Harness
```

## External Composition

- `Sqloom.Host` stays generic and loads app-owned harness assemblies through explicit target paths.
- Harness targets must contain exactly one public non-abstract `ISqloomApplication` implementation.
- App-specific replay harnesses belong with the apps they support, not in the generic host composition root.

## Packaging and Publication

- This document describes project references first, then the public package surface.
- Public releases include the `Sqloom.Testing` harness/pipeline package and the `sqloom` .NET tool produced from `Sqloom.Host`.
- `Sqloom.Testing` contains the `Sqloom.Pipeline.*` pipeline namespaces, and package preparation verifies that a consumer project can restore and build against `Sqloom.Testing` alone.
- SQL Server observation, DACPAC schema extraction, ASP.NET Core replay, Query Store correlation, and advice stage implementations live in `Sqloom.Host`.

## Boundary Rules

- No production project may reference anything under [tests/](../../tests/).
- Keep the `Sqloom.Pipeline.*` namespaces limited to provider-neutral pipeline models, persisted artifact schemas, replay evidence models, Query Store evidence models, correlation report models, and shared pure helpers.
- Keep the harness-facing `Sqloom.Testing.*` namespaces limited to harness contracts and harness-facing ASP.NET Core capture helpers.
- Keep CLI argument parsing, stage orchestration, ASP.NET Core replay, live SQL Server connectivity, DACPAC schema extraction, Query Store collection, correlation implementation, and advice implementation inside `Sqloom.Host`.
- Keep project references acyclic and minimal.
- If a capability must support multiple concrete providers or hosts, extract a dedicated abstraction or provider-specific project instead of broadening the `Sqloom.Testing` package.
