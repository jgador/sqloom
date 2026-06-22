[CmdletBinding()]
param(
    [string]$ConnectionString = 'Server=localhost;Database=AdventureWorksLT2025;Integrated Security=True;TrustServerCertificate=True;MultipleActiveResultSets=True',
    [string]$OutputPath = (Join-Path $PSScriptRoot 'AdventureWorksLT2025.seed.sql')
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

Add-Type -AssemblyName System.Data

function Normalize-ConnectionString {
    param([Parameter(Mandatory = $true)][string]$ConnectionString)

    $normalizedConnectionString = $ConnectionString `
        -replace '(?i)\bTrust Server Certificate\b', 'TrustServerCertificate' `
        -replace '(?i)(^|;)\s*MultipleActiveResultSets\s*=\s*[^;]*', '' `
        -replace '(?i)(^|;)\s*Multiple Active Result Sets\s*=\s*[^;]*', ''
    return $normalizedConnectionString.Trim().Trim(';')
}

function New-SqlConnection {
    param([Parameter(Mandatory = $true)][string]$ConnectionString)

    return [System.Data.SqlClient.SqlConnection]::new((Normalize-ConnectionString -ConnectionString $ConnectionString))
}

function Invoke-Query {
    param(
        [Parameter(Mandatory = $true)][System.Data.SqlClient.SqlConnection]$Connection,
        [Parameter(Mandatory = $true)][string]$CommandText,
        [hashtable]$Parameters = @{}
    )

    $command = $Connection.CreateCommand()
    $command.CommandText = $CommandText
    $command.CommandTimeout = 120

    foreach ($entry in $Parameters.GetEnumerator()) {
        $parameter = $command.CreateParameter()
        $parameter.ParameterName = [string]$entry.Key
        $parameter.Value = if ($null -eq $entry.Value) { [DBNull]::Value } else { $entry.Value }
        [void]$command.Parameters.Add($parameter)
    }

    $adapter = [System.Data.SqlClient.SqlDataAdapter]::new($command)
    $table = [System.Data.DataTable]::new()
    [void]$adapter.Fill($table)
    return ,$table
}

function Quote-Identifier {
    param([Parameter(Mandatory = $true)][string]$Name)

    return '[' + $Name.Replace(']', ']]') + ']'
}

function Get-FullyQualifiedTableName {
    param(
        [Parameter(Mandatory = $true)][string]$SchemaName,
        [Parameter(Mandatory = $true)][string]$TableName
    )

    return ('{0}.{1}' -f (Quote-Identifier $SchemaName), (Quote-Identifier $TableName))
}

function Get-UserTables {
    param([Parameter(Mandatory = $true)][System.Data.SqlClient.SqlConnection]$Connection)

    $table = Invoke-Query -Connection $Connection -CommandText @'
SELECT
    t.object_id AS ObjectId,
    s.name AS SchemaName,
    t.name AS TableName
FROM sys.tables AS t
INNER JOIN sys.schemas AS s
    ON s.schema_id = t.schema_id
WHERE t.is_ms_shipped = 0
  AND NOT (s.name = N'dbo' AND t.name = N'sysdiagrams')
ORDER BY
    s.name,
    t.name;
'@

    return $table.Rows | ForEach-Object {
        [pscustomobject]@{
            ObjectId = [int]$_.ObjectId
            SchemaName = [string]$_.SchemaName
            TableName = [string]$_.TableName
        }
    }
}

function Get-ForeignKeyEdges {
    param([Parameter(Mandatory = $true)][System.Data.SqlClient.SqlConnection]$Connection)

    $table = Invoke-Query -Connection $Connection -CommandText @'
SELECT
    fk.parent_object_id AS ChildObjectId,
    fk.referenced_object_id AS ParentObjectId
FROM sys.foreign_keys AS fk
WHERE fk.is_ms_shipped = 0
  AND fk.parent_object_id <> fk.referenced_object_id;
'@

    return $table.Rows | ForEach-Object {
        [pscustomobject]@{
            ChildObjectId = [int]$_.ChildObjectId
            ParentObjectId = [int]$_.ParentObjectId
        }
    }
}

function Get-OrderedTables {
    param(
        [Parameter(Mandatory = $true)][object[]]$Tables,
        [Parameter(Mandatory = $true)][object[]]$ForeignKeyEdges
    )

    $tableLookup = @{}
    $incomingCount = @{}
    $dependentsByParent = @{}

    foreach ($table in $Tables) {
        $tableLookup[$table.ObjectId] = $table
        $incomingCount[$table.ObjectId] = 0
        $dependentsByParent[$table.ObjectId] = [System.Collections.Generic.List[int]]::new()
    }

    foreach ($edge in $ForeignKeyEdges) {
        if (-not $tableLookup.ContainsKey($edge.ChildObjectId) -or -not $tableLookup.ContainsKey($edge.ParentObjectId)) {
            continue
        }

        $dependentsByParent[$edge.ParentObjectId].Add($edge.ChildObjectId)
        $incomingCount[$edge.ChildObjectId]++
    }

    $available = [System.Collections.Generic.List[object]]::new()
    foreach ($table in ($Tables | Sort-Object SchemaName, TableName)) {
        if ($incomingCount[$table.ObjectId] -eq 0) {
            $available.Add($table)
        }
    }

    $ordered = [System.Collections.Generic.List[object]]::new()
    $addedIds = @{}

    while ($available.Count -gt 0) {
        $nextTable = $available
            | Sort-Object SchemaName, TableName
            | Select-Object -First 1
        [void]$available.Remove($nextTable)
        if ($addedIds.ContainsKey($nextTable.ObjectId)) {
            continue
        }

        $ordered.Add($nextTable)
        $addedIds[$nextTable.ObjectId] = $true

        foreach ($childObjectId in ($dependentsByParent[$nextTable.ObjectId] | Sort-Object)) {
            $incomingCount[$childObjectId]--
            if ($incomingCount[$childObjectId] -eq 0) {
                $available.Add($tableLookup[$childObjectId])
            }
        }
    }

    foreach ($table in ($Tables | Sort-Object SchemaName, TableName)) {
        if (-not $addedIds.ContainsKey($table.ObjectId)) {
            $ordered.Add($table)
        }
    }

    return $ordered
}

function Get-InsertableColumns {
    param(
        [Parameter(Mandatory = $true)][System.Data.SqlClient.SqlConnection]$Connection,
        [Parameter(Mandatory = $true)][int]$ObjectId
    )

    $table = Invoke-Query -Connection $Connection -CommandText @'
SELECT
    c.column_id AS ColumnId,
    c.name AS ColumnName,
    CASE
        WHEN ty.is_user_defined = 1 THEN baseTy.name
        ELSE ty.name
    END AS TypeName,
    c.is_identity AS IsIdentity
FROM sys.columns AS c
INNER JOIN sys.types AS ty
    ON ty.user_type_id = c.user_type_id
INNER JOIN sys.types AS baseTy
    ON baseTy.system_type_id = c.system_type_id
   AND baseTy.user_type_id = c.system_type_id
WHERE c.object_id = @objectId
  AND c.is_computed = 0
  AND c.generated_always_type = 0
  AND ty.name NOT IN (N'rowversion', N'timestamp')
ORDER BY c.column_id;
'@ -Parameters @{
        objectId = $ObjectId
    }

    return $table.Rows | ForEach-Object {
        [pscustomobject]@{
            ColumnId = [int]$_.ColumnId
            ColumnName = [string]$_.ColumnName
            TypeName = [string]$_.TypeName
            IsIdentity = [bool]$_.IsIdentity
        }
    }
}

function Get-PrimaryKeyColumnNames {
    param(
        [Parameter(Mandatory = $true)][System.Data.SqlClient.SqlConnection]$Connection,
        [Parameter(Mandatory = $true)][int]$ObjectId
    )

    $table = Invoke-Query -Connection $Connection -CommandText @'
SELECT
    c.name AS ColumnName
FROM sys.key_constraints AS kc
INNER JOIN sys.index_columns AS ic
    ON ic.object_id = kc.parent_object_id
   AND ic.index_id = kc.unique_index_id
INNER JOIN sys.columns AS c
    ON c.object_id = ic.object_id
   AND c.column_id = ic.column_id
WHERE kc.parent_object_id = @objectId
  AND kc.type = N'PK'
ORDER BY ic.key_ordinal;
'@ -Parameters @{
        objectId = $ObjectId
    }

    return $table.Rows | ForEach-Object { [string]$_.ColumnName }
}

function Format-SqlStringExpression {
    param(
        [Parameter(Mandatory = $true)][string]$Value,
        [Parameter(Mandatory = $true)][bool]$Unicode
    )

    $escaped = $Value.Replace("'", "''")
    if ($Unicode) {
        $escaped = $escaped.Replace("`r`n", "' + CHAR(13) + CHAR(10) + N'")
        $escaped = $escaped.Replace("`n", "' + CHAR(10) + N'")
        $escaped = $escaped.Replace("`r", "' + CHAR(13) + N'")
        return "N'$escaped'"
    }

    $escaped = $escaped.Replace("`r`n", "' + CHAR(13) + CHAR(10) + '")
    $escaped = $escaped.Replace("`n", "' + CHAR(10) + '")
    $escaped = $escaped.Replace("`r", "' + CHAR(13) + '")
    return "'$escaped'"
}

