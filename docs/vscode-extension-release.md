# VS Code Extension Preview Release

This runbook is for packaging and publishing the Sqloom VS Code extension preview.

The extension is a UI shell over the `sqloom` CLI. Keep the CLI as the source of truth for replay, observe, correlate, advise, and tune behavior.

## What gets published

- Marketplace publisher: `jessegador`
- Extension identifier: `jessegador.sqloom`
- Extension package root: [extensions/sqloom](../extensions/sqloom)
- Preview flag: `preview: true` in the extension manifest

## 1. Validate the CLI surface

Run from the repository root:

```powershell
dotnet run --file .\tools\Sqloom.CommandDocs.cs -- --check
dotnet restore .\Sqloom.slnx
dotnet build .\Sqloom.slnx --tl:off --nologo "-clp:ErrorsOnly;NoSummary"
dotnet test --solution .\Sqloom.UnitTests.slnf
```

Run integration tests when the local SQL Server and sample prerequisites are available:

```powershell
dotnet test --solution .\Sqloom.IntegrationTests.slnf
```

## 2. Validate the extension

Install Node dependencies once:

```powershell
npm install
```

Build, type-check, and package the extension:

```powershell
npm run lint -- --target sqloom
npm run build -- --target sqloom
npm run package -- --target sqloom
```

The package command writes a `.vsix` under [extensions/sqloom](../extensions/sqloom). Install it locally with VS Code before publishing:

```powershell
code --install-extension .\extensions\sqloom\sqloom-0.0.1.vsix
```

## 3. Marketplace publisher setup

Create or confirm the Visual Studio Marketplace publisher ID `jessegador`. Use Jesse Gador as the display identity where the Marketplace account UI supports it, but keep the manifest publisher ID lowercase:

```json
"publisher": "jessegador"
```

Do not store Marketplace tokens, OpenAI keys, or SQL connection strings in the repository.

## 4. Publish the preview

From the extension package root, publish the preview after local VSIX validation:

```powershell
Set-Location .\extensions\sqloom
npx vsce login jessegador
npx vsce publish --pre-release --no-dependencies
```

For a manual upload flow, use the `.vsix` produced by `npm run package -- --target sqloom` and upload it through the Marketplace publisher portal.

## 5. Verify after publish

Install the Marketplace preview in a clean VS Code profile and verify:

- The Sqloom Activity Bar appears.
- `Sqloom: Select Sqloom CLI Path` can point at either `sqloom` on `PATH` or an absolute local build.
- `Sqloom: Initialize Sqloom Agent Skill` runs `sqloom init`.
- `Sqloom: Run Tune Workflow` launches the CLI and writes output to the Sqloom output channel.
- The Runs and Advice views refresh from `artifacts/sqloom`.
