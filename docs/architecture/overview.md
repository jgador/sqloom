# Sqloom Architecture Overview

This is the canonical repo-layout and project-ownership document for the standalone Sqloom repository.

## Pipeline Shape

- User-facing flow: `observe -> replay -> correlate -> advise`
- Convenience front door: `tune` runs the common path and writes the same stage-owned artifacts under `artifacts/sqloom/`
- `Sqloom.Host` and the packaged `sqloom` tool stay host-first and generic

## Top-Level Structure

- `src/`: production libraries and the CLI host
- `tests/`: unit tests, integration tests, the sample app, and its companion integration harness
- `scripts/`: local tooling and packaging automation
- `artifacts/`: generated build, package, replay, and tune output
- `docs/`: architecture notes and repo guidance

## Host and Companion Model

- `Sqloom.Host`: generic runner for project, solution, solution-filter, or directory targets
- `Sqloom.TestApp`: sample target app in this repo
- `Sqloom.TestApp.IntegrationTests`: sample companion harness loaded by the host for generic replay coverage, including optional SQL Server DACPAC bootstrap
- Additional app-owned companion harnesses can follow the same pattern without changing the host
- Replay, correlation, and advice artifacts keep explicit stage metadata so downstream steps stay tied to the right pipeline state

## Current Project Roles

- `Sqloom.Core`: shared contracts, options, artifact layout, pipeline models, generic helpers, and merged Showplan/OpenAI advice contracts
- `Sqloom.QueryStore`: Query Store models, workload classification, and discovery-first catalog logic that can stay independent from live SQL connectivity
- `Sqloom.AzureSql`: Azure SQL and SQL Server connectivity, Query Store collection, statement-handle resolution, replay support, and statistics capture
- `Sqloom.AspNetCore`: OpenAPI discovery, request resolution, replay planning, ASP.NET Core replay orchestration, request-scoped SQL capture hooks, and Query Store correlation types
- `Sqloom.Host`: CLI verbs, argument parsing, target resolution, diagnostics wiring, library-harness loading, and the composition root
- `Sqloom.TestApp`: sample target app for generic host coverage
- `Sqloom.TestApp.IntegrationTests`: sample replay harness, replay profile, optional DACPAC bootstrap, and sample SQL Server setup
- `Sqloom.UnitTests`: unit-test lane for core libraries and host-adjacent logic
- `Sqloom.IntegrationTests`: process and host integration lane for the standalone repository

Retired runtime boundaries stay merged into adjacent survivors: `Sqloom.Showplan -> Sqloom.Core`, `Sqloom.OpenAI -> Sqloom.Core`, and `Sqloom.Correlation -> Sqloom.AspNetCore`.

## Current Repo Direction

- Keep the current `Sqloom.*` names. Do not rename into generic `Domain`, `Application`, or `Infrastructure` buckets unless there is a concrete repo need.
- Keep repo automation in `scripts/` for now.
- Keep `Sqloom.Host` as the only CLI composition root.
- Keep new shared code narrowly owned and task-driven.
