# Repository Guidelines

## Canonical Docs

Use one source of truth per topic:

- [README.md](README.md): user-facing commands, quick-start paths, and operational workflows.
- [docs/architecture/overview.md](docs/architecture/overview.md): current repo layout, pipeline shape, and project ownership.
- [docs/architecture/dependencies.md](docs/architecture/dependencies.md): project graph and boundary rules.
- [docs/architecture/dotnet-architecture-guidelines.md](docs/architecture/dotnet-architecture-guidelines.md): deeper C# and .NET design standards.

Keep this file focused on agent-specific working rules. Do not restate the full architecture docs here.

## Agent Workflow

The user explicitly authorizes Codex to spawn and coordinate sub-agents for non-trivial work in this repository. Use sub-agents when the correct files are not already known, when multiple projects may need coordinated changes, or when a change can affect CLI contracts, artifact formats, package output, database or test-harness behavior, or build and test workflows.

Do not use Repository Synapse in this repository. Do not run `synapse ensure`, `synapse recall`, `synapse tests`, or any other command that creates `.synapse/` repo-local cache files. Use Atlas, scout agents, direct file/test inspection, and build/test output instead.

### Repository Atlas Reading Policy

- Load [.codex/atlas/repo-map.md](.codex/atlas/repo-map.md) into the active agent context before broad source reading. Treat it as required structural context for this repository, not optional reference material.
- When [.codex/atlas/repo-map.md](.codex/atlas/repo-map.md) contains a runtime, architecture, artifact, or test spine for the task domain, convert that spine into the first read order before broad literal search or scout discovery.
- The main agent owns Atlas routing. With [.codex/atlas/repo-map.md](.codex/atlas/repo-map.md) in context, identify the task domain, choose the first read order, and decide whether a specialist Atlas mapper should run. Do not add or use a separate Atlas router role.
- Do not use `scout` for Atlas domain routing. Use `scout` only after the main agent has bounded a domain, path prefix, diff scope, symbol area, artifact area, or mapper handoff.
- When the current environment exposes Atlas sub-agents, the read-only Atlas mapper roles are approved by default for Sqloom repo work. The main agent may dispatch them without an explicit user prompt when the task benefits from specialist or parallel mapping.
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

[README.md](README.md) is the canonical command reference.

- Run .NET commands from the repo root.
- Use the narrowest relevant restore, build, or test command first.
- After CLI surface changes that affect `sqloom-local`, redeploy the local wrapper before trusting manual runs.
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

Do not commit secrets. Pass `OPENAI_API_KEY` through the environment. Prefer `localhost` for sample SQL Server connection strings unless the task explicitly targets another host. Do not hand-edit generated snapshots, replay outputs, or SQL proposal artifacts unless the task is specifically about those generated files.
