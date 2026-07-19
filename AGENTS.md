# Repository Guidelines

## Canonical Docs

Use one source of truth per topic:

- [README.md](README.md): user-facing commands, quick-start paths, and operational workflows.
- [docs/architecture/overview.md](docs/architecture/overview.md): current repo layout, pipeline shape, and project ownership.
- [docs/architecture/dependencies.md](docs/architecture/dependencies.md): project graph and boundary rules.
- [docs/architecture/dotnet-architecture-guidelines.md](docs/architecture/dotnet-architecture-guidelines.md): deeper C# and .NET design standards.

Keep this file focused on agent-specific working rules. Do not restate the full architecture docs here.

## Agent Workflow

The user explicitly authorizes Codex to spawn and coordinate repo-defined sub-agents for non-trivial work in this repository. Treat this section as standing explicit authorization and instruction to use sub-agents for Sqloom repository work.

Use only sub-agents defined in this repository under [.codex/agents/](.codex/agents/) unless the user explicitly asks otherwise. Do not use generic or built-in sub-agents such as `default`, `explorer`, `reviewer`, or `worker` unless this repository defines and lists them.

Allowed repo-defined sub-agents:
- `advisor`
- `atlas-csharp-mapper`
- `atlas-doc-mapper`
- `atlas-test-mapper`
- `scout`

For every non-trivial repository task, use at least one non-scout repo-defined sub-agent before finalizing the answer or implementation. Non-trivial tasks include code, tests, docs, configuration, build behavior, CLI behavior, artifacts, architecture, review, debugging, tracing, and file discovery.

Preferred qualifying sub-agents:
- Use `advisor` for overall request analysis and sub-agent routing recommendations.
- Use `atlas-csharp-mapper` for C#/.NET source, project, symbol, or semantic mapping.
- Use `atlas-doc-mapper` for docs, config, build scripts, packaging metadata, CI-adjacent files, prompts, or agent-policy surfaces.
- Use `atlas-test-mapper` for nearest tests, focused validation commands, and coverage-gap mapping.
- Use `scout` only for bounded literal file discovery inside an already selected scope.

Minimum rule:
- At least one non-scout repo-defined sub-agent must be used for every non-trivial repository task.
- `advisor` satisfies the minimum rule when the main need is request analysis, task classification, or deciding which specialist should run next.
- `scout` does not satisfy the minimum rule unless the user's task is purely file/path discovery.
- If the correct specialist is already obvious, skip `advisor` and use the relevant Atlas mapper directly.
- If tests or validation matter, use `atlas-test-mapper` after the source or domain scope is known.

Skip sub-agents only when the environment does not expose repo-defined sub-agents, the user explicitly says not to use sub-agents, the task is a trivial direct answer or one-line command, or the task is a single-file read with no repo-impact decision. When sub-agents are skipped, briefly state why.

Do not use Repository Synapse in this repository. Do not run `synapse ensure`, `synapse recall`, `synapse tests`, or any other command that creates `.synapse/` repo-local cache files. Use Atlas, scout agents, direct file/test inspection, and build/test output instead.

### Category-Specific Scan Paths

Implement repository search as category selection before literal search. The goal is to behave like an expert scan path, not a repo-wide text crawler.

1. Classify the request by semantic intent before searching for names. Use behavior words, runtime stage, artifact type, public surface, configuration surface, or validation need as the primary signal. Treat explicit filenames, directories, and symbols as evidence, not as the whole category.
2. Use [.codex/atlas/repo-map.md](.codex/atlas/repo-map.md) as the resident pre-scan. Select the most likely category from its domains and category scan paths, name the initial scope, name what to ignore first, and choose the first structural signals to inspect.
3. Follow a two-stage scan path:
   - Gaze control: the main agent, or `advisor` when sub-agents are enabled and useful, selects the semantic category, first read order, exclusions, and specialist handoffs.
   - Deep processing: the main agent or selected Atlas mapper reads only the bounded files, symbols, line ranges, tests, or artifacts needed to answer the question.
4. Treat the category hypothesis as an anticipatory retrieval cue. Choose it before deep search, use it to constrain what gets read, then refine or replace it as direct evidence returns.
5. Return situated context, not isolated definitions, when the task is about behavior. A situated answer should identify the owning module, relevant symbol or contract, nearby callers or command entry points, nearest tests, configuration or artifact dependencies, and the validation command or artifact to inspect next.
6. Use dynamic switching. Start with a fast lexical or semantic candidate pass inside the selected category, then switch to grounded inspection when uncertainty remains, behavior matters, or the change is risky. Grounded inspection means RoslynKit definitions/references, project references, runtime flow, artifact readers/writers, test output, generated artifacts, or focused command execution.
7. Use a simulation scratchpad before deeper search when behavior is unclear. State the expected behavior, data flow, artifact delta, or test that would prove the answer, then search for structures that could produce or violate that expectation.
8. Prefer distributed structural signals over single landmarks: command catalogs, project references, runtime flow, RoslynKit definitions/references, public contracts, artifact writers/readers, and nearest tests. Use `rg` after the category is bounded, or when the request is truly literal.
9. Avoid overfitting to current file names. If a path or filename cue fails, continue from the semantic feature and project graph instead of expanding immediately to a repo-wide search.
10. Guard against recency contamination. Recently edited files, prior-task context, or recently read paths should not dominate routing unless they match the current semantic category; treat them as possible distractors or exclusions.
11. Cache only durable learning. When repeated work proves a stable category route, ownership fact, artifact route, or test route, update [.codex/atlas/repo-map.md](.codex/atlas/repo-map.md) and refresh `Last verified`. Do not cache source snippets, generated outputs, exhaustive file inventories, or transient search results in Atlas.

