Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Get-SqloomToolingContext
{
    param(
        [Parameter(Mandatory = $true)]
        [string]$ScriptPath
    )

    $scriptsRoot = Split-Path -Parent $ScriptPath
    $repoRoot = (Resolve-Path (Join-Path $scriptsRoot "..")).Path
    $dotnet = (Get-Command dotnet -ErrorAction Stop).Source
    $solutionPath = Join-Path $repoRoot "Sqloom.slnx"
    $packageFeedPath = Join-Path $repoRoot "artifacts\packages\sqloom"
    $localToolPath = Join-Path $repoRoot ".tools\sqloom-local"
    $wrapperBinPath = Join-Path $repoRoot ".tools\bin"
    $wrapperPath = Join-Path $wrapperBinPath "sqloom-local.cmd"
    $verifyToolPath = Join-Path $repoRoot "artifacts\tools\sqloom-verify"

    [xml]$versionXml = Get-Content (Join-Path $repoRoot "Directory.Build.props")
    $packageVersion = $versionXml.Project.PropertyGroup.Version

    if ([string]::IsNullOrWhiteSpace($packageVersion))
    {
        throw "Directory.Build.props must define <Version> for Sqloom packages."
    }

    if ($packageVersion.StartsWith("v", [System.StringComparison]::OrdinalIgnoreCase))
    {
        throw "Directory.Build.props <Version> must use a bare NuGet version like 0.1.0, not v0.1.0. Use the leading 'v' only for Git tags or release titles."
    }

    return [pscustomobject]@{
        RepoRoot = $repoRoot
        DotNet = $dotnet
        SolutionPath = $solutionPath
        PackageFeedPath = $packageFeedPath
        LocalToolPath = $localToolPath
        WrapperBinPath = $wrapperBinPath
        WrapperPath = $wrapperPath
        VerifyToolPath = $verifyToolPath
        PackConfiguration = "Release"
        PackageVersion = $packageVersion
    }
}

function Get-SqloomPackProjects
{
    param(
        [Parameter(Mandatory = $true)]
        [string]$RepoRoot
    )

    return @(
        Join-Path $RepoRoot "src\Sqloom.Core\Sqloom.Core.csproj"
        Join-Path $RepoRoot "src\Sqloom.QueryStore\Sqloom.QueryStore.csproj"
        Join-Path $RepoRoot "src\Sqloom.AzureSql\Sqloom.AzureSql.csproj"
        Join-Path $RepoRoot "src\Sqloom.AspNetCore\Sqloom.AspNetCore.csproj"
        Join-Path $RepoRoot "src\Sqloom.Testing\Sqloom.Testing.csproj"
        Join-Path $RepoRoot "src\Sqloom.Host\Sqloom.Host.csproj"
    )
}

function Get-SqloomPackagePaths
{
    param(
        [Parameter(Mandatory = $true)]
        [pscustomobject]$Context
    )

    return @(
        Join-Path $Context.PackageFeedPath "Sqloom.Core.$($Context.PackageVersion).nupkg"
        Join-Path $Context.PackageFeedPath "Sqloom.QueryStore.$($Context.PackageVersion).nupkg"
        Join-Path $Context.PackageFeedPath "Sqloom.AzureSql.$($Context.PackageVersion).nupkg"
        Join-Path $Context.PackageFeedPath "Sqloom.AspNetCore.$($Context.PackageVersion).nupkg"
        Join-Path $Context.PackageFeedPath "Sqloom.Testing.$($Context.PackageVersion).nupkg"
        Join-Path $Context.PackageFeedPath "sqloom.$($Context.PackageVersion).nupkg"
    )
}

function Get-SqloomPublicPackagePaths
{
    param(
        [Parameter(Mandatory = $true)]
        [pscustomobject]$Context
    )

    return @(
        Join-Path $Context.PackageFeedPath "Sqloom.Core.$($Context.PackageVersion).nupkg"
        Join-Path $Context.PackageFeedPath "Sqloom.QueryStore.$($Context.PackageVersion).nupkg"
        Join-Path $Context.PackageFeedPath "Sqloom.Testing.$($Context.PackageVersion).nupkg"
        Join-Path $Context.PackageFeedPath "sqloom.$($Context.PackageVersion).nupkg"
    )
}

