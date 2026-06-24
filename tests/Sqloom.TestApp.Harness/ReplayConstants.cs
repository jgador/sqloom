namespace Sqloom.TestApp.Harness;

/// <summary>
/// Holds constants shared across the sample Sqloom replay harness.
/// </summary>
internal static class ReplayConstants
{
    public const int CommandTimeoutSeconds = 180;
    public const string DbNamePrefix = "SqloomTestApp";
    public const string MasterDatabaseName = "master";
    public const string DacpacFileName = "AdventureWorksLT2025.dacpac";
    public const string SeedSqlFileName = "AdventureWorksLT2025.seed.sql";
    public const string SqlServerImage = "mcr.microsoft.com/mssql/server:2025-CU5-ubuntu-22.04";
    public const string SqlServerPassword = "Sqloom-Test-Sql-2026!";
}