function Format-SqlLiteral {
    param(
        $Value,
        [Parameter(Mandatory = $true)][string]$TypeName
    )

    if ($null -eq $Value -or $Value -is [DBNull]) {
        return 'NULL'
    }

    $normalizedType = $TypeName.ToLowerInvariant()
    switch ($normalizedType) {
        { $_ -in @('nvarchar', 'nchar', 'ntext', 'sysname') } {
            return Format-SqlStringExpression -Value ([string]$Value) -Unicode $true
        }
        { $_ -in @('varchar', 'char', 'text') } {
            return Format-SqlStringExpression -Value ([string]$Value) -Unicode $false
        }
        'xml' {
            return 'CONVERT(xml, ' + (Format-SqlStringExpression -Value ([string]$Value) -Unicode $true) + ')'
        }
        { $_ -in @('tinyint', 'smallint', 'int', 'bigint') } {
            return [Convert]::ToString($Value, [System.Globalization.CultureInfo]::InvariantCulture)
        }
        { $_ -in @('decimal', 'numeric', 'money', 'smallmoney') } {
            return [Convert]::ToString($Value, [System.Globalization.CultureInfo]::InvariantCulture)
        }
        'float' {
            return [Convert]::ToDouble($Value, [System.Globalization.CultureInfo]::InvariantCulture).ToString('R', [System.Globalization.CultureInfo]::InvariantCulture)
        }
        'real' {
            return [Convert]::ToSingle($Value, [System.Globalization.CultureInfo]::InvariantCulture).ToString('R', [System.Globalization.CultureInfo]::InvariantCulture)
        }
        'bit' {
            if ([Convert]::ToBoolean($Value, [System.Globalization.CultureInfo]::InvariantCulture)) {
                return '1'
            }

            return '0'
        }
        'datetime' {
            return "CONVERT(datetime, '" + ([DateTime]$Value).ToString('yyyy-MM-ddTHH:mm:ss.fff', [System.Globalization.CultureInfo]::InvariantCulture) + "', 126)"
        }
        'smalldatetime' {
            return "CONVERT(smalldatetime, '" + ([DateTime]$Value).ToString('yyyy-MM-ddTHH:mm:ss', [System.Globalization.CultureInfo]::InvariantCulture) + "', 126)"
        }
        'datetime2' {
            return "CONVERT(datetime2, '" + ([DateTime]$Value).ToString('yyyy-MM-ddTHH:mm:ss.fffffff', [System.Globalization.CultureInfo]::InvariantCulture) + "', 126)"
        }
        'date' {
            return "CONVERT(date, '" + ([DateTime]$Value).ToString('yyyy-MM-dd', [System.Globalization.CultureInfo]::InvariantCulture) + "', 23)"
        }
        'time' {
            return "CONVERT(time, '" + ([TimeSpan]$Value).ToString('c', [System.Globalization.CultureInfo]::InvariantCulture) + "')"
        }
        'datetimeoffset' {
            return "CONVERT(datetimeoffset, '" + ([DateTimeOffset]$Value).ToString('o', [System.Globalization.CultureInfo]::InvariantCulture) + "', 127)"
        }
        'uniqueidentifier' {
            return "'" + ([Guid]$Value).ToString() + "'"
        }
        { $_ -in @('binary', 'varbinary', 'image') } {
            return '0x' + [BitConverter]::ToString([byte[]]$Value).Replace('-', '')
        }
        default {
            throw "Unsupported SQL type '$TypeName' encountered while exporting seed data."
        }
    }
}

