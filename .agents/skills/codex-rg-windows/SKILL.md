---
name: codex-rg-windows
description: Compare the current Windows Codex CLI bundled ripgrep version with the local system rg version and print a matching Winget install or update script when they differ. Use when the user asks whether their installed rg should be updated to match Codex, which rg/ripgrep version Codex CLI bundles on Windows, or why Codex and local PowerShell report different rg versions.
---

# Codex Ripgrep On Windows

Use this skill on Windows when the user wants to compare the Codex CLI bundled `rg.exe` with the local `rg.exe` available in normal PowerShell.

Do not install anything or modify `PATH` from this skill. Print the detected Codex CLI version, the bundled `rg.exe` path and version, the local system `rg.exe` path and version when present, whether they match, and a separate Winget script the user can run only if they want the local version to match Codex.

## Compare Codex And Local `rg`

Run this from PowerShell:

```powershell
$ErrorActionPreference = 'Stop'

Write-Host 'Codex CLI:'
codex --version
Write-Host ''

$candidatePaths = New-Object System.Collections.Generic.List[string]

Get-Command rg -All -ErrorAction SilentlyContinue |
    Select-Object -ExpandProperty Source -Unique |
    Where-Object { $_ -and ($_ -match '\\codex-path\\rg\.exe$' -or $_ -match '\\@openai\\codex\\') } |
    ForEach-Object { $candidatePaths.Add($_) }

$npmRoot = $null
try {
    $npmRoot = (npm root -g 2>$null).Trim()
} catch {
    $npmRoot = $null
}

if ($npmRoot) {
    $candidatePaths.Add((Join-Path $npmRoot '@openai\codex\node_modules\@openai\codex-win32-x64\vendor\x86_64-pc-windows-msvc\codex-path\rg.exe'))
    $candidatePaths.Add((Join-Path $npmRoot '@openai\codex\node_modules\@openai\codex-win32-arm64\vendor\aarch64-pc-windows-msvc\codex-path\rg.exe'))
}

$rgPath = $candidatePaths |
    Select-Object -Unique |
    Where-Object { Test-Path -LiteralPath $_ -PathType Leaf } |
    Select-Object -First 1

if (-not $rgPath) {
    throw 'Could not find Codex bundled rg.exe. Run Get-Command rg -All and inspect Codex/npm/OpenAI paths manually.'
}

$codexVersionOutput = & $rgPath --version
$codexVersionLine = $codexVersionOutput | Select-Object -First 1
$codexVersion = if ($codexVersionLine -match '^ripgrep\s+([^\s]+)') { $Matches[1] } else { '<unknown>' }

Write-Host 'Codex bundled rg.exe:'
Write-Host $rgPath
Write-Host ''
Write-Host 'Codex bundled rg version:'
$codexVersionOutput
Write-Host ''

$localCandidates = New-Object System.Collections.Generic.List[string]

Get-Command rg -All -ErrorAction SilentlyContinue |
    Select-Object -ExpandProperty Source -Unique |
    Where-Object {
        $_ -and
        $_ -notmatch '\\@openai\\codex\\' -and
        $_ -notmatch '\\codex-path\\rg\.exe$' -and
        $_ -notmatch '^C:\\Program Files\\WindowsApps\\'
    } |
    ForEach-Object { $localCandidates.Add($_) }

$wingetRg = Join-Path $env:LOCALAPPDATA 'Microsoft\WinGet\Links\rg.exe'
$localCandidates.Add($wingetRg)

$localRg = $localCandidates |
    Select-Object -Unique |
    Where-Object { Test-Path -LiteralPath $_ -PathType Leaf } |
    Select-Object -First 1

if ($localRg) {
    $localVersionOutput = & $localRg --version
    $localVersionLine = $localVersionOutput | Select-Object -First 1
    $localVersion = if ($localVersionLine -match '^ripgrep\s+([^\s]+)') { $Matches[1] } else { '<unknown>' }

    Write-Host 'Local system rg.exe:'
    Write-Host $localRg
    Write-Host ''
    Write-Host 'Local system rg version:'
    $localVersionOutput
    Write-Host ''

    if ($localVersion -eq $codexVersion) {
        Write-Host "Local rg already matches Codex bundled rg: $codexVersion"
        return
    }

    Write-Host "Local rg version ($localVersion) differs from Codex bundled rg ($codexVersion)."
} else {
    Write-Host 'Local system rg.exe was not found outside Codex private paths.'
}

$installScript = @"
# Run separately in a normal PowerShell session if you want local rg to match Codex.
winget show --id BurntSushi.ripgrep.MSVC -e --versions
winget install --id BurntSushi.ripgrep.MSVC -e --version $codexVersion

# If Winget no longer offers $codexVersion, install the current package instead:
# winget install --id BurntSushi.ripgrep.MSVC -e

# Verify after restarting PowerShell:
# rg --version
# where.exe rg
"@

Write-Host ''
Write-Host 'Optional matching install/update script:'
Write-Host $installScript
```

## Interpreting The Output

- If the local and Codex versions match, no action is needed.
- If the local version is missing or differs, run the printed Winget command only if you want local PowerShell `rg` to match Codex's bundled version.
- If `rg` resolves only to a path under `C:\Program Files\WindowsApps\OpenAI.Codex_...`, do not treat that as the local system install; install a normal ripgrep package instead.
- Keep Codex private package paths out of permanent `PATH` because they can change when Codex updates.
