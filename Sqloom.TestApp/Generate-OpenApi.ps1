[CmdletBinding()]
param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Debug",

    [string]$OutputPath = "openapi.json"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$RepoRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot "..\..\.."))

function Join-RepoPath {
    param(
        [Parameter(Mandatory = $true)]
        [string]$RelativePath
    )

    return [System.IO.Path]::GetFullPath((Join-Path $RepoRoot ($RelativePath -replace '/', [System.IO.Path]::DirectorySeparatorChar)))
}

function Assert-InRepo {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path
    )

    $fullPath = [System.IO.Path]::GetFullPath($Path)
    $rootPrefix = $RepoRoot.TrimEnd([System.IO.Path]::DirectorySeparatorChar, [System.IO.Path]::AltDirectorySeparatorChar) + [System.IO.Path]::DirectorySeparatorChar
    if (-not ($fullPath.Equals($RepoRoot, [System.StringComparison]::OrdinalIgnoreCase) -or $fullPath.StartsWith($rootPrefix, [System.StringComparison]::OrdinalIgnoreCase))) {
        throw "Refusing to use path outside repository root: $fullPath"
    }

    return $fullPath
}

function Resolve-OutputPath {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path
    )

    if ([System.IO.Path]::IsPathRooted($Path)) {
        return Assert-InRepo $Path
    }

    return Assert-InRepo (Join-Path $PSScriptRoot $Path)
}

$dotnet = (Get-Command dotnet -ErrorAction Stop).Source
$backendDirectory = Join-RepoPath "backend"
$backendProjectPath = Join-RepoPath "backend/tools/Sqloom.TestApp/Sqloom.TestApp.csproj"
$generatedSourcePath = Join-RepoPath "backend/artifacts/obj/Sqloom.TestApp/Sqloom.TestApp.json"
$destinationPath = Resolve-OutputPath $OutputPath

Push-Location $backendDirectory
try {
    & $dotnet build $backendProjectPath --tl:off --nologo "-clp:ErrorsOnly;NoSummary" "-p:OpenApiGenerateDocuments=true" "-p:Configuration=$Configuration"
    if ($LASTEXITCODE -ne 0) {
        throw "OpenAPI generation build failed with exit code $LASTEXITCODE."
    }
}
finally {
    Pop-Location
}

if (-not (Test-Path -LiteralPath $generatedSourcePath -PathType Leaf)) {
    throw "OpenAPI generation did not produce the expected file: $generatedSourcePath"
}

$destinationDirectory = Split-Path -Parent $destinationPath
New-Item -ItemType Directory -Force -Path $destinationDirectory | Out-Null
Copy-Item -LiteralPath $generatedSourcePath -Destination $destinationPath -Force

Write-Host "Generated OpenAPI contract: $($destinationPath.Substring($RepoRoot.Length + 1))"
