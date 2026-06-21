# Sqloom Architecture Overview

This document explains how the current `Sqloom.*` projects are laid out and what each one owns.

## Top-Level Structure

- `src/`: production libraries and the CLI host
- `tests/`: unit tests, integration tests, the sample app, and its companion integration harness
- `scripts/`: local tooling and packaging automation
- `artifacts/`: generated build, package, replay, and tune output
- `docs/`: architecture notes and repo guidance

## Current Project Roles

- `Sqloom.Core`: lowest-level shared contracts, options, artifact layout, pipeline models, and generic helpers
- `Sqloom.QueryStore`: Query Store models, workload classification, and correlation-adjacent logic that can remain independent from live SQL connectivity
- `Sqloom.AzureSql`: Azure SQL and SQL Server connectivity, collection, statement-handle resolution, and database-backed replay support
- `Sqloom.AspNetCore`: OpenAPI discovery, request resolution, replay planning, and ASP.NET Core replay orchestration
- `Sqloom.Host`: CLI verbs, argument parsing, target resolution, diagnostics wiring, and the composition root
- `Sqloom.TestApp`: executable sample target used for generic host coverage
- `Sqloom.TestApp.IntegrationTests`: companion replay harness for the sample app

## How the Standard Applies Here

- Keep the current `Sqloom.*` names. The standard guides responsibilities and dependency direction, not a forced rename into generic `Domain` or `Infrastructure` buckets.
- Keep `Sqloom.Core` focused on shared primitives. If a capability is provider-specific, host-specific, or ASP.NET Core-specific, it belongs in a more specific project.
- Treat `Sqloom.Host` as the only CLI composition root. Reusable runtime logic should live in libraries, not in command handlers.
- Treat `Sqloom.TestApp` and `Sqloom.TestApp.IntegrationTests` as sample and harness code, not as production dependencies.
- If a stable extension point is needed across more than one provider or host, prefer a dedicated abstraction project over broadening `Sqloom.Core`.

## Current Repo Direction

- Keep the current `Sqloom.*` project names. Do not force a generic `Domain` / `Application` / `Infrastructure` rename unless there is a concrete repo need.
- Keep `scripts/` at the repo root for now. Do not move repo automation into `eng/` unless the engineering layer grows enough to justify that split.
- Make structural refactors incremental and task-driven rather than rewriting the whole project graph at once.
- Preserve the current dependency direction and keep new shared code narrowly owned.
