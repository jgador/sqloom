# Changelog

## Unreleased

## 0.5.0

- Added the Sqloom VS Code preview with an Activity Bar launcher, Tune dashboard webview, and CLI-backed `init` and `tune` commands.
- Added dashboard setup and readiness checks for the Sqloom CLI path, harness detection, OpenAI model selection, and replay data agent mode.
- Added dashboard endpoint discovery through `sqloom endpoints` and required explicit endpoint selection before Tune runs.
- Added streaming tune workflow progress, recent runs, and artifact browsing in the dashboard.
- Prefilled the local SQL Server connection for the detected Sqloom test-app harness without persisting it.
- Added file-based Sqloom harness support and Roslyn endpoint catalog discovery in the CLI.
- Scoped endpoint catalogs to workflow artifacts and streamlined local sample harness tuning.

## 0.0.1

- Initial Marketplace preview.
- Added the Sqloom Activity Bar logo, compact webview launcher, and Sqloom Tune webview dashboard preview.
- Added CLI-backed commands for `init`, `tune`, and CLI path selection.