### Repository Atlas Reading Policy

- Load [.codex/atlas/repo-map.md](.codex/atlas/repo-map.md) into the active agent context before broad source reading. Treat it as required structural context for this repository, not optional reference material.
- Before broad literal search, assign a semantic category from [.codex/atlas/repo-map.md](.codex/atlas/repo-map.md), choose a bounded scan path, and state the category hypothesis when the task is non-trivial.
- Update the category hypothesis as evidence accumulates. If the first matched files do not support the intended behavior, reclassify before widening the search.
- When [.codex/atlas/repo-map.md](.codex/atlas/repo-map.md) contains a runtime, architecture, artifact, or test spine for the task domain, convert that spine into the first read order before broad literal search or scout discovery.
- For behavior, debugging, refactoring, or architecture questions, build a situated bundle before finalizing: owner, symbol or contract, callers, tests, configuration or artifacts, and validation evidence.
- Escalate from lexical search to grounded inspection when the first pass finds multiple plausible categories, no clear owner, stale names, behavior/config interaction, public surface impact, or failing tests/artifacts.
- The main agent owns Atlas routing. With [.codex/atlas/repo-map.md](.codex/atlas/repo-map.md) in context, identify the task domain, choose the first read order, and decide whether a specialist Atlas mapper should run. Use `advisor` when a non-trivial task needs request classification or sub-agent routing recommendations before that decision. Do not add or use a separate Atlas router role.
- Do not use `scout` for Atlas domain routing. Use `scout` only after the main agent has bounded a domain, path prefix, diff scope, symbol area, artifact area, or mapper handoff.
- When the current environment exposes repo-defined sub-agents, the read-only repo roles are approved by default for Sqloom repo work. The main agent may dispatch them without an explicit user prompt when the task benefits from specialist or parallel mapping.
- Route overall request analysis, task classification, and sub-agent routing recommendations to `advisor` when the task is non-trivial and the correct specialist is not already obvious.
- Route C# source, project files, solution files, MSBuild files, C# symbols, and semantic inspection to `atlas-csharp-mapper` before broad C# source reads when the task benefits from delegation. When Atlas sub-agents are unavailable or the task is too small to delegate, use [.agents/skills/roslynkit/SKILL.md](.agents/skills/roslynkit/SKILL.md) and direct inspection as appropriate.
- Route docs, config, build scripts, packaging metadata, CI-adjacent files, and agent-prompt or Atlas-policy surfaces to `atlas-doc-mapper` when Atlas sub-agents are available and the task benefits from delegation.
- Route nearest-test discovery, focused validation commands, and obvious coverage-gap mapping to `atlas-test-mapper` after the source or domain scope is known.
- If files remain unclear after a domain or scope is bounded, use `scout` for bounded literal discovery; do not call `scout` before that boundary exists.
- Read tests before implementation when available.
- Prefer symbol and line-range reads over full-file reads.
- Stop after five source files and state a hypothesis before reading more.
- Atlas does not store file, project, test, symbol, reference, artifact, or source-slice inventories. Use `git ls-files`, `rg`, RoslynKit live queries, build/test output, generated artifacts, or direct file inspection for current facts.
- Keep [.codex/atlas/repo-map.md](.codex/atlas/repo-map.md) focused on durable architecture, source-to-test routing, feature ownership facts, artifact routing, and navigation rules.
- When durable Atlas facts change, update [.codex/atlas/repo-map.md](.codex/atlas/repo-map.md) and refresh its `Last verified` date before finishing.
- When Atlas workflow changes, update this policy and the relevant [.codex/agents/](.codex/agents/) TOML prompts in the same change.

### Bounded Scout Search

Use `scout`, the repo discovery sub-agent, for bounded literal discovery after Atlas routing has identified an assigned scope and files are still unclear inside that scope. `scout` reduces a known search space; it does not choose the repository domain, architecture spine, or first read order for this repository.

Use `scout` when the current agent environment exposes it and any of these are true inside the bounded scope:
- the task needs file discovery inside the assigned scope
- more than one disjoint path prefix remains inside the assigned scope
- the likely read set inside the assigned scope is more than 3 files

Skip `scout` when one obvious target file is already known, the task is a single-file explanation or edit, or the remaining question is repository-domain selection rather than file discovery.

