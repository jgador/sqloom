using System;
using System.IO;
using System.Threading.Tasks;
using Sqloom.Pipeline.Execution;
using Sqloom.Testing;

namespace Sqloom.Host.Tests;

/// <summary>
/// Resolves filesystem paths used by the sample Sqloom integration tests.
/// </summary>
internal static class SqloomTestAppPaths
{
    private static readonly Lazy<Task<ISqloomApplication>> _application = new(ResolveApplicationCoreAsync);

    public static string GetRepositoryRoot()
    {
        return RepositoryRootLocator.TryFind(AppContext.BaseDirectory)
            ?? throw new InvalidOperationException("Could not locate the repository root for Sqloom integration tests.");
    }

    public static string GetHarnessPath()
    {
        return Path.Combine(
            GetRepositoryRoot(),
            "tests",
            "Sqloom",
            "Sqloom.TestApp",
            "default",
            "Harness.cs");
    }

    public static string GetProjectPath()
    {
        return Path.Combine(
            GetRepositoryRoot(),
            "tests",
            "Sqloom.TestApp",
            "Sqloom.TestApp.csproj");
    }

    public static Task<ISqloomApplication> ResolveApplicationAsync()
    {
        return _application.Value;
    }

    private static Task<ISqloomApplication> ResolveApplicationCoreAsync()
    {
        return AppResolver.ResolveAsync(new HostStartupOptions
        {
            AppTargetPath = GetHarnessPath(),
            DotNetCommand = "dotnet",
        });
    }
}