function Write-TableData {
    param(
        [Parameter(Mandatory = $true)][System.Data.SqlClient.SqlConnection]$Connection,
        [Parameter(Mandatory = $true)][System.IO.StreamWriter]$Writer,
        [Parameter(Mandatory = $true)]$Table
    )

    $columns = @(Get-InsertableColumns -Connection $Connection -ObjectId $Table.ObjectId)
    if ($columns.Count -eq 0) {
        return
    }

    $quotedColumnNames = @($columns | ForEach-Object { Quote-Identifier $_.ColumnName })
    $primaryKeyColumnNames = @(Get-PrimaryKeyColumnNames -Connection $Connection -ObjectId $Table.ObjectId)
    $orderColumnNames = @(
        if ($primaryKeyColumnNames.Count -gt 0) {
            $primaryKeyColumnNames
        }
        else {
            $columns | ForEach-Object ColumnName
        }
    )
    $fullyQualifiedTableName = Get-FullyQualifiedTableName -SchemaName $Table.SchemaName -TableName $Table.TableName
    $selectSql = 'SELECT ' + ($quotedColumnNames -join ', ') + ' FROM ' + $fullyQualifiedTableName
    if ($orderColumnNames.Count -gt 0) {
        $selectSql += ' ORDER BY ' + (($orderColumnNames | ForEach-Object { Quote-Identifier $_ }) -join ', ')
    }

    $command = $Connection.CreateCommand()
    $command.CommandText = $selectSql
    $command.CommandTimeout = 120

    $reader = $command.ExecuteReader()
    try {
        if (-not $reader.HasRows) {
            return
        }

        $Writer.WriteLine("-- Table: $fullyQualifiedTableName")

        if ($columns.IsIdentity -contains $true) {
            $Writer.WriteLine("SET IDENTITY_INSERT $fullyQualifiedTableName ON;")
        }

        while ($reader.Read()) {
            $valueExpressions = for ($index = 0; $index -lt $columns.Count; $index++) {
                Format-SqlLiteral -Value $reader.GetValue($index) -TypeName $columns[$index].TypeName
            }

            $insertSql =
                'INSERT INTO ' + $fullyQualifiedTableName `
                + ' (' + ($quotedColumnNames -join ', ') + ')' `
                + ' VALUES (' + ($valueExpressions -join ', ') + ');'
            $Writer.WriteLine($insertSql)
        }

        if ($columns.IsIdentity -contains $true) {
            $Writer.WriteLine("SET IDENTITY_INSERT $fullyQualifiedTableName OFF;")
        }

        $Writer.WriteLine('GO')
        $Writer.WriteLine()
    }
    finally {
        $reader.Dispose()
    }
}

