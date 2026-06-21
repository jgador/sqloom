namespace Sqloom.TestApp.IntegrationTests;

/// <summary>
/// Holds constants shared across the sample Sqloom replay harness.
/// </summary>
internal static class TestAppReplayConstants
{
    public const int CommandTimeoutSeconds = 180;
    public const string DatabaseNamePrefix = "SqloomTestApp";
    public const string MasterDatabaseName = "master";
    public const string SqlServerDacpacFileName = "AdventureWorksLT2025.dacpac";
    public const string SqlServerImage = "mcr.microsoft.com/mssql/server:2025-CU5-ubuntu-22.04";
    public const string SqlServerPassword = "Sqloom-Test-Sql-2026!";
}
