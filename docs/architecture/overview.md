# Sqloom Architecture Overview

This is the canonical repo-layout and project-ownership document for the standalone Sqloom repository.

## Pipeline Shape

- User-facing tune flow: `replay -> observe -> correlate -> advise`
- Convenience front door: `tune` runs the common path and writes the same stage-owned artifacts under `artifacts/sqloom/`
- `Sqloom.Host` and the packaged `sqloom` tool stay host-first and generic

## Top-Level Structure

- `src/`: production libraries and the CLI host
- `tests/`: unit tests, integration tests, the sample app, and its app-owned harness
- `scripts/`: local tooling and packaging automation
- `artifacts/`: generated build, package, replay, and tune output
- `docs/`: architecture notes and repo guidance

## Host and Harness Model

- `Sqloom.Host`: generic runner for harness project, harness assembly, solution, solution-filter, or directory targets
- `Sqloom.Testing`: public harness contract package containing `ISqloomApplication`, `ISqloomApplicationSession`, and manifest types that external app harnesses implement
- `Sqloom.TestApp`: sample target app in this repo
- `Sqloom.TestApp.Harness`: sample harness loaded by the host for generic replay coverage, SQL Server DACPAC bootstrap, seed setup, Query Store profile, and schema defaults
- Additional app-owned harnesses can follow the same pattern without changing the host
- The host scans loadable harness assemblies for public non-abstract `ISqloomApplication` implementations and requires exactly one implementation for a run
- Replay, correlation, and advice artifacts keep explicit stage metadata so downstream steps stay tied to the right pipeline state

## Current Project Roles

- `Sqloom.Core`: shared contracts, options, artifact layout, pipeline models, replay evidence models, generic helpers, and merged Showplan/OpenAI advice contracts
- `Sqloom.QueryStore`: Query Store models, workload classification, and discovery-first catalog logic that can stay independent from live SQL connectivity
- `Sqloom.SqlServer`: SQL Server and Azure SQL connectivity, Query Store collection, statement-handle resolution, replay support, and statistics capture
- `Sqloom.AspNetCore`: OpenAPI discovery, request resolution, replay planning, ASP.NET Core replay orchestration, and request-scoped SQL capture hooks
- `Sqloom.Correlation`: replay evidence to Query Store matching, correlation reports, match summaries, and baseline correlation-driven advice heuristics
- `Sqloom.Testing`: public app-harness runner contracts used by external harness projects and the sample harness
- `Sqloom.Host`: CLI verbs, argument parsing, target resolution, diagnostics wiring, library-harness loading, and the composition root
- `Sqloom.TestApp`: sample target app for generic host coverage
- `Sqloom.TestApp.Harness`: sample replay harness, replay profile, DACPAC bootstrap, seed setup, Query Store profile, and sample SQL Server setup
- `Sqloom.UnitTests`: unit-test lane for core libraries and host-adjacent logic
- `Sqloom.IntegrationTests`: process and host integration lane for the standalone repository

Retired runtime boundaries stay merged into adjacent survivors: `Sqloom.Showplan -> Sqloom.Core` and `Sqloom.OpenAI -> Sqloom.Core`.

## Current Repo Direction

- Keep the current `Sqloom.*` names. Do not rename into generic `Domain`, `Application`, or `Infrastructure` buckets unless there is a concrete repo need.
- Keep repo automation in `scripts/` for now.
- Keep `Sqloom.Host` as the only CLI composition root.
- Keep new shared code narrowly owned and task-driven.
