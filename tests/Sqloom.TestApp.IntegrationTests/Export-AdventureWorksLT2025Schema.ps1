[CmdletBinding()]
param(
    [string]$DacpacPath = (Join-Path $PSScriptRoot 'AdventureWorksLT2025.dacpac'),
    [string]$SchemaSqlPath = (Join-Path $PSScriptRoot 'AdventureWorksLT2025.schema.sql'),
    [string]$ServerName = 'localhost',
    [string]$ScratchDatabaseName = 'Sqloom_AdventureWorksLT2025_SchemaExport',
    [string]$SqlPackageVersion = '170.4.83'
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
        [Parameter(Mandatory = $true)][string[]]$Arguments
    )

    & $FilePath @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "Command failed with exit code ${LASTEXITCODE}: $FilePath $($Arguments -join ' ')"
    }
}

function Wait-ForSqlConnection {
    param(
        [Parameter(Mandatory = $true)][string]$SqlCmdPath,
        [Parameter(Mandatory = $true)][string]$ServerName
    )

    for ($attempt = 1; $attempt -le 30; $attempt++) {
        & $SqlCmdPath -S $ServerName -Q 'SELECT 1;' | Out-Null
        if ($LASTEXITCODE -eq 0) {
            return
        }

        Start-Sleep -Seconds 1
    }

    throw "Timed out waiting for SQL Server to accept connections on $ServerName"
}

$dotnetPath = Get-RequiredCommandPath -Name 'dotnet'
$sqlCmdPath = Get-RequiredCommandPath -Name 'sqlcmd'

$resolvedDacpacPath = (Resolve-Path -LiteralPath $DacpacPath).Path
$resolvedSchemaSqlPath = [System.IO.Path]::GetFullPath($SchemaSqlPath)
$schemaOutputDirectory = Split-Path -Path $resolvedSchemaSqlPath -Parent
if ([string]::IsNullOrWhiteSpace($schemaOutputDirectory)) {
    throw "SchemaSqlPath must include a file name: $SchemaSqlPath"
}

New-Item -ItemType Directory -Force -Path $schemaOutputDirectory | Out-Null

$sqlPackageToolPath = Join-Path ([System.IO.Path]::GetTempPath()) "sqloom-sqlpackage-$SqlPackageVersion"
$sqlPackageExePath = Join-Path $sqlPackageToolPath 'sqlpackage.exe'
if (-not (Test-Path -LiteralPath $sqlPackageExePath)) {
    if (Test-Path -LiteralPath $sqlPackageToolPath) {
        Remove-Item -LiteralPath $sqlPackageToolPath -Recurse -Force
    }

    New-Item -ItemType Directory -Force -Path $sqlPackageToolPath | Out-Null
    Invoke-CheckedProcess -FilePath $dotnetPath -Arguments @(
        'tool',
        'install',
        'microsoft.sqlpackage',
        '--version',
        $SqlPackageVersion,
        '--tool-path',
        $sqlPackageToolPath
    )
}

$dropDatabaseCommand = "IF DB_ID(N'$ScratchDatabaseName') IS NOT NULL BEGIN ALTER DATABASE [$ScratchDatabaseName] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [$ScratchDatabaseName]; END"

try {
    Wait-ForSqlConnection -SqlCmdPath $sqlCmdPath -ServerName $ServerName
    & $sqlCmdPath -S $ServerName -Q $dropDatabaseCommand | Out-Null

    if (Test-Path -LiteralPath $resolvedSchemaSqlPath) {
        Remove-Item -LiteralPath $resolvedSchemaSqlPath -Force
    }

    Invoke-CheckedProcess -FilePath $sqlPackageExePath -Arguments @(
        '/Action:Publish',
        "/SourceFile:$resolvedDacpacPath",
        "/TargetServerName:$ServerName",
        "/TargetDatabaseName:$ScratchDatabaseName",
        '/p:CreateNewDatabase=True',
        '/p:BlockOnPossibleDataLoss=False',
        '/TargetTrustServerCertificate:True',
        '/Quiet:True'
    )

    Invoke-CheckedProcess -FilePath $sqlPackageExePath -Arguments @(
        '/Action:Extract',
        "/SourceServerName:$ServerName",
        "/SourceDatabaseName:$ScratchDatabaseName",
        "/TargetFile:$resolvedSchemaSqlPath",
        '/p:ExtractTarget=File',
        '/p:ScriptSortElementsByName=True',
        '/OverwriteFiles:True',
        '/SourceTrustServerCertificate:True',
        '/Quiet:True'
    )
}
finally {
    try {
        & $sqlCmdPath -S $ServerName -Q $dropDatabaseCommand | Out-Null
    }
    catch {
    }
}

Write-Host "Wrote schema script to $resolvedSchemaSqlPath"
