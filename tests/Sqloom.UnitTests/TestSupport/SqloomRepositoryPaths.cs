using System;
using System.IO;
using Sqloom.Core.Execution;

namespace Sqloom.Tests;

/// <summary>
/// Resolves stable standalone-repo paths for the Sqloom unit tests.
/// </summary>
internal static class SqloomRepositoryPaths
{
    public static string GetRepositoryRoot()
    {
        return RepositoryRootLocator.TryFind(AppContext.BaseDirectory)
            ?? throw new InvalidOperationException("Could not locate the repository root for Sqloom unit tests.");
    }

    public static string GetSolutionPath()
    {
        return Path.Combine(
            GetRepositoryRoot(),
            "Sqloom.sln");
    }

    public static string GetTestAppProjectPath()
    {
        return Path.Combine(
            GetRepositoryRoot(),
            "Sqloom.TestApp",
            "Sqloom.TestApp.csproj");
    }

    public static string GetTestAppProjectDirectory()
    {
        return Path.Combine(
            GetRepositoryRoot(),
            "Sqloom.TestApp");
    }

    public static string GetTestAppIntegrationProjectPath()
    {
        return Path.Combine(
            GetRepositoryRoot(),
            "Sqloom.TestApp.IntegrationTests",
            "Sqloom.TestApp.IntegrationTests.csproj");
    }

    public static string GetExpectedTestAppIntegrationBuildOutputPath()
    {
        return Path.Combine(
            GetRepositoryRoot(),
            "artifacts",
            "bin",
            "Sqloom.TestApp.IntegrationTests",
            "debug",
            "Sqloom.TestApp.IntegrationTests.dll");
    }

    public static string GetDefaultArtifactRoot()
    {
        return Path.Combine(
            GetRepositoryRoot(),
            "artifacts",
            "sqloom");
    }
}