function Assert-PathUnderRoot
{
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path,
        [Parameter(Mandatory = $true)]
        [string]$RootPath,
        [Parameter(Mandatory = $true)]
        [string]$Label
    )

    $normalizedRoot = [System.IO.Path]::GetFullPath($RootPath).TrimEnd("\", "/")
    $normalizedPath = [System.IO.Path]::GetFullPath($Path)

    if (-not $normalizedPath.StartsWith($normalizedRoot, [System.StringComparison]::OrdinalIgnoreCase))
    {
        throw "$Label must stay under $normalizedRoot, but resolved to $normalizedPath."
    }

    if ($normalizedPath.Equals($normalizedRoot, [System.StringComparison]::OrdinalIgnoreCase))
    {
        throw "$Label cannot target the protected root path $normalizedRoot."
    }
}

function Reset-Directory
{
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path,
        [Parameter(Mandatory = $true)]
        [string]$RootPath,
        [Parameter(Mandatory = $true)]
        [string]$Label
    )

    Assert-PathUnderRoot -Path $Path -RootPath $RootPath -Label $Label

    if (Test-Path -LiteralPath $Path)
    {
        Remove-Item -LiteralPath $Path -Recurse -Force
    }

    New-Item -ItemType Directory -Path $Path -Force | Out-Null
}

function Ensure-Directory
{
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path,
        [Parameter(Mandatory = $true)]
        [string]$RootPath,
        [Parameter(Mandatory = $true)]
        [string]$Label
    )

    Assert-PathUnderRoot -Path $Path -RootPath $RootPath -Label $Label

    New-Item -ItemType Directory -Path $Path -Force | Out-Null
}

function Invoke-DotNet
{
    param(
        [Parameter(Mandatory = $true)]
        [pscustomobject]$Context,
        [Parameter(Mandatory = $true)]
        [string[]]$Arguments
    )

    Write-Host "dotnet $($Arguments -join ' ')"
    & $Context.DotNet @Arguments

    if ($LASTEXITCODE -ne 0)
    {
        throw "dotnet $($Arguments[0]) failed with exit code $LASTEXITCODE."
    }
}

function Invoke-SqloomPackSet
{
    param(
        [Parameter(Mandatory = $true)]
        [pscustomobject]$Context,
        [switch]$NoBuild,
        [switch]$NoRestore
    )

    Reset-Directory -Path $Context.PackageFeedPath -RootPath $Context.RepoRoot -Label "Sqloom package feed"

    foreach ($projectPath in (Get-SqloomPackProjects -RepoRoot $Context.RepoRoot))
    {
        $arguments = @(
            "pack"
            $projectPath
            "-c"
            $Context.PackConfiguration
            "--tl:off"
            "--nologo"
            "-clp:ErrorsOnly;NoSummary"
            "-o"
            $Context.PackageFeedPath
        )

        if ($NoBuild)
        {
            $arguments += "--no-build"
        }

        if ($NoRestore)
        {
            $arguments += "--no-restore"
        }

        Invoke-DotNet -Context $Context -Arguments $arguments
    }
}

function Install-SqloomToolPath
{
    param(
        [Parameter(Mandatory = $true)]
        [pscustomobject]$Context,
        [Parameter(Mandatory = $true)]
        [string]$ToolPath
    )

    Assert-PathUnderRoot -Path $ToolPath -RootPath $Context.RepoRoot -Label "Sqloom tool path"

    if (Test-Path -LiteralPath $ToolPath)
    {
        try
        {
            Invoke-DotNet -Context $Context -Arguments @(
                "tool"
                "uninstall"
                "--tool-path"
                $ToolPath
                "sqloom"
            )
        }
        catch
        {
            Write-Host "Existing sqloom tool-path uninstall failed; removing the directory directly."
        }

        Remove-Item -LiteralPath $ToolPath -Recurse -Force -ErrorAction SilentlyContinue
    }

    Invoke-DotNet -Context $Context -Arguments @(
        "tool"
        "install"
        "--tool-path"
        $ToolPath
        "sqloom"
        "--add-source"
        $Context.PackageFeedPath
        "--ignore-failed-sources"
    )
}

