# Repository Guidelines

## Project Structure & Module Organization

`src/` contains the production libraries: `Sqloom.Core`, `Sqloom.QueryStore`, `Sqloom.AzureSql`, `Sqloom.AspNetCore`, and the `sqloom` CLI in `Sqloom.Host`. `tests/` contains the unit and integration lanes plus the sample app and companion harness: `Sqloom.UnitTests`, `Sqloom.IntegrationTests`, `Sqloom.TestApp`, and `Sqloom.TestApp.IntegrationTests`. Use `scripts/tools/` for local tool deployment and package preparation. Treat `artifacts/` as generated output for builds, packages, replay runs, and tune runs. If a bug report mentions replay, correlate, advise, or tune behavior, inspect the generated files under `artifacts/sqloom/` before guessing from code alone.

## Agent Workflow

Use sub-agents for non-trivial work in this repository. Treat work as non-trivial when the correct files are not already known, when multiple projects may need coordinated changes, or when a change can affect CLI contracts, artifact formats, package output, database/test harness behavior, or build and test workflows.

### Scout-First Repo Search

Use `scout` for repo discovery when any of these are true:
- the user asks to find, trace, investigate, or discover which files matter
- the user did not name an exact target file
- more than one top-level area may be relevant
- the likely read set is more than 3 files

Skip `scout` only when the task is clearly a single-file explanation or edit and one obvious target file is already known.

When using `scout`:
- ground first in `AGENTS.md`, `README.md`, and any directly named files
- use Repository Synapse first when available, then pass the strongest recall hints into the scout prompt
- normalize and de-overlap scopes before spawning
- spawn one `scout` per disjoint scope
- prefer these default scopes when the area is unclear: `src/`, `tests/`, `scripts/`, `artifacts/`, and repo-root config or docs files
- include `artifacts/sqloom/` in scope when investigating generated replay or tuning behavior

Every scout prompt must include:
- `assigned_scope`
- `search_goal`
- known keywords, symbols, routes, config keys, or filenames when available
- the required response format

Every scout must return:
- `assigned_scope`
- `files_examined`
- `likely_relevant_files`
- `evidence`
- `handoff_paths`
- `short_summary`
- `confidence`

After scouts return:
- inspect `likely_relevant_files` locally first
- use `handoff_paths` to decide whether another scout batch is needed
- escalate to deeper tracing only after the likely files have been read

## Build, Test, and Development Commands

Run .NET commands from the repo root. This repo targets .NET 10 and uses Microsoft Testing Platform.

```powershell
dotnet restore .\Sqloom.sln
dotnet build .\Sqloom.sln --tl:off --nologo "-clp:ErrorsOnly;NoSummary"
dotnet test --solution .\Sqloom.UnitTests.slnf
dotnet test --solution .\Sqloom.IntegrationTests.slnf
pwsh .\scripts\tools\deploy-sqloom-local.ps1 -SkipSmoke
pwsh .\scripts\tools\prepare-sqloom-packages.ps1 -SkipSmoke
dotnet run --project .\src\Sqloom.Host\Sqloom.Host.csproj -- replay .\tests\Sqloom.TestApp\Sqloom.TestApp.csproj --target "GET /api/products/by-category"
```

Use the narrowest relevant target first. After CLI surface changes that affect `sqloom-local`, redeploy the local wrapper before trusting smoke tests or manual runs.

## State-Changing Git Command Safety

Git read commands such as `git status`, `git log`, `git diff`, `git show`, and `git branch --show-current` are allowed for inspection.

Do not run state-changing Git commands unless the user explicitly asks for that exact action in the current task, or unless you ask for permission in chat and receive approval first. State-changing Git commands include `git commit`, `git push`, `git merge`, `git rebase`, `git cherry-pick`, `git checkout -b`, `git switch -c`, `git tag`, `git reset`, `git revert`, `git stash`, branch deletion, and any command that changes refs, the index, or the working tree.

