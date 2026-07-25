# Sqloom Agent Docs

This folder is for agent-maintenance guidance that should be discoverable during repository work.

## Routing Sources

- [AGENTS.md](../../AGENTS.md) is the default execution policy for coding agents in this repository.
- [.codex/atlas/repo-map.md](../../.codex/atlas/repo-map.md) is the durable Atlas routing map for architecture, source-to-test routing, artifact routing, and first-read-order decisions.
- [.codex/agents/](../../.codex/agents/) contains `.codex/agents/*.toml` read-only sub-agent role prompts for advisor routing, Atlas specialists, and bounded scout discovery.
- [.agents/skills/dotnet-refactor/SKILL.md](../../.agents/skills/dotnet-refactor/SKILL.md) is the .NET architecture debloat audit, refactor planning, and safe simplification path.
- [.agents/skills/security-audit/SKILL.md](../../.agents/skills/security-audit/SKILL.md) is the full audit path; [.agents/skills/security-audit-fast/SKILL.md](../../.agents/skills/security-audit-fast/SKILL.md) is the staged, working-tree, named-path, and base-ref diff leak-check path.

Keep user-facing quick starts and workflow overview prose in [README.md](../../README.md). Keep exact Sqloom command syntax, options, defaults, allowed values, and command notes in the generated [.agents/skills/sqloom/references/commands.md](../../.agents/skills/sqloom/references/commands.md). Keep architecture ownership and project-boundary details in [docs/architecture/](../architecture/).

## Category-Specific Search

Sqloom agents implement search as category selection before keyword search. The durable category index lives in [.codex/atlas/repo-map.md](../../.codex/atlas/repo-map.md); [AGENTS.md](../../AGENTS.md) defines the required scan-path behavior.

Use Atlas to classify the request by semantic intent first: command behavior, endpoint replay, Query Store observe, correlation, advice, tune workflow, artifacts, harness resolution, packaging, VS Code extension, or agent policy. Then inspect the category's first structural signals and nearest tests before using broader literal search.

The category acts as an anticipatory retrieval cue: select it before deep search, let it constrain the first read order, then refine it as evidence returns. Do not let recently edited files or prior-task context pull the search into a stale category unless the current request matches that category.

The operational retrieval model is functional rather than neuroanatomical:

1. Task frame: record the goal, symptoms or observations, constraints and exclusions, and proof signal.
2. Live evidence substrate: combine durable Atlas routes with current project, symbol, runtime, artifact, configuration, and test relationships. Atlas is a routing index, not a stored repository graph.
3. Bounded hypothesis competition: keep one primary category and normally no more than two alternatives when the route is genuinely ambiguous.
4. Admission gate: retain only hypotheses that fit the task frame, identify a plausible structural anchor, and predict distinct observable evidence.
5. Minimal predictive probe: state confirming and disconfirming evidence, then inspect the cheapest bounded slice that can distinguish them.
6. Evidence update: confirm, refine, or reclassify using the probe result; widen only after the admitted hypotheses fail.
7. Durable route promotion: record only stable ownership, boundary, artifact, or test-routing facts supported by repeated work or independent structural evidence.

For behavior questions, return situated context rather than a detached file match. The useful bundle is the owner module, relevant symbols or contracts, callers or entry points, nearest tests, configuration or artifact dependencies, and the validation command or generated artifact that grounds the answer.

Use dynamic switching: a fast lexical or semantic pass can find candidates inside the selected category, but uncertainty, stale names, public-surface impact, or behavior/config interaction should trigger grounded inspection with RoslynKit, project references, runtime flow, tests, command output, or generated artifacts. When the desired behavior is unclear, first sketch the expected data flow, artifact delta, or passing test, then name the evidence that would confirm or weaken the active hypothesis and declare the switch trigger before probing.

Successful or failed search paths become repository memory only when repeated work or independent structural evidence turns them into durable ownership, boundary, artifact, or test-routing facts. Record those in [.codex/atlas/repo-map.md](../../.codex/atlas/repo-map.md) and refresh `Last verified`; do not add transient source inventories, generated outputs, episodic task traces, or search-result caches.

## Maintenance Rules

- Update [.codex/atlas/repo-map.md](../../.codex/atlas/repo-map.md) when durable ownership, runtime flow, artifact routing, or source-to-test routing changes.
- Update [AGENTS.md](../../AGENTS.md) and the relevant [.codex/agents/](../../.codex/agents/) TOML prompts in the same change when Atlas workflow changes.
- Do not duplicate the full architecture docs here; link agents to the canonical source instead.
- Sqloom command metadata is authored in `src/Sqloom.Host/CommandCatalog.cs`. Regenerate the canonical command reference at [.agents/skills/sqloom/references/commands.md](../../.agents/skills/sqloom/references/commands.md) with `dotnet run --file .\tools\Sqloom.CommandDocs.cs -- --write` and verify it with the corresponding `--check` command. Do not duplicate generated command tables in hand-written docs.
