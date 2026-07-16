---
name: sqloom
description: Guide tune-first Sqloom harness setup and command usage for ASP.NET Core apps.
---

# Sqloom Harness And Commands

Use this skill when helping a user create or update a Sqloom harness, scaffold the Sqloom skill with `sqloom init`, or run Sqloom against an ASP.NET Core app. Default to `sqloom tune` when the user wants tuning or the complete Sqloom workflow. If a usable harness already exists, skip harness generation intake and route to the requested command. Do not generate harness files until the required intake is complete.

## Default To `sqloom tune`

Treat `sqloom tune` as the primary path for tuning. It is the simplest complete workflow because it runs replay -> observe -> correlate -> advise in one command and writes the stage artifacts together.

Use the individual commands as focused tools:

- Use `sqloom replay` for replay-only checks.
- Use `sqloom observe` when the user only needs Query Store evidence.
- Use `sqloom correlate` when replay and Query Store artifacts already exist.
- Use `sqloom advise` when correlation evidence already exists and the user only needs advice or SQL proposals.
- Use `sqloom init` only to scaffold the Sqloom agent skill into a repository.

## Use Exact Sqloom Syntax

Use [references/commands.md](references/commands.md) for exact Sqloom commands, arguments, required options, defaults, allowed values, and option names. Do not infer CLI syntax from examples or prose when the command reference covers it.

- Lead with `sqloom tune` for tuning unless the user asks for a specific stage or artifact-level command.
- Before forming a command, check the command reference for current provider, API key, replay-data-agent, schema-source, and connection-string requirements.
- Treat `$env:OPENAI_API_KEY` examples as shell expansion into the `--openai-api-key` option, not as automatic Sqloom environment-variable loading.

## Reference `Sqloom.Testing` From Consumer Harnesses

Default new harnesses to a .NET 10 C# file-based app that lives in the app repository as durable test-support source. Follow an established repository convention when one exists; otherwise generate `tests/Sqloom/<app>/<profile>/Harness.cs` in a dedicated non-project directory. Keep it outside `artifacts/` and outside an existing SDK project directory unless that project explicitly excludes the file from its compile globs. Pin the public `Sqloom.Testing` package to the installed Sqloom tool version and reference the target app project with file-app directives:

```csharp
#:sdk Microsoft.NET.Sdk.Web
#:property TargetFramework=net10.0
#:property ManagePackageVersionsCentrally=false
#:package Sqloom.Testing@<sqloom-version>
#:project <relative-app-project.csproj>
```

Determine `<sqloom-version>` from `sqloom --version`. Keep the path after `#:project` relative to `Harness.cs` when practical. The file needs a harmless valid entry point and exactly one public non-abstract `ISqloomApplication` implementation. Generate it once, commit it, and maintain its app startup, authentication, tenant, database, and replay setup like an integration-test fixture.

Existing project-backed harnesses remain supported. When updating one, keep its normal NuGet `PackageReference` to `Sqloom.Testing`; do not convert a working harness unless the user requests file-based generation.

Use `using Sqloom.Testing;` for `ISqloomApplication`, manifest, and session contracts. Use `using Sqloom.Testing.AspNetCore;` only when the harness needs the ASP.NET Core replay SQL capture helpers. Do not ask the user to reference `Sqloom.Pipeline` directly for normal harness work; `Sqloom.Testing` contains the shared `Sqloom.Pipeline.*` pipeline surface.

## Start With Evidence

Inspect the target app, OpenAPI document, and existing tests before asking questions. Look for:

- the ASP.NET Core entry point and hosting model
- an existing public non-abstract `ISqloomApplication` harness
- available OpenAPI JSON and operation IDs
- existing integration or WebApplicationFactory tests
- auth, tenant, and header requirements the harness must supply
- database setup, seed data, DACPAC, and SQL Server connection patterns
- replay overlays for non-GET operations, skips, or app-specific deterministic values

Prefer concrete evidence from the target repository over assumptions.

## Ask Only For Missing Required Inputs

Ask at most three questions at a time. By default, Sqloom `replay` and `tune` use Microsoft Agent Framework through `--replay-data-agent required` to fill missing replay path, query, header, and body values. Do not ask the user to provide those HTTP request values unless one of these is true:

- the user will opt out with `--replay-data-agent off`
- the user wants deterministic fixture values in the harness
- the value is app-owned auth, tenant, or security setup that the harness must provide
- the target repository already declares app-specific values that should be confirmed

Collect only the missing non-agent inputs needed for the intended command:

- endpoint method and route
- target C# file-based harness, harness project, harness assembly, solution, solution filter, or directory
- OpenAPI source when the target repository does not expose one clearly
- auth and tenant requirements
- database bootstrap choice, such as existing test setup, DACPAC, seed SQL, or user-provided setup, when the app needs it
- OpenAI API key availability for the default replay data agent and advice workflows, without asking the user to paste secrets into source

When the target repository already answers one of these, state the observed value and do not ask for it again.

## Generate Only After Intake Completes

Before writing harness files, confirm the complete intake:

- target app project
- OpenAPI source
- endpoint selection
- auth and tenant setup
- database bootstrap path
- whether the default replay data agent stays enabled or the user is opting out with `--replay-data-agent off`

Generate the smallest C# file-based harness in durable test-support source, defaulting to `tests/Sqloom/<app>/<profile>/Harness.cs`. Use one committed harness per app/startup profile, not one per endpoint. The file references the public `Sqloom.Testing` package and target app project, starts the app, exposes exactly one `ISqloomApplication`, and locates the app-owned OpenAPI document. Keep endpoint selection in the Sqloom command through `--target "METHOD /path/template"`. Preserve the harness between runs and update it in place only when startup, hosting, auth, tenant, database bootstrap, OpenAPI source, or app-owned replay policy changes.

Sqloom always builds `.cs` harness targets and requires .NET SDK 10 or later through the selected `--dotnet-command`. Do not pass `--no-build` for a file-based harness. Keep app-specific setup in the harness; do not add Sqloom runtime features for one app's setup. Keep `ReplayProfile` minimal: do not add personas or overlays solely to supply path, query, header, or body values when the default replay data agent can prepare them. Use overlays for app-owned deterministic values, non-GET opt-in, and skip rules.