function Write-SqloomLocalWrapper
{
    param(
        [Parameter(Mandatory = $true)]
        [pscustomobject]$Context
    )

    Ensure-Directory -Path $Context.WrapperBinPath -RootPath $Context.RepoRoot -Label "Sqloom wrapper bin path"

    $wrapperContent = @(
        "@echo off"
        "setlocal"
        """%~dp0..\sqloom-local\sqloom.exe"" %*"
    )

    Set-Content -LiteralPath $Context.WrapperPath -Value $wrapperContent -Encoding ASCII
}

function Test-PathEntryPresent
{
    param(
        [AllowEmptyString()]
        [string]$PathValue,
        [Parameter(Mandatory = $true)]
        [string]$EntryPath
    )

    if ([string]::IsNullOrWhiteSpace($PathValue))
    {
        return $false
    }

    $normalizedEntryPath = [System.IO.Path]::GetFullPath(
        [Environment]::ExpandEnvironmentVariables($EntryPath).Trim('"')).TrimEnd("\", "/")
    $segments = $PathValue.Split(
        ';',
        [System.StringSplitOptions]::RemoveEmptyEntries)

    foreach ($segment in $segments)
    {
        $normalizedSegment = [System.IO.Path]::GetFullPath(
            [Environment]::ExpandEnvironmentVariables($segment).Trim('"')).TrimEnd("\", "/")
        if ($normalizedSegment.Equals($normalizedEntryPath, [System.StringComparison]::OrdinalIgnoreCase))
        {
            return $true
        }
    }

    return $false
}

function Add-PathEntry
{
    param(
        [AllowEmptyString()]
        [string]$PathValue,
        [Parameter(Mandatory = $true)]
        [string]$EntryPath
    )

    if (Test-PathEntryPresent -PathValue $PathValue -EntryPath $EntryPath)
    {
        return $PathValue
    }

    if ([string]::IsNullOrWhiteSpace($PathValue))
    {
        return $EntryPath
    }

    return "$EntryPath;$PathValue"
}

function Ensure-SqloomLocalWrapperPath
{
    param(
        [Parameter(Mandatory = $true)]
        [pscustomobject]$Context
    )

    $userPath = [Environment]::GetEnvironmentVariable(
        "Path",
        [System.EnvironmentVariableTarget]::User)
    $updatedUserPath = Add-PathEntry -PathValue $userPath -EntryPath $Context.WrapperBinPath
    if (-not [string]::Equals($userPath, $updatedUserPath, [System.StringComparison]::Ordinal))
    {
        [Environment]::SetEnvironmentVariable(
            "Path",
            $updatedUserPath,
            [System.EnvironmentVariableTarget]::User)
    }

    $env:PATH = Add-PathEntry -PathValue $env:PATH -EntryPath $Context.WrapperBinPath
}

function Assert-SqloomPackagesExist
{
    param(
        [Parameter(Mandatory = $true)]
        [pscustomobject]$Context
    )

    foreach ($packagePath in (Get-SqloomPackagePaths -Context $Context))
    {
        if (-not (Test-Path -LiteralPath $packagePath))
        {
            throw "Expected Sqloom package was not produced: $packagePath"
        }
    }
}

function Show-SqloomPublishCommands
{
    param(
        [Parameter(Mandatory = $true)]
        [pscustomobject]$Context
    )

    Write-Host ""
    Write-Host "Manual NuGet.org publish commands for public packages:"
    foreach ($packagePath in (Get-SqloomPublicPackagePaths -Context $Context))
    {
        Write-Host "dotnet nuget push `"$packagePath`" --source https://api.nuget.org/v3/index.json --api-key <nuget-api-key>"
    }
}
