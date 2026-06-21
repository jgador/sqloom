[CmdletBinding()]
param(
    [string]$ConnectionString = 'Server=localhost;Database=AdventureWorksLT2025;Integrated Security=True;TrustServerCertificate=True;MultipleActiveResultSets=True',
    [string]$DotnetEfVersion = '10.0.1',
    [string[]]$Tables = @()
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Get-RequiredCommandPath {
    param([Parameter(Mandatory = $true)][string]$Name)

    return (Get-Command $Name -ErrorAction Stop).Source
}

function Invoke-CheckedProcess {
    param(
        [Parameter(Mandatory = $true)][string]$FilePath,
        [Parameter(Mandatory = $true)][string[]]$Arguments,
        [string]$WorkingDirectory
    )

    if ([string]::IsNullOrWhiteSpace($WorkingDirectory)) {
        & $FilePath @Arguments
    }
    else {
        Push-Location $WorkingDirectory
        try {
            & $FilePath @Arguments
        }
        finally {
            Pop-Location
        }
    }

    if ($LASTEXITCODE -ne 0) {
        throw "Command failed with exit code ${LASTEXITCODE}: $FilePath $($Arguments -join ' ')"
    }
}

function Remove-DirectoryWithRetries {
    param([Parameter(Mandatory = $true)][string]$Path)

    for ($attempt = 1; $attempt -le 10; $attempt++) {
        if (-not (Test-Path -LiteralPath $Path)) {
            return
        }

        try {
            Remove-Item -LiteralPath $Path -Recurse -Force
            return
        }
        catch {
            if ($attempt -eq 10) {
                Write-Warning "Could not delete temporary directory '$Path': $($_.Exception.Message)"
                return
            }

            Start-Sleep -Milliseconds 500
        }
    }
}

function Get-SelectedTables {
    param(
        [string[]]$ExplicitTables
    )

    if ($null -ne $ExplicitTables -and $ExplicitTables.Count -gt 0) {
        return $ExplicitTables
    }

    return @(
        'dbo.BuildVersion',
        'dbo.ErrorLog',
        'SalesLT.Address',
        'SalesLT.Customer',
        'SalesLT.CustomerAddress',
        'SalesLT.Product',
        'SalesLT.ProductCategory',
        'SalesLT.ProductDescription',
        'SalesLT.ProductModel',
        'SalesLT.ProductModelProductDescription',
        'SalesLT.SalesOrderDetail',
        'SalesLT.SalesOrderHeader'
    )
}

$dotnetPath = Get-RequiredCommandPath -Name 'dotnet'
$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = (Resolve-Path (Join-Path $scriptRoot '..\..')).Path
$appProjectPath = (Resolve-Path (Join-Path $scriptRoot 'Sqloom.TestApp.csproj')).Path
$appProjectRoot = Split-Path -Parent $appProjectPath
$selectedTables = Get-SelectedTables -ExplicitTables $Tables

# Keep the reverse-engineered model inside Sqloom.TestApp so the sample app owns the generated EF types.
$generatedRoot = Join-Path $appProjectRoot 'Generated'
$stageRoot = Join-Path $repoRoot 'artifacts\ef-scaffold\Sqloom.TestApp'
$contextDir = '..\..\artifacts\ef-scaffold\Sqloom.TestApp\Generated'
$entitiesDir = '..\..\artifacts\ef-scaffold\Sqloom.TestApp\Generated\Entities'

if (Test-Path -LiteralPath $stageRoot) {
    $resolvedStageRoot = (Resolve-Path -LiteralPath $stageRoot).Path
    if (-not $resolvedStageRoot.StartsWith($repoRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Refusing to clear scaffold staging outside the repository root: $resolvedStageRoot"
    }

    Remove-Item -LiteralPath $resolvedStageRoot -Recurse -Force
}

New-Item -ItemType Directory -Path $stageRoot -Force | Out-Null

$efToolPath = Join-Path ([System.IO.Path]::GetTempPath()) "sqloom-dotnet-ef-$DotnetEfVersion"
$dotnetEfExePath = Join-Path $efToolPath 'dotnet-ef.exe'
if (-not (Test-Path -LiteralPath $dotnetEfExePath)) {
    if (Test-Path -LiteralPath $efToolPath) {
        Remove-Item -LiteralPath $efToolPath -Recurse -Force
    }

    New-Item -ItemType Directory -Path $efToolPath -Force | Out-Null
    Invoke-CheckedProcess -FilePath $dotnetPath -WorkingDirectory $repoRoot -Arguments @(
        'tool',
        'install',
        'dotnet-ef',
        '--version',
        $DotnetEfVersion,
        '--tool-path',
        $efToolPath
    )
}

$designHostRoot = Join-Path ([System.IO.Path]::GetTempPath()) "sqloom-testapp-ef-designhost-$([Guid]::NewGuid().ToString('N'))"
$designHostProjectPath = Join-Path $designHostRoot 'Sqloom.TestApp.DesignHost.csproj'
$designHostProgramPath = Join-Path $designHostRoot 'Program.cs'
$escapedAppProjectPath = $appProjectPath.Replace('&', '&amp;')

New-Item -ItemType Directory -Path $designHostRoot -Force | Out-Null
Set-Content -LiteralPath $designHostProjectPath -Value @"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>disable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="$DotnetEfVersion" PrivateAssets="all" />
    <ProjectReference Include="$escapedAppProjectPath" />
  </ItemGroup>
</Project>
"@
Set-Content -LiteralPath $designHostProgramPath -Value @"
internal static class Program
{
    public static void Main(string[] args)
    {
    }
}
"@

Invoke-CheckedProcess -FilePath $dotnetPath -WorkingDirectory $repoRoot -Arguments @(
    'restore',
    $designHostProjectPath,
    '--nologo'
)

$scaffoldArguments = @(
    'dbcontext',
    'scaffold',
    $ConnectionString,
    'Microsoft.EntityFrameworkCore.SqlServer',
    '--project',
    $appProjectPath,
    '--startup-project',
    $designHostProjectPath,
    '--context',
    'TestAppProductCatalogDbContext',
    '--context-dir',
    $contextDir,
    '--output-dir',
    $entitiesDir,
    '--context-namespace',
    'Sqloom.TestApp',
    '--namespace',
    'Sqloom.TestApp.Entities',
    '--no-onconfiguring',
    '--force'
)

foreach ($table in $selectedTables) {
    $scaffoldArguments += @('--table', $table)
}

try {
    Invoke-CheckedProcess -FilePath $dotnetEfExePath -WorkingDirectory $repoRoot -Arguments $scaffoldArguments
}
finally {
    if (Test-Path -LiteralPath $designHostRoot) {
        Remove-DirectoryWithRetries -Path $designHostRoot
    }
}

if (Test-Path -LiteralPath $generatedRoot) {
    $resolvedGeneratedRoot = (Resolve-Path -LiteralPath $generatedRoot).Path
    if (-not $resolvedGeneratedRoot.StartsWith($appProjectRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Refusing to clear scaffold output outside the Sqloom.TestApp project: $resolvedGeneratedRoot"
    }

    Remove-Item -LiteralPath $resolvedGeneratedRoot -Recurse -Force
}

Copy-Item -Path (Join-Path $stageRoot 'Generated') -Destination $generatedRoot -Recurse -Force
Invoke-CheckedProcess -FilePath $dotnetPath -WorkingDirectory $repoRoot -Arguments @(
    'format',
    'style',
    $appProjectPath,
    '--no-restore',
    '--severity',
    'warn',
    '--verbosity',
    'minimal'
)

Write-Host "Raw scaffold output staged at: $stageRoot"
Write-Host "Reverse-engineered model written to: $generatedRoot"
Write-Host "Review the generated files under tests\\Sqloom.TestApp before committing."
Write-Host "Scaffolded tables: $($selectedTables -join ', ')"
