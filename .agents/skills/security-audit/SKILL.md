---
name: security-audit
description: Run a read-only security audit of the current repository, combining a code-level vulnerability review with secret scanning across the working tree, the full git history, and every blob in the git object database. Use when the user asks for a security audit, a secret scan, or a check for committed API keys, credentials, or similar sensitive data.
---

# Security Audit

Run a read-only deep security analysis of the current repository. Do not modify any files and do not run state-changing git commands; read-only git commands such as `git log`, `git ls-files`, and `git cat-file` are allowed. Do not print raw secret values in the final report; report only safe signatures, prefixes, file paths, line numbers, commit IDs, and remediation steps.

Do not use sub-agents for this audit. Keep the code review, secret triage, and final risk judgment in the main agent. Independent read-only shell/search commands may still run in parallel when they do not depend on each other, but thoroughness takes priority over parallelism.

The audit has two main-agent tracks:

1. A code-level vulnerability review using the checklist below.
2. A three-layer secret scan: working tree, all commit diffs, and every blob in the git object database (which covers amended or rebased commits, stashes, and other unreachable objects).

## Workflow

1. Size and scope the repository first with `git status --short --branch`, `git rev-list --all --count`, `git ls-files`, `git ls-files -o --exclude-standard`, and `Get-Command gitleaks,trufflehog -ErrorAction SilentlyContinue`. The pipeline-based history scans below are practical for small histories (roughly under a few thousand commits); for large repositories, prefer a dedicated scanner such as gitleaks or trufflehog for layers 2 and 3.
2. Run the code-level review checklist in the main agent. Use search results to identify security-sensitive code paths, then verify every suspected issue by reading the actual code.
3. Run all three secret-scan layers: working tree, full git history, and every blob in the object database.
4. For .NET repositories with a solution or project file, run package metadata checks such as `dotnet list <solution-or-project> package --vulnerable --include-transitive` and `dotnet list <solution-or-project> package --deprecated --include-transitive`. For other ecosystems, run the nearest read-only package audit command when available.
5. For any hit, locate the introducing commit with `git log --all -S '<string>' --oneline --name-only` before deciding whether history rewriting (git-filter-repo or BFG) is needed. Use the literal string locally for tracing, but keep raw secret material out of the final report.
6. Report findings with file or commit, severity, a one-sentence description, and a concrete exploitation scenario. Explicitly list the areas verified clean, commands or checks run, limitations, and whether git-history cleanup is needed.

## Code-Level Vulnerability Review Checklist

Adapt the directory layout to the repository. Review production source, tools, tests, scripts, project files, package configuration, and CI workflow files for security vulnerabilities. Look specifically for:

1. Command/process injection: any Process.Start, ProcessStartInfo, shell
   invocation where user-controlled arguments are interpolated into a
   command line without proper escaping (UseShellExecute, cmd.exe /c, etc.).
2. Path traversal / arbitrary file write: file path construction from user
   input; whether destination paths are validated, whether ".." in embedded
   resource names or arguments could escape the target root, and overwrite
   behavior.
3. Unsafe deserialization (BinaryFormatter, insecure JSON settings, XML
   with DTD/XXE enabled - XmlReader/XDocument settings).
4. Loading/executing untrusted code: MSBuildWorkspace loading arbitrary
   projects executes MSBuild targets - note if the tool documents/mitigates
   that risk; any Assembly.Load / add-in loading.
5. Insecure network calls: http:// URLs, disabled certificate validation
   (ServerCertificateCustomValidationCallback returning true), credentials
   in URLs.
6. Secrets in source: hardcoded API keys, tokens, passwords, connection
   strings in any file including test fixtures.
7. Dependency risks: check PackageReference entries for known-vulnerable or
   unusual packages; note any pinned prerelease/unofficial feeds in
   nuget.config; check for NuGet package source mapping issues.
8. CI/CD risks in .github/workflows: pull_request_target misuse, script
   injection via ${{ github.event... }} interpolation into run: steps,
   overly broad permissions, secrets exposure.
9. Symlink/zip-slip issues in any packaging or extraction code.
10. Anything else notable (e.g., predictable temp files, world-writable
    output, logging of sensitive data).

For each finding report: file path (repo-relative), line number, severity
(critical/high/medium/low/info), a one-sentence description of the defect,
and a concrete exploitation scenario. If an area is clean, say so briefly.
Also list the files actually examined. Be rigorous - verify each finding by
reading the actual code, not just pattern matches.

## Secret-Scan Commands (PowerShell, read-only)

All commands assume the current directory is the repository root. Define the high-confidence token pattern once per session (AWS, GitHub, Anthropic, OpenAI, Slack, Google, GitLab, npm/NuGet, Azure, private key blocks, JWTs):

