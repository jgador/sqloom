using System;
using System.IO;
using Sqloom.Core.Execution;

namespace Sqloom.Host.Tests;

/// <summary>
/// Resolves filesystem paths used by the sample Sqloom integration tests.
/// </summary>
internal static class SqloomTestAppPaths
{
    public static string GetBackendRoot()
    {
        var repositoryRoot = RepositoryRootLocator.TryFind(AppContext.BaseDirectory)
            ?? throw new InvalidOperationException("Could not locate the repository root for Sqloom integration tests.");
        return Path.Combine(repositoryRoot, "backend");
    }

    public static string GetProjectPath()
    {
        return Path.Combine(
            GetBackendRoot(),
            "tools",
            "Sqloom.TestApp",
            "Sqloom.TestApp.csproj");
    }

    public static string GetSqlServerDacpacPath()
    {
        return Path.Combine(
            GetBackendRoot(),
            "tools",
            "Sqloom.TestApp.IntegrationTests",
            "AdventureWorksLT2025.dacpac");
    }

    public static string GetSeedExportScriptPath()
    {
        return Path.Combine(
            GetBackendRoot(),
            "tools",
            "Sqloom.TestApp.IntegrationTests",
            "Export-AdventureWorksLT2025SeedSql.ps1");
    }

    public static string GetSqlServerSeedScriptPath()
    {
        return Path.Combine(
            GetBackendRoot(),
            "tools",
            "Sqloom.TestApp.IntegrationTests",
            "AdventureWorksLT2025.seed.sql");
    }

    public static string GetSqlServerSchemaPath()
    {
        return Path.Combine(
            GetBackendRoot(),
            "tools",
            "Sqloom.TestApp.IntegrationTests",
            "AdventureWorksLT2025.schema.sql");
    }
}