$resolvedOutputPath = [System.IO.Path]::GetFullPath($OutputPath)
$outputDirectory = Split-Path -Path $resolvedOutputPath -Parent
if ([string]::IsNullOrWhiteSpace($outputDirectory)) {
    throw "OutputPath must include a file name: $OutputPath"
}

New-Item -ItemType Directory -Force -Path $outputDirectory | Out-Null

$normalizedConnectionString = Normalize-ConnectionString -ConnectionString $ConnectionString
$connection = New-SqlConnection -ConnectionString $normalizedConnectionString
try {
    $connection.Open()

    $builder = [System.Data.SqlClient.SqlConnectionStringBuilder]::new($normalizedConnectionString)
    $databaseName = if ([string]::IsNullOrWhiteSpace($builder.InitialCatalog)) { 'current database' } else { $builder.InitialCatalog }
    $tables = @(Get-UserTables -Connection $connection)
    $foreignKeyEdges = @(Get-ForeignKeyEdges -Connection $connection)
    $orderedTables = @(Get-OrderedTables -Tables $tables -ForeignKeyEdges $foreignKeyEdges)

    $utf8NoBom = [System.Text.UTF8Encoding]::new($false)
    $writer = [System.IO.StreamWriter]::new($resolvedOutputPath, $false, $utf8NoBom)
    try {
        $writer.NewLine = "`n"
        $writer.WriteLine("-- Generated by Export-AdventureWorksLT2025SeedSql.ps1")
        $writer.WriteLine("-- Source database: $databaseName")
        $writer.WriteLine("-- Generated at (UTC): $(Get-Date -AsUTC -Format o)")
        $writer.WriteLine('SET NOCOUNT ON;')
        $writer.WriteLine('SET XACT_ABORT ON;')
        $writer.WriteLine()

        foreach ($table in $orderedTables) {
            $fullyQualifiedTableName = Get-FullyQualifiedTableName -SchemaName $table.SchemaName -TableName $table.TableName
            $writer.WriteLine("ALTER TABLE $fullyQualifiedTableName NOCHECK CONSTRAINT ALL;")
        }

        $writer.WriteLine('GO')
        $writer.WriteLine()

        foreach ($table in $orderedTables) {
            Write-TableData -Connection $connection -Writer $writer -Table $table
        }

        foreach ($table in ($orderedTables | Sort-Object SchemaName, TableName)) {
            $fullyQualifiedTableName = Get-FullyQualifiedTableName -SchemaName $table.SchemaName -TableName $table.TableName
            $writer.WriteLine("ALTER TABLE $fullyQualifiedTableName WITH CHECK CHECK CONSTRAINT ALL;")
        }

        $writer.WriteLine('GO')
    }
    finally {
        $writer.Dispose()
    }
}
finally {
    $connection.Dispose()
}

Write-Host "Wrote seed script to $resolvedOutputPath"