```powershell
$tokenPatterns = 'AKIA[0-9A-Z]{16}|ghp_[A-Za-z0-9]{36}|gho_[A-Za-z0-9]{36}|github_pat_[A-Za-z0-9_]{22,}|sk-ant-[A-Za-z0-9-]{20,}|sk-proj-[A-Za-z0-9_-]{20,}|sk-[A-Za-z0-9_-]{20,}|xox[baprs]-[0-9A-Za-z-]{10,}|AIza[0-9A-Za-z_-]{35}|BEGIN [A-Z ]*PRIVATE KEY|_authToken|npm_[A-Za-z0-9]{36}|glpat-[A-Za-z0-9_-]{20}|oy2[a-z0-9]{40,}|AccountKey=[A-Za-z0-9+/=]{20,}|SharedAccessSignature|eyJhbGciOi'
```

### Layer 1: working tree

Scan tracked files and non-ignored untracked files for `$tokenPatterns` with the session's native search tool (for example `Grep`, `rg`, or `git grep -I -E`), plus a looser case-insensitive pass for `(password|passwd|secret|api[_-]?key|apikey|token|credential|connectionstring|pwd)\s*[:=]`. Expect and dismiss benign hits such as `CancellationToken`, syntax tokens, and `Environment.GetEnvironmentVariable("SOME_API_KEY")` (reads from the environment; no value committed). Keep ignored or local-only files separate from committed evidence: enumerate them with `git status --short --ignored`, and inspect obvious sensitive filenames such as `.env`, `secrets.json`, `.vscode/launch.json`, or local credential files only when needed.

### Layer 2: full git history

Sensitive filenames ever added in any commit:

```powershell
git log --all --diff-filter=A --name-only --pretty=format:'COMMIT %h' |
  Select-String -Pattern '\.(env|pem|key|pfx|p12|jks|keystore|ppk)$|id_rsa|id_ed25519|credentials|secrets?\.|\.npmrc|nuget\.config|appsettings|\.netrc|\.pypirc|authinfo'
```

Token patterns across all history diffs:

```powershell
git log --all -p --no-color | Select-String -Pattern $tokenPatterns | Select-Object -First 50
```

Loose credential keywords and connection strings on added lines only, with a false-positive filter:

```powershell
git log --all -p --no-color |
  Select-String -Pattern '^\+.*((api[_-]?key|secret|passw(or)?d|bearer |authorization:)[^a-z0-9]{0,3}[A-Za-z0-9+/_-]{8,}|Server=.*;.*Password=|Data Source=.*Password=|mongodb(\+srv)?://[^ ]*:[^ ]*@|postgres(ql)?://[^ ]*:[^ ]*@|mysql://[^ ]*:[^ ]*@|redis://[^ ]*:[^ ]*@|amqps?://[^ ]*:[^ ]*@|https?://[^/ ]*:[^/@ ]*@)' |
  Where-Object { $_ -notmatch 'cancellationtoken|findtoken|placeholder|example|<key>|your[-_ ]?(api)?key' } |
  Select-Object -First 40
```

Inline secret values in config-file history:

```powershell
git log --all -p --no-color -- '*.json' '*.config' '*.props' '*.targets' '*.yml' '*.yaml' '*.xml' |
  Select-String -Pattern '^\+.*"(value|token|key|secret|password)"\s*:\s*"[^"]{8,}"' |
  Select-Object -First 20
```

### Layer 3: every blob in the object database

Covers unreachable objects from amended or rebased commits and stashes; this is the layer that proves nothing secret-shaped survives even in rewritten history.

Optionally summarize unreachable objects first with `git fsck --no-reflogs --unreachable --no-progress`. If the all-object scan below runs successfully, those unreachable blobs are covered; the `fsck` count is useful for the report.

```powershell
$blobs = git cat-file --batch-all-objects --batch-check='%(objecttype) %(objectname)' |
  Where-Object { $_ -like 'blob *' } |
  ForEach-Object { ($_ -split ' ')[1] }
"Scanning $($blobs.Count) blobs"
foreach ($b in $blobs) {
  $content = git cat-file -p $b 2>$null | Out-String
  if ($content -match $tokenPatterns) { "HIT: $b" }
}
```

## Report Contract

Structure the final report as:

- TLDR line: whether secrets were found and whether the code has exploitable vulnerabilities.
- Secrets scan: result per layer (working tree, history diffs and filenames, all blobs), with benign hits explained.
- Code review: findings ranked by severity with file, line, description, and exploitation scenario; then areas verified clean.
- Dependency audit: package vulnerability/deprecation or ecosystem audit result when applicable.
- Whether git-history cleanup (git-filter-repo or BFG) is needed.
- Commands/checks run and explicit limitations, including whether dedicated scanners were unavailable and native git/search scans were used instead.
- The single highest-priority recommended action, if any.