If the user asks for one state-changing Git action, do only that action. Do not infer permission for adjacent actions such as creating a branch, committing, pushing, tagging, merging, or opening a release. Before any state-changing Git action approved through chat rather than explicitly requested, state the exact command, target branch or ref, and whether it is local-only or remote-mutating.

Especially for release work, do not create `release/v*` branches unless the user explicitly asks to create that branch. A request to prepare a release does not imply permission to create, switch to, push, publish, or otherwise manage a release branch.

Before any commit or push:
- verify there is a real diff, not just stat or line-ending noise
- stage only the intended files
- re-check `git status`
- stop cleanly if the tree is already synced

## C# Conventions

Follow the existing style in touched files. Keep 4-space indentation, file-scoped namespaces when already used, explicit `using` directives, and nullable-aware code. Avoid broad refactors unless the task requires them.

- `Directory.Build.props` is authoritative for nullable, analyzers, warnings-as-errors, and related build policy.
- `Directory.Packages.props` is authoritative for package versions. Do not hardcode versions inside individual `.csproj` files unless there is a repo-wide reason.
- Use sparse comments. Keep XML `<summary>` docs minimal and add comments only when behavior is non-obvious or complex.
- Keep names concise and non-redundant. Align file names with their primary types unless the existing structure clearly says otherwise.
- For public API-facing request or response contracts, prefer `Request` and `Response` suffixes over `Dto`.
- Public API-facing request and response properties should declare explicit `JsonPropertyName` values. Preserve existing wire names when refactoring C# property names.
- For async disposables, prefer explicit `await using (...) { }` blocks when disposal behavior needs to stay visible.

### Class Design

- Keep classes narrow. A class should have one primary responsibility and one clear reason to change.
- Keep CLI entrypoints, startup code, and controllers thin. They should validate inputs, compose dependencies, and delegate to focused collaborators.
- Split classes when parsing, orchestration, domain logic, persistence, artifact writing, and output rendering start to mix.
- Prefer small collaborators over one large coordinator.
- If a needed refactor is too large for the current task, do not keep adding responsibilities to a fat class. Note the next extraction in the task summary instead.

## Testing Guidelines

Tests use xUnit v3 on Microsoft Testing Platform, configured through `global.json` and `tests/Directory.Build.props`.

- Keep tests near the affected code and mirror the existing naming style such as `*Tests.cs` and `Method_Scenario_ExpectedBehavior`.
- Add or update tests with behavior changes when practical.
- Run the narrowest relevant command first. If only one test class changed, prefer an MTP-native filtered run before a full lane. Example: `dotnet test --project .\tests\Sqloom.UnitTests\Sqloom.UnitTests.csproj -- --filter-class Sqloom.Host.Tests.TuneArgumentParserTests`
- Use `Sqloom.UnitTests.slnf` for unit work and `Sqloom.IntegrationTests.slnf` for host and process integration work.
- For replay, correlate, advise, and tune issues, verify the emitted artifact chain under `artifacts/sqloom/` instead of relying only on console output.
- If Docker, SQL Server assets, or OpenAI credentials are required for a deeper integration run, say exactly what you ran and what you skipped.

## Commit & Pull Request Guidelines

Recent history mixes short imperative subjects with scoped prefixes such as `feat:` and `fix:`. Match the surrounding history and keep subjects imperative.

- Keep commits focused. Separate behavior changes, generated output refreshes, and repo surgery when practical.
- Before committing, confirm the diff is real and relevant. Do not commit CRLF-only or stat-only churn just because `git status` mentions a file.
- Pull requests should explain which stage or module changed, note config or artifact impacts, and list the exact validation commands that were run.
- For this CLI-first repo, artifact paths and console snippets are usually more useful than screenshots.
- If a public API, public CLI or package surface, public contract, configuration surface, or documented workflow changes, update `README.md` or the relevant docs in the same change so repository documentation does not become stale.

## Security & Configuration Tips

Do not commit secrets. Pass `OPENAI_API_KEY` through the environment. Prefer `localhost` for sample SQL Server connection strings unless the task explicitly targets another host. Do not hand-edit generated snapshots, replay outputs, or SQL proposal artifacts unless the task is specifically about those generated files.
