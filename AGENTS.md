# Repository Guidelines

## Canonical Docs

Use one source of truth per topic:

- `README.md`: user-facing commands, quick-start paths, and operational workflows.
- `docs/architecture/overview.md`: current repo layout, pipeline shape, and project ownership.
- `docs/architecture/dependencies.md`: project graph and boundary rules.
- `docs/architecture/dotnet-architecture-guidelines.md`: deeper C# and .NET design standards.

Keep this file focused on agent-specific working rules. Do not restate the full architecture docs here.

## Agent Workflow

The user explicitly authorizes Codex to spawn and coordinate sub-agents for non-trivial work in this repository. Use sub-agents when the correct files are not already known, when multiple projects may need coordinated changes, or when a change can affect CLI contracts, artifact formats, package output, database or test-harness behavior, or build and test workflows.

### Scout-First Repo Search

Use `scout`, the repo discovery sub-agent, for repo discovery when any of these are true:
- the user asks to find, trace, investigate, or discover which files matter
- the user did not name an exact target file
- more than one top-level area may be relevant
- the likely read set is more than 3 files

Skip `scout` only when the task is clearly a single-file explanation or edit and one obvious target file is already known.

When using `scout`:
- ground first in `AGENTS.md`, `README.md`, and any directly named files
- use Repository Synapse first when available, then pass the strongest recall hints into the scout prompt
- normalize and de-overlap scopes before spawning
- spawn one `scout` sub-agent per disjoint scope
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

## Build and Verification

`README.md` is the canonical command reference.

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

- Preserve the current `Sqloom.*` project names and follow the repo-specific ownership and boundary docs instead of inventing a new project split.
- Keep comments sparse. Prefer clear names first and add only short comments for non-obvious behavior.
- For public-facing ASP.NET Core contracts, prefer `Request` and `Response` suffixes over `Dto`.
- Use structured logging, typed options, and pass `CancellationToken` last and downstream.
- If a public API, public CLI or package surface, public contract, configuration surface, or documented workflow changes, update `README.md` or the relevant docs in the same change.

## Commit & Pull Request Guidelines

Recent history mixes short imperative subjects with scoped prefixes such as `feat:` and `fix:`. Match the surrounding history and keep subjects imperative.

- Keep commits focused. Separate behavior changes, generated output refreshes, and repo surgery when practical.
- Before committing, confirm the diff is real and relevant. Do not commit CRLF-only or stat-only churn just because `git status` mentions a file.
- Pull requests should explain which stage or module changed, note config or artifact impacts, and list the exact validation commands that were run.
- For this CLI-first repo, artifact paths and console snippets are usually more useful than screenshots.

## Security & Configuration Tips

Do not commit secrets. Pass `OPENAI_API_KEY` through the environment. Prefer `localhost` for sample SQL Server connection strings unless the task explicitly targets another host. Do not hand-edit generated snapshots, replay outputs, or SQL proposal artifacts unless the task is specifically about those generated files.
