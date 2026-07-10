---
name: sqloom
description: Guide an interactive intake workflow before generating Sqloom harness files for an ASP.NET Core app.
---

# Sqloom Harness Intake

Use this skill when helping a user create or update a Sqloom harness. Do not generate harness files until the required intake is complete.

## Start With Evidence

Inspect the target app, OpenAPI document, and existing tests before asking questions. Look for:

- the ASP.NET Core entry point and hosting model
- available OpenAPI JSON and operation IDs
- existing integration or WebApplicationFactory tests
- auth, tenant, and header requirements
- database setup, seed data, and SQL Server connection patterns

Prefer concrete evidence from the repo over assumptions.

## Ask Only For Missing Required Inputs

Ask at most three questions at a time. Collect the missing values needed to replay one endpoint:

- endpoint method and route
- path, query, header, and body values
- auth and tenant requirements
- database bootstrap choice, such as existing test setup, DACPAC, seed SQL, or user-provided setup
- expected HTTP status

When the repo already answers one of these, state the observed value and do not ask for it again.

## Generate Only After Intake Completes

Before writing harness files, confirm the complete intake:

- target app project
- OpenAPI source
- endpoint selection
- request values
- auth and tenant setup
- database bootstrap path
- expected status

Generate the smallest harness that can start the app, expose `ISqloomApplication`, locate the app-owned OpenAPI document, and replay the selected endpoint. Keep app-specific setup in the harness; do not add Sqloom runtime features for one app's setup.

## Use Exact Sqloom Syntax

After intake, use [references/commands.md](references/commands.md) for the exact generated Sqloom commands, arguments, defaults, and option names. Do not infer CLI syntax from examples or prose when the generated reference covers it.
