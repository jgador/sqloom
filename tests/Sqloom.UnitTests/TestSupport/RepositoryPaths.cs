using System;
using System.IO;
using Sqloom.Core.Execution;

namespace Sqloom.Tests;

/// <summary>
/// Resolves stable standalone-repo paths for the Sqloom unit tests.
/// </summary>
internal static class RepositoryPaths
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
            "Sqloom.slnx");
    }

    public static string GetTestingProjectPath()
    {
        return Path.Combine(
            GetRepositoryRoot(),
            "src",
            "Sqloom.Testing",
            "Sqloom.Testing.csproj");
    }

    public static string GetTestAppProjectPath()
    {
        return Path.Combine(
            GetRepositoryRoot(),
            "tests",
            "Sqloom.TestApp",
            "Sqloom.TestApp.csproj");
    }

    public static string GetTestAppProjectDirectory()
    {
        return Path.Combine(
            GetRepositoryRoot(),
            "tests",
            "Sqloom.TestApp");
    }

    public static string GetTestAppOpenApiPath()
    {
        return Path.Combine(
            GetTestAppProjectDirectory(),
            "openapi.json");
    }

    public static string GetSampleApplicationProjectPath()
    {
        return Path.Combine(
            GetRepositoryRoot(),
            "tests",
            "Sqloom.TestApp.Harness",
            "Sqloom.TestApp.Harness.csproj");
    }

    public static string GetSampleApplicationDacpacPath()
    {
        return Path.Combine(
            GetRepositoryRoot(),
            "tests",
            "Sqloom.TestApp.Harness",
            "AdventureWorksLT2025.dacpac");
    }

    public static string GetExpectedSampleApplicationBuildOutputPath()
    {
        return Path.Combine(
            GetRepositoryRoot(),
            "artifacts",
            "bin",
            "Sqloom.TestApp.Harness",
            "debug",
            "Sqloom.TestApp.Harness.dll");
    }

    public static string GetDefaultArtifactRoot()
    {
        return Path.Combine(
            GetRepositoryRoot(),
            "artifacts",
            "sqloom");
    }
}
