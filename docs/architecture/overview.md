# Sqloom Architecture Overview

This is the canonical repo-layout and project-ownership document for the standalone Sqloom repository.

## Pipeline Shape

- User-facing tune flow: `replay -> observe -> correlate -> advise`
- Convenience front door: `tune` runs the common path and writes the same stage-owned artifacts under `artifacts/sqloom/`
- `Sqloom.Host` and the packaged `sqloom` tool stay host-first and generic

## Top-Level Structure

- [src/](../../src/): production libraries and the CLI host
- [extensions/sqloom/](../../extensions/sqloom/): VS Code extension preview that shells out to the `sqloom` CLI, anchors the Activity Bar logo with a compact webview launcher, and renders the Sqloom Tune dashboard webview
- [tests/](../../tests/): unit tests, integration tests, the sample app, and its app-owned harness
- [scripts/](../../scripts/): local tooling and packaging automation
- `artifacts/`: generated build, package, replay, and tune output
- [docs/](../): architecture notes and repo guidance

## Host and Harness Model

- `Sqloom.Host`: generic runner for explicit .NET 10 C# file-based harness, harness project, harness assembly, solution, solution-filter, or directory targets
- `Sqloom.Testing`: harness contract library containing `ISqloomApplication`, `ISqloomApplicationSession`, manifest types, the shared `Sqloom.Pipeline.*` pipeline surface, and persisted artifact models
- `Sqloom.TestApp`: sample target app in this repo
- `tests/Sqloom/Sqloom.TestApp/default/Harness.cs`: consumer-style sample harness loaded directly by the host for replay coverage and local SQL Server-backed sample runs
- Additional app-owned harnesses can follow the same pattern without changing the host
- App-owned C# harness source is generated once and checked in as durable test support, defaulting to `tests/Sqloom/<app>/<profile>/Harness.cs`; endpoint selection stays a per-command `--target` input
- The host always builds explicit `.cs` targets with .NET SDK 10 or later into isolated system-temporary output, loads each build through an isolated assembly context, and rejects `--no-build` for them
- The host scans loadable harness assemblies for public non-abstract `ISqloomApplication` implementations and requires exactly one implementation for a run
- Replay, correlation, and advice artifacts keep explicit stage metadata so downstream steps stay tied to the right pipeline state

## Current Project Roles

- `Sqloom.Testing`: app-harness runner contracts, manifest types, harness-facing ASP.NET Core SQL capture helpers, shared pipeline surface, artifact layout, pipeline models, replay evidence models, endpoint/replay artifact schemas, Query Store evidence models, correlation report models, workload classification helpers, and merged Showplan/OpenAI advice contracts
- `Sqloom.Host`: CLI verbs, argument parsing, target resolution, diagnostics wiring, library-harness loading, ASP.NET Core replay implementation, live SQL Server Query Store collection, statement-handle resolution, replay-to-Query Store correlation, DACPAC schema extraction, advice generation, and the composition root
- `extensions/sqloom`: VS Code Activity Bar logo, compact webview launcher, Command Palette commands, and webview dashboard surface over the public `sqloom` CLI
- `Sqloom.TestApp`: sample target app for generic host coverage
- `tests/Sqloom/Sqloom.TestApp/default/Harness.cs`: standalone app-owned replay harness, public-package consumer, and file-app resolution fixture
- `Sqloom.UnitTests`: unit-test lane for shared pipeline code and host-adjacent logic
- `Sqloom.IntegrationTests`: process and host integration lane for the standalone repository

Retired runtime boundaries stay merged into adjacent survivors: Showplan, OpenAI advice, and Query Store pipeline models live under `Sqloom.Testing` / `Sqloom.Pipeline.*`; SQL Server host work lives under `Sqloom.Host`; ASP.NET Core capture lives under `Sqloom.Host` / `Sqloom.Testing`; and correlation orchestration lives under `Sqloom.Host` / `Sqloom.Testing`.

## Current Repo Direction

- Keep the current surviving `Sqloom.*` names. Do not rename into generic `Domain`, `Application`, or `Infrastructure` buckets unless there is a concrete repo need.
- Keep VS Code extension code under `extensions/sqloom` and route shared TypeScript helpers through `packages/` only when more than one extension target needs them.
- Keep repo automation in [scripts/](../../scripts/) for now.
- Keep `Sqloom.Host` as the only CLI composition root.
- Keep new shared code narrowly owned and task-driven.
