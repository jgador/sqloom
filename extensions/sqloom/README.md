# Sqloom Preview

Sqloom for Visual Studio Code runs the existing `sqloom` CLI from a workspace UI and shows generated tuning artifacts inside the editor.

## Requirements

- Install the `sqloom` .NET tool and make sure the `sqloom` command is on `PATH`.
- Use a workspace that contains a Sqloom harness or can be initialized with `Sqloom: Initialize Sqloom Agent Skill`.
- Set `OPENAI_API_KEY` before launching VS Code, or enter the key when `Sqloom: Run Tune Workflow` prompts for it.

## Features

- Run `sqloom init` for Codex, Claude, Copilot, or all supported agent skill locations.
- Run `sqloom tune` from the Command Palette or the Sqloom Activity Bar.
- Refresh and inspect run artifacts under `artifacts/sqloom`.
- Open generated SQL proposal files from the Sqloom Advice view.

## Settings

- `sqloom.cli.path`: Sqloom CLI executable or absolute path. Defaults to `sqloom`.
- `sqloom.artifactsRoot`: Workspace-relative or absolute artifact root. Defaults to `artifacts/sqloom`.
- `sqloom.openai.model`: OpenAI model passed to `sqloom tune`. Defaults to `gpt-5.4-mini`.
- `sqloom.replayDataAgent`: Replay data agent mode passed to `sqloom tune`. Defaults to `required`.

## Preview Notes

This preview keeps the CLI as the complete workflow engine. The extension does not store API keys or connection strings in VS Code settings; prompted values are passed only to the current CLI run.
