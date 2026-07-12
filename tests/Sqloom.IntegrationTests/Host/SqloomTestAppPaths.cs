using System;
using System.IO;
using Sqloom.Core.Execution;

namespace Sqloom.Host.Tests;

/// <summary>
/// Resolves filesystem paths used by the sample Sqloom integration tests.
/// </summary>
internal static class SqloomTestAppPaths
{
    public static string GetRepositoryRoot()
    {
        return RepositoryRootLocator.TryFind(AppContext.BaseDirectory)
            ?? throw new InvalidOperationException("Could not locate the repository root for Sqloom integration tests.");
    }

    public static string GetProjectPath()
    {
        return Path.Combine(
            GetRepositoryRoot(),
            "tests",
            "Sqloom.TestApp.Harness",
            "Sqloom.TestApp.Harness.csproj");
    }

    public static string GetOpenApiPath()
    {
        return Path.Combine(
            GetRepositoryRoot(),
            "tests",
            "Sqloom.TestApp",
            "openapi.json");
    }
}
