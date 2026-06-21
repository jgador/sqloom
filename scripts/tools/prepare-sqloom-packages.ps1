param(
    [switch]$SkipSmoke
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

. (Join-Path $PSScriptRoot "Sqloom.Tooling.ps1")

$context = Get-SqloomToolingContext -ScriptPath $PSCommandPath

Write-Host "Sqloom package preparation"
Write-Host "dotnet: $($context.DotNet)"
Write-Host "repo: $($context.RepoRoot)"
Write-Host "solution: $($context.SolutionPath)"
Write-Host "pack configuration: $($context.PackConfiguration)"
Write-Host "package feed: $($context.PackageFeedPath)"
Write-Host "verify tool path: $($context.VerifyToolPath)"

Push-Location $context.RepoRoot
try
{
    Invoke-DotNet -Context $context -Arguments @(
        "restore"
        $context.SolutionPath
    )

    Invoke-DotNet -Context $context -Arguments @(
        "build"
        $context.SolutionPath
        "-c"
        $context.PackConfiguration
        "--tl:off"
        "--nologo"
        "-clp:ErrorsOnly;NoSummary"
    )

    Invoke-SqloomPackSet -Context $context -NoBuild -NoRestore
    Assert-SqloomPackagesExist -Context $context
    Install-SqloomToolPath -Context $context -ToolPath $context.VerifyToolPath

    $verifyExePath = Join-Path $context.VerifyToolPath "sqloom.exe"
    & $verifyExePath --help
    if ($LASTEXITCODE -ne 0)
    {
        throw "Prepared sqloom tool help check failed."
    }

    if (-not $SkipSmoke)
    {
        $sampleAppProject = Join-Path $context.RepoRoot "tests\Sqloom.TestApp\Sqloom.TestApp.csproj"
        & $verifyExePath replay $sampleAppProject --target "GET /api/products/by-category"
        if ($LASTEXITCODE -ne 0)
        {
            throw "Prepared sqloom package smoke check failed."
        }
    }
}
finally
{
    Pop-Location
}

Show-SqloomPublishCommands -Context $context
