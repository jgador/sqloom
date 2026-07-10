# Sqloom Agent Docs

This folder is for agent-maintenance guidance that should be discoverable during repository work.

## Routing Sources

- [AGENTS.md](../../AGENTS.md) is the default execution policy for coding agents in this repository.
- [.codex/atlas/repo-map.md](../../.codex/atlas/repo-map.md) is the durable Atlas routing map for architecture, source-to-test routing, artifact routing, and first-read-order decisions.
- [.codex/agents/](../../.codex/agents/) contains `.codex/agents/*.toml` read-only sub-agent role prompts for advisor routing, Atlas specialists, and bounded scout discovery.

Keep user-facing product commands, quick starts, and workflows in [README.md](../../README.md) or [docs/command-reference.md](../command-reference.md). Keep architecture ownership and project-boundary details in [docs/architecture/](../architecture/).

## Maintenance Rules

- Update [.codex/atlas/repo-map.md](../../.codex/atlas/repo-map.md) when durable ownership, runtime flow, artifact routing, or source-to-test routing changes.
- Update [AGENTS.md](../../AGENTS.md) and the relevant [.codex/agents/](../../.codex/agents/) TOML prompts in the same change when Atlas workflow changes.
- Do not duplicate the full architecture docs here; link agents to the canonical source instead.
- Sqloom command metadata is authored in `src/Sqloom.Host/CommandCatalog.cs`. Regenerate the Sqloom skill command reference with `dotnet run --file .\tools\Sqloom.CommandDocs.cs -- --write` and verify it with the corresponding `--check` command.