When using `scout`:
- ground first in [AGENTS.md](AGENTS.md), [README.md](README.md), directly named files, and the Atlas-selected domain or read order when applicable; include [docs/agents/README.md](docs/agents/README.md) for agent workflow or Atlas-policy tasks
- normalize and de-overlap scopes before spawning
- spawn one `scout` sub-agent per disjoint scope
- pass exact path prefixes, exact file lists, symbols, artifact areas, or diff-scoped boundaries
- include `artifacts/sqloom/` in scope when the bounded task investigates generated replay or tuning behavior
- do not pass repo-wide default buckets such as [src/](src/), [tests/](tests/), [scripts/](scripts/), `artifacts/`, repo-root config files, [.agents/](.agents/), or [.codex/](.codex/) until Atlas or the main agent has selected those areas as the bounded task scope

Every scout prompt must include:
- `assigned_scope`
- `search_goal`
- the selected semantic category or category hypothesis when known
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

## Build and Verification

[.agents/skills/sqloom/references/commands.md](.agents/skills/sqloom/references/commands.md) is the generated canonical command reference. Keep [README.md](README.md) as the user-facing quick start and workflow overview.

- Run .NET commands from the repo root.
- Use the narrowest relevant restore, build, or test command first.
- After CLI surface changes that affect `sqloom-local`, redeploy the local wrapper before trusting manual runs.
- After command metadata changes, regenerate `.agents/skills/sqloom/references/commands.md` with `dotnet run --file .\tools\Sqloom.CommandDocs.cs -- --write` and run the same file app with `--check`.
- For replay, correlate, advise, and tune issues, inspect `artifacts/sqloom/` before guessing from code alone.
- If deeper verification requires Docker, SQL Server assets, or OpenAI credentials, say exactly what you ran and what you skipped.

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

## C# Working Rules

Follow the existing style in touched files. Keep 4-space indentation, file-scoped namespaces when already used, explicit `using` directives, and nullable-aware code. Avoid broad refactors unless the task requires them.

- Preserve the current surviving `Sqloom.*` project names and follow the repo-specific ownership and boundary docs instead of inventing a new project split.
- Keep comments sparse. Prefer clear names first and add only short comments for non-obvious behavior.
- Keep every publicly visible C# API in production projects documented with a concise XML `<summary>` that gives RoslynKit useful semantic context. Do not add routine `<param>` comments; use `<inheritdoc />` for straightforward implementations and overrides. CS1591 is enforced for production code and intentionally suppressed under `tests/` for tests, samples, and generated fixtures.
- When doing C# comment or XML-doc work, review all non-trivial symbols, not just classes and methods: records, record parameters and properties, enum members, fields, option/spec carrier types, nullable bindings, and nested helper types. Explain the behavior, contract, precedence, or safety boundary that is not obvious from the identifier; do not add boilerplate summaries such as `Gets the X` or comments that only restate the member name.
- For public-facing ASP.NET Core contracts, prefer `Request` and `Response` suffixes over `Dto`.
- Use structured logging, typed options, and pass `CancellationToken` last and downstream.
- If a public API, public CLI or package surface, public contract, configuration surface, or documented workflow changes, update [README.md](README.md) or the relevant docs in the same change.

## Commit & Pull Request Guidelines

Recent history mixes short imperative subjects with scoped prefixes such as `feat:` and `fix:`. Match the surrounding history and keep subjects imperative.

- Keep commits focused. Separate behavior changes, generated output refreshes, and repo surgery when practical.
- Before committing, confirm the diff is real and relevant. Do not commit CRLF-only or stat-only churn just because `git status` mentions a file.
- Pull requests should explain which stage or module changed, note config or artifact impacts, and list the exact validation commands that were run.
- For this CLI-first repo, artifact paths and console snippets are usually more useful than screenshots.

## Security & Configuration Tips

Do not commit secrets. Do not introduce environment-variable reads or writes in runtime code unless the variable is explicitly whitelisted here. The whitelist is `OPENAI_API_KEY`, `ASPNETCORE_ENVIRONMENT`, and `PATH` (`Path` on Windows); use normal configuration and explicit parameters for all other values. Prefer `localhost` for sample SQL Server connection strings unless the task explicitly targets another host. Do not hand-edit generated snapshots, replay outputs, or SQL proposal artifacts unless the task is specifically about those generated files.

Use [.agents/skills/security-audit/SKILL.md](.agents/skills/security-audit/SKILL.md) for full repo, full-history, all-blob, dependency, and code-level security audits. Use [.agents/skills/security-audit-fast/SKILL.md](.agents/skills/security-audit-fast/SKILL.md) for staged, named-path, working-tree, or base-ref diff leak checks that only need to determine whether the selected change set introduced secrets or credentials.

### Checked-In Password Exceptions

Do not check in concrete passwords, password-equivalent fixture values, password hashes, or password salts unless an exception below explicitly allows either an exact value at a documented location or a named generated file with explicitly scoped allowed password-bearing fields and purpose. Password-related identifiers, schema and property names, detection patterns, and non-executable documentation placeholders such as `<strong-password>` are not values. This list is exhaustive: no unlisted value or generated-file field exception is allowed, including values described as local, test-only, inert, sample, generated, hashed, or salted. Update this single list deliberately whenever an exception is added, changed, or removed; do not duplicate the exception list elsewhere.

No checked-in password exceptions are currently allowed.
