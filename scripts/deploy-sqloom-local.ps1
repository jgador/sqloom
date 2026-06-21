param(
    [switch]$SkipSmoke
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

. (Join-Path $PSScriptRoot "Sqloom.Tooling.ps1")

$context = Get-SqloomToolingContext -ScriptPath $PSCommandPath

Write-Host "Sqloom local deploy"
Write-Host "dotnet: $($context.DotNet)"
Write-Host "repo: $($context.RepoRoot)"
Write-Host "pack configuration: $($context.PackConfiguration)"
Write-Host "package feed: $($context.PackageFeedPath)"
Write-Host "local tool path: $($context.LocalToolPath)"
Write-Host "wrapper path: $($context.WrapperPath)"

Push-Location $context.RepoRoot
try
{
    Invoke-SqloomPackSet -Context $context
    Assert-SqloomPackagesExist -Context $context
    Install-SqloomToolPath -Context $context -ToolPath $context.LocalToolPath
    Write-SqloomLocalWrapper -Context $context
    Ensure-SqloomLocalWrapperPath -Context $context

    $localCommand = (Get-Command "sqloom-local" -ErrorAction Stop).Source

    & $localCommand --version
    if ($LASTEXITCODE -ne 0)
    {
        throw "sqloom-local version check failed."
    }

    & $localCommand --help
    if ($LASTEXITCODE -ne 0)
    {
        throw "sqloom-local wrapper help check failed."
    }

    if (-not $SkipSmoke)
    {
        $sampleAppProject = Join-Path $context.RepoRoot "tests\Sqloom.TestApp\Sqloom.TestApp.csproj"
        & $localCommand replay $sampleAppProject --target "GET /api/products/by-category"
        if ($LASTEXITCODE -ne 0)
        {
            throw "sqloom-local sample app smoke check failed."
        }
    }
}
finally
{
    Pop-Location
}

Write-Host ""
Write-Host "Sqloom local deploy complete."
Write-Host "sqloom-local is available in this PowerShell session."
Write-Host "The wrapper directory is also ensured on the user PATH for new terminals:"
Write-Host $context.WrapperBinPath
