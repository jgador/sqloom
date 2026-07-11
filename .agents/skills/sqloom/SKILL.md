---
name: sqloom
description: Guide tune-first Sqloom harness setup and command usage for ASP.NET Core apps.
---

# Sqloom Harness And Commands

Use this skill when helping a user create or update a Sqloom harness, scaffold the repo skill with `sqloom init`, or run Sqloom against an ASP.NET Core app. Default to `sqloom tune` when the user wants tuning or the complete Sqloom workflow. If a usable harness already exists, skip harness generation intake and route to the requested command. Do not generate harness files until the required intake is complete.

## Default To `sqloom tune`

Treat `sqloom tune` as the primary path for tuning. It is the simplest complete workflow because it runs replay -> observe -> correlate -> advise in one command and writes the stage artifacts together.

Use the individual commands as focused tools:

- Use `sqloom replay` for replay-only checks.
- Use `sqloom observe` when the user only needs Query Store evidence.
- Use `sqloom correlate` when replay and Query Store artifacts already exist.
- Use `sqloom advise` when correlation evidence already exists and the user only needs advice or SQL proposals.
- Use `sqloom init` only to scaffold the embedded agent skill into a repository.

## Use Exact Sqloom Syntax

Use [references/commands.md](references/commands.md) for the exact generated Sqloom commands, arguments, defaults, and option names. Do not infer CLI syntax from examples or prose when the generated reference covers it.

- Lead with `sqloom tune` for tuning unless the user asks for a specific stage or artifact-level command.
- Explain that `--replay-data-agent` defaults to `required`; pass `--replay-data-agent off` only when the user wants to rely entirely on harness-supplied replay values.
- For `replay`, pass `--openai-api-key` unless `--replay-data-agent off` is supplied.
- For `tune` and `advise`, pass `--model-provider openai` and `--openai-api-key` for advice generation.
- Do not imply Sqloom automatically reads `OPENAI_API_KEY`; `$env:OPENAI_API_KEY` in examples is shell expansion into `--openai-api-key`.

## Start With Evidence

Inspect the target app, OpenAPI document, and existing tests before asking questions. Look for:

- the ASP.NET Core entry point and hosting model
- an existing public non-abstract `ISqloomApplication` harness
- available OpenAPI JSON and operation IDs
- existing integration or WebApplicationFactory tests
- auth, tenant, and header requirements the harness must supply
- database setup, seed data, DACPAC, and SQL Server connection patterns
- replay overlays for non-GET operations, skips, or app-specific deterministic values

Prefer concrete evidence from the repo over assumptions.

## Ask Only For Missing Required Inputs

Ask at most three questions at a time. By default, Sqloom `replay` and `tune` use Microsoft Agent Framework through `--replay-data-agent required` to fill missing replay path, query, header, and body values. Do not ask the user to provide those HTTP request values unless one of these is true:

- the user will opt out with `--replay-data-agent off`
- the user wants deterministic fixture values in the harness
- the value is app-owned auth, tenant, or security setup that the harness must provide
- the repo already declares app-specific values that should be confirmed

Collect only the missing non-agent inputs needed for the intended command:

- endpoint method and route
- target harness project, harness assembly, solution, solution filter, or directory
- OpenAPI source when the repo does not expose one clearly
- auth and tenant requirements
- database bootstrap choice, such as existing test setup, DACPAC, seed SQL, or user-provided setup, when the app needs it
- OpenAI API key availability for the default replay data agent and advice workflows, without asking the user to paste secrets into source

When the repo already answers one of these, state the observed value and do not ask for it again.

## Generate Only After Intake Completes

Before writing harness files, confirm the complete intake:

- target app project
- OpenAPI source
- endpoint selection
- auth and tenant setup
- database bootstrap path
- whether the default replay data agent stays enabled or the user is opting out with `--replay-data-agent off`

Generate the smallest harness that can start the app, expose `ISqloomApplication`, locate the app-owned OpenAPI document, and run the selected Sqloom command. Keep app-specific setup in the harness; do not add Sqloom runtime features for one app's setup. Keep `ReplayProfile` minimal: do not add personas or overlays solely to supply path, query, header, or body values when the default replay data agent can prepare them. Use overlays for app-owned deterministic values, non-GET opt-in, and skip rules.
