---
name: security-audit-fast
description: Run a fast read-only security leak check over staged changes, uncommitted diffs, named paths, or a branch diff against a base ref such as origin/master. Use for PR checks, pre-commit checks, and requests to scan newly introduced secrets or credentials; use security-audit instead for full repository, full history, all-blob, dependency, or code-level security audits.
---

# Security Audit Fast

Run a read-only incremental leak check. This skill answers whether the selected change set appears to introduce secrets, private keys, credentials, password values, or sensitive files. It does not prove that the whole repository, reachable history, or unreachable git object database is clean.

Do not modify files or run state-changing git commands. Do not print raw secret values in the final report; report only safe signatures, prefixes, file paths, line numbers, hunk context, and remediation steps.

## Scope Selection

Choose the smallest scope that answers the request and state it in the report.

1. Staged changes: use for pre-commit checks or when the user asks about staged files.
2. Uncommitted working-tree diff: use when the user asks about local unstaged changes.
3. Branch diff against a base ref: use for PR, branch, or "against master" checks.
4. Named paths: combine with one of the above when the user names files or directories.

Start with `git status --short --branch`. If the worktree is dirty and the user did not ask for committed changes only, include staged changes, unstaged changes, and non-ignored untracked files in addition to the branch diff.

For Sqloom branch checks, default the base ref to `origin/master` when it exists, otherwise `master`. Prefer triple-dot comparison for branch work so the scan covers changes since the merge base:

```powershell
git status --short --branch
$baseRef = if (git rev-parse --verify origin/master 2>$null) { 'origin/master' } elseif (git rev-parse --verify master 2>$null) { 'master' } else { $null }
if (-not $baseRef) { throw 'No base ref found. Provide a base ref such as origin/master.' }
git merge-base HEAD $baseRef
git diff --name-status "${baseRef}...HEAD"
git diff --no-color --diff-filter=ACMRT "${baseRef}...HEAD"
```

Use pathspecs when the user asks for a narrower path:

```powershell
git diff --name-status "${baseRef}...HEAD" -- <pathspec>
git diff --no-color --diff-filter=ACMRT "${baseRef}...HEAD" -- <pathspec>
```

Use staged or working-tree commands when those scopes are requested:

```powershell
git diff --cached --name-status
git diff --cached --no-color --diff-filter=ACMRT
git diff --name-status
git diff --no-color --diff-filter=ACMRT
git ls-files -o --exclude-standard
```

## Scan Commands

Define the high-confidence token pattern once per session:

```powershell
$tokenPatterns = 'AKIA[0-9A-Z]{16}|ASIA[0-9A-Z]{16}|ghp_[A-Za-z0-9]{36}|gho_[A-Za-z0-9]{36}|ghs_[A-Za-z0-9]{36}|ghu_[A-Za-z0-9]{36}|github_pat_[A-Za-z0-9_]{22,}|sk-ant-[A-Za-z0-9-]{20,}|sk-proj-[A-Za-z0-9_-]{20,}|sk-[A-Za-z0-9_-]{20,}|xox[baprs]-[0-9A-Za-z-]{10,}|AIza[0-9A-Za-z_-]{35}|BEGIN [A-Z ]*PRIVATE KEY|_authToken|npm_[A-Za-z0-9]{36}|glpat-[A-Za-z0-9_-]{20}|oy2[a-z0-9]{40,}|AccountKey=[A-Za-z0-9+/=]{20,}|SharedAccessSignature|eyJhbGciOi'
```

Scan the selected diff for high-confidence patterns, loose credentials on added lines, and sensitive filenames:

```powershell
$diff = git diff --no-color --diff-filter=ACMRT "${baseRef}...HEAD"
$diff | Select-String -Pattern $tokenPatterns
$diff | Select-String -Pattern '^\+[^+].*((api[_-]?key|apikey|secret|passw(or)?d|token|credential|connectionstring|pwd|bearer |authorization:)[^a-z0-9]{0,3}[A-Za-z0-9+/_=@:;.,-]{8,}|Server=.*;.*Password=|Data Source=.*Password=|mongodb(\+srv)?://[^ ]*:[^ ]*@|postgres(ql)?://[^ ]*:[^ ]*@|mysql://[^ ]*:[^ ]*@|redis://[^ ]*:[^ ]*@|amqps?://[^ ]*:[^ ]*@|https?://[^/ ]*:[^/@ ]*@)'
git diff --name-only --diff-filter=ACMRT "${baseRef}...HEAD" |
  Select-String -Pattern '\.(env|pem|key|pfx|p12|jks|keystore|ppk)$|id_rsa|id_ed25519|credentials|secrets?\.|\.npmrc|nuget\.config|appsettings|\.netrc|\.pypirc|authinfo'
```

For staged or working-tree scopes, replace the `git diff` source with the matching command from the scope section. If a tool requires a file input, write the diff to a system temporary file with `New-TemporaryFile`, scan it, and remove it before finishing.

Expect binary assets in some diffs. Keep filename checks for them, but avoid treating binary body noise as secret evidence unless a text extraction or direct file review shows readable secret material.

For non-ignored untracked files in the selected scope, scan file contents directly:

```powershell
$untracked = git ls-files -o --exclude-standard
if ($untracked) { rg -n -I -e $tokenPatterns -- $untracked }
if ($untracked) { rg -n -I -i -e '(password|passwd|secret|api[_-]?key|apikey|token|credential|connectionstring|pwd)\s*[:=]' -- $untracked }
```

## Triage

Review each candidate in context before reporting it. Use `git diff --no-color -U5 ... -- <file>` or read the changed file directly when a hit is ambiguous.

Treat [AGENTS.md](../../../AGENTS.md#checked-in-password-exceptions) as the sole allowlist for concrete password, password-equivalent, password-hash, and password-salt values introduced by the selected change set. Do not dismiss a changed value because it is local, test-only, inert, sample, generated, hashed, or salted unless the allowlist explicitly permits it.

Ignore obvious non-findings only after context review: variable names without values, `CancellationToken`, parser tokens, documentation placeholders such as `<strong-password>`, public keys or public certificates without private material, and clearly synthetic examples that are not executable configuration or fixture data.

If a hit may predate the selected diff, say that the fast scan cannot determine the full exposure and recommend `security-audit`.

## Report Contract

Structure the final report as:

- TLDR line: whether the selected change set appears to introduce credible leaks.
- Scope: staged, working-tree, branch diff with base ref, and any pathspecs.
- Dirty worktree handling: whether staged, unstaged, and untracked files were included or intentionally excluded.
- Findings: severity, file path, line or hunk context, category, safe evidence signature, and action. Do not include raw secret values.
- Non-findings: important benign hits reviewed and why they were dismissed.
- Limitations: state that full history, all git blobs, dependency risk, and code-level vulnerability review were not covered.
- Commands/checks run.
- Highest-priority next action, if any.

Use `CRITICAL` for private key material or values that look real and operational. Use `WARNING` for ambiguous credentials or sensitive files that cannot be safely dismissed.
