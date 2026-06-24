[CmdletBinding()]
param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Debug",

    [string]$OutputPath = "openapi.json"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$RepoRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot "..\.."))

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

function Get-GeneratedOpenApiPath {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ProjectName,

        [Parameter(Mandatory = $true)]
        [string]$DocumentsDirectory
    )

    $projectNamedDocumentPath = Join-Path $DocumentsDirectory "$ProjectName.json"
    if (Test-Path -LiteralPath $projectNamedDocumentPath -PathType Leaf) {
        return $projectNamedDocumentPath
    }

    $generatedDocuments = @(Get-ChildItem -LiteralPath $DocumentsDirectory -File -Filter "*.json" -ErrorAction SilentlyContinue)
    if ($generatedDocuments.Count -eq 1) {
        return $generatedDocuments[0].FullName
    }

    if ($generatedDocuments.Count -gt 1) {
        throw "OpenAPI generation produced multiple JSON documents under $DocumentsDirectory."
    }

    throw "OpenAPI generation did not produce the expected file under $DocumentsDirectory."
}

$dotnet = (Get-Command dotnet -ErrorAction Stop).Source
$projectPath = Join-RepoPath "tests/Sqloom.TestApp/Sqloom.TestApp.csproj"
$destinationPath = Resolve-OutputPath $OutputPath
$documentsDirectory = Join-RepoPath "artifacts/openapi/Sqloom.TestApp/$($Configuration.ToLowerInvariant())"
New-Item -ItemType Directory -Force -Path $documentsDirectory | Out-Null
Get-ChildItem -LiteralPath $documentsDirectory -File -Filter "*.json" -ErrorAction SilentlyContinue |
    Remove-Item -Force

Push-Location $RepoRoot
try {
    & $dotnet build $projectPath --no-incremental --tl:off --nologo "-clp:ErrorsOnly;NoSummary" "-p:OpenApiGenerateDocuments=true" "-p:OpenApiDocumentsDirectory=$documentsDirectory" "-p:Configuration=$Configuration"
    if ($LASTEXITCODE -ne 0) {
        throw "OpenAPI generation build failed with exit code $LASTEXITCODE."
    }
}
finally {
    Pop-Location
}

$generatedSourcePath = Get-GeneratedOpenApiPath -ProjectName "Sqloom.TestApp" -DocumentsDirectory $documentsDirectory

$destinationDirectory = Split-Path -Parent $destinationPath
New-Item -ItemType Directory -Force -Path $destinationDirectory | Out-Null
Copy-Item -LiteralPath $generatedSourcePath -Destination $destinationPath -Force

Write-Host "Generated OpenAPI contract: $($destinationPath.Substring($RepoRoot.Length + 1))"
