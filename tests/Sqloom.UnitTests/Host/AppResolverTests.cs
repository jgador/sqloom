using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using Sqloom.Core.Execution;
using Xunit;

namespace Sqloom.Host.Tests;

/// <summary>
/// Exercises Sqloom app resolver.
/// </summary>
public sealed class AppResolverTests
{
    [Fact]
    public void ResolveProjectSelection_UsesCompanionIntegrationProjectForSqloomTestApp()
    {
        AppProjectResolver resolver = new();

        var projectSelection = resolver.ResolveProjectSelection(GetTestAppProjectPath());

        Assert.Equal(
            GetTestAppProjectPath(),
            projectSelection.RequestedTargetPath,
            StringComparer.OrdinalIgnoreCase);
        Assert.Equal(
            GetTestAppProjectPath(),
            projectSelection.TargetProjectPath,
            StringComparer.OrdinalIgnoreCase);
        Assert.Equal(
            GetTestAppIntegrationProjectPath(),
            projectSelection.IntegrationProjectPath,
            StringComparer.OrdinalIgnoreCase);
        Assert.False(projectSelection.UsesResolvedTargetProject);
        Assert.True(projectSelection.UsesCompanionIntegrationProject);
    }

    [Fact]
    public void ResolveProjectSelection_UsesCompanionIntegrationProjectWhenDeclared()
    {
        AppProjectResolver resolver = new();

        var projectSelection = resolver.ResolveProjectSelection(GetTalioApiProjectPath());

        Assert.Equal(
            GetTalioApiProjectPath(),
            projectSelection.RequestedTargetPath,
            StringComparer.OrdinalIgnoreCase);
        Assert.Equal(
            GetTalioApiProjectPath(),
            projectSelection.TargetProjectPath,
            StringComparer.OrdinalIgnoreCase);
        Assert.Equal(
            GetTalioSqloomProjectPath(),
            projectSelection.IntegrationProjectPath,
            StringComparer.OrdinalIgnoreCase);
        Assert.False(projectSelection.UsesResolvedTargetProject);
        Assert.True(projectSelection.UsesCompanionIntegrationProject);
    }

    [Fact]
    public void ResolveProjectSelection_UsesProjectFromDirectoryTarget()
    {
        AppProjectResolver resolver = new();
        var targetDirectoryPath = GetTalioApiDirectoryPath();

        var projectSelection = resolver.ResolveProjectSelection(targetDirectoryPath);

        Assert.Equal(
            targetDirectoryPath,
            projectSelection.RequestedTargetPath,
            StringComparer.OrdinalIgnoreCase);
        Assert.Equal(
            GetTalioApiProjectPath(),
            projectSelection.TargetProjectPath,
            StringComparer.OrdinalIgnoreCase);
        Assert.Equal(
            GetTalioSqloomProjectPath(),
            projectSelection.IntegrationProjectPath,
            StringComparer.OrdinalIgnoreCase);
        Assert.True(projectSelection.UsesResolvedTargetProject);
        Assert.True(projectSelection.UsesCompanionIntegrationProject);
    }

    [Fact]
    public void ResolveProjectSelection_UsesSqloomCapableProjectFromBackendDirectory()
    {
        AppProjectResolver resolver = new();
        var targetDirectoryPath = GetBackendDirectoryPath();

        var projectSelection = resolver.ResolveProjectSelection(targetDirectoryPath);

        Assert.Equal(
            targetDirectoryPath,
            projectSelection.RequestedTargetPath,
            StringComparer.OrdinalIgnoreCase);
        Assert.Equal(
            GetTalioApiProjectPath(),
            projectSelection.TargetProjectPath,
            StringComparer.OrdinalIgnoreCase);
        Assert.Equal(
            GetTalioSqloomProjectPath(),
            projectSelection.IntegrationProjectPath,
            StringComparer.OrdinalIgnoreCase);
        Assert.True(projectSelection.UsesResolvedTargetProject);
        Assert.True(projectSelection.UsesCompanionIntegrationProject);
    }

    [Fact]
    public void ResolveProjectSelection_UsesSqloomCapableProjectFromSolutionTarget()
    {
        AppProjectResolver resolver = new();
        var solutionPath = GetBackendSolutionPath();

        var projectSelection = resolver.ResolveProjectSelection(solutionPath);

        Assert.Equal(
            solutionPath,
            projectSelection.RequestedTargetPath,
            StringComparer.OrdinalIgnoreCase);
        Assert.Equal(
            GetTalioApiProjectPath(),
            projectSelection.TargetProjectPath,
            StringComparer.OrdinalIgnoreCase);
        Assert.Equal(
            GetTalioSqloomProjectPath(),
            projectSelection.IntegrationProjectPath,
            StringComparer.OrdinalIgnoreCase);
        Assert.True(projectSelection.UsesResolvedTargetProject);
        Assert.True(projectSelection.UsesCompanionIntegrationProject);
    }

    [Fact]
    public void ResolveProjectSelection_ThrowsWhenDirectoryContainsMultipleSqloomCapableProjects()
    {
        AppProjectResolver resolver = new();
        var directoryPath = Path.Combine(
            Path.GetTempPath(),
            "sqloom-tests",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directoryPath);

        try
        {
            var projectAPath = Path.Combine(directoryPath, "AppA.csproj");
            var projectBPath = Path.Combine(directoryPath, "AppB.csproj");
            File.WriteAllText(
                projectAPath,
                """
                <Project Sdk="Microsoft.NET.Sdk">
                  <PropertyGroup>
                    <TargetFramework>net10.0</TargetFramework>
                    <SqloomAppIntegrationType>Example.AppAIntegration</SqloomAppIntegrationType>
                  </PropertyGroup>
                </Project>
                """);
            File.WriteAllText(
                projectBPath,
                """
                <Project Sdk="Microsoft.NET.Sdk">
                  <PropertyGroup>
                    <TargetFramework>net10.0</TargetFramework>
                    <SqloomAppIntegrationType>Example.AppBIntegration</SqloomAppIntegrationType>
                  </PropertyGroup>
                </Project>
                """);

            var exception = Assert.Throws<AppResolutionException>(
                () => resolver.ResolveProjectSelection(directoryPath));

            Assert.Contains("multiple distinct app integrations", exception.Message, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("AppA.csproj", exception.Message, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("AppB.csproj", exception.Message, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            if (Directory.Exists(directoryPath))
            {
                Directory.Delete(
                    directoryPath,
                    recursive: true);
            }
        }
    }

    [Fact]
    public void Resolve_LoadsAppIntegrationFromExplicitProjectPathWithoutBuild()
    {
        AppResolver resolver = new();
        HostStartupOptions startupOptions = new()
        {
            AppTargetPath = GetTestAppProjectPath(),
            NoBuild = true,
        };

        var appIntegration = resolver.Resolve(startupOptions);

        Assert.Equal("Sqloom Test App", appIntegration.AppName);
        Assert.Equal("Sqloom.TestApp.IntegrationTests.TestAppIntegration", appIntegration.GetType().FullName);
    }

    [Fact]
    public void Resolve_ThrowsWhenProjectPathIsMissing()
    {
        AppResolver resolver = new();
        HostStartupOptions startupOptions = new()
        {
            AppTargetPath = Path.Combine(
                Path.GetTempPath(),
                "sqloom-tests",
                Guid.NewGuid().ToString("N"),
                "MissingApp.csproj"),
        };

        var exception = Assert.Throws<AppResolutionException>(
            () => resolver.Resolve(startupOptions));

        Assert.Contains("does not exist", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Resolve_ThrowsWhenTargetPathIsMissing()
    {
        AppResolver resolver = new();

        var exception = Assert.Throws<AppResolutionException>(
            () => resolver.Resolve(new HostStartupOptions()));

        Assert.Contains("requires an explicit target path", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ResolveReplayIntegrations_LoadsDistinctAppsFromSolutionFilter()
    {
        AppResolver resolver = new();
        var tempDirectoryPath = Path.Combine(
            Path.GetTempPath(),
            "sqloom-tests",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDirectoryPath);

        try
        {
            var solutionFilterPath = WriteSolutionFilter(
                tempDirectoryPath,
                GetTestAppProjectPath(),
                GetTalioApiProjectPath());
            HostStartupOptions startupOptions = new()
            {
                AppTargetPath = solutionFilterPath,
                NoBuild = true,
            };

            var appIntegrations = resolver.ResolveReplayIntegrations(startupOptions);
            var appNames = appIntegrations
                .Select(static appIntegration => appIntegration.AppName)
                .ToArray();

            Assert.Collection(
                appNames,
                appName => Assert.Equal("Sqloom Test App", appName),
                appName => Assert.Equal("Talio", appName));
        }
        finally
        {
            if (Directory.Exists(tempDirectoryPath))
            {
                Directory.Delete(
                    tempDirectoryPath,
                    recursive: true);
            }
        }
    }

    [Fact]
    public void ResolveAssemblyPath_WithProjectPathWithoutBuild_ReturnsBuildOutputPath()
    {
        AppResolver resolver = new();
        HostStartupOptions startupOptions = new()
        {
            AppTargetPath = GetTestAppProjectPath(),
            NoBuild = true,
        };

        var assemblyPath = resolver.ResolveAssemblyPath(startupOptions);

        Assert.Equal(
            GetExpectedTestAppIntegrationBuildOutputPath(),
            assemblyPath,
            StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public void ResolveAssemblyPath_ThrowsWhenTargetPathIsMissing()
    {
        AppResolver resolver = new();

        var exception = Assert.Throws<AppResolutionException>(
            () => resolver.ResolveAssemblyPath(new HostStartupOptions()));

        Assert.Contains("requires an explicit target path", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static string GetTestAppProjectPath()
    {
        var repositoryRoot = RepositoryRootLocator.TryFind(AppContext.BaseDirectory)
            ?? throw new InvalidOperationException("Could not locate the repository root for Sqloom unit tests.");
        return Path.Combine(
            repositoryRoot,
            "backend",
            "tools",
            "Sqloom.TestApp",
            "Sqloom.TestApp.csproj");
    }

    private static string GetExpectedTestAppIntegrationBuildOutputPath()
    {
        var repositoryRoot = RepositoryRootLocator.TryFind(AppContext.BaseDirectory)
            ?? throw new InvalidOperationException("Could not locate the repository root for Sqloom unit tests.");
        return Path.Combine(
            repositoryRoot,
            "backend",
            "artifacts",
            "bin",
            "Sqloom.TestApp.IntegrationTests",
            "debug",
            "Sqloom.TestApp.IntegrationTests.dll");
    }

    private static string GetTestAppIntegrationProjectPath()
    {
        var repositoryRoot = RepositoryRootLocator.TryFind(AppContext.BaseDirectory)
            ?? throw new InvalidOperationException("Could not locate the repository root for Sqloom unit tests.");
        return Path.Combine(
            repositoryRoot,
            "backend",
            "tools",
            "Sqloom.TestApp.IntegrationTests",
            "Sqloom.TestApp.IntegrationTests.csproj");
    }

    private static string GetTalioApiProjectPath()
    {
        var repositoryRoot = RepositoryRootLocator.TryFind(AppContext.BaseDirectory)
            ?? throw new InvalidOperationException("Could not locate the repository root for Sqloom unit tests.");
        return Path.Combine(
            repositoryRoot,
            "backend",
            "src",
            "Talio.Api",
            "Talio.Api.csproj");
    }

    private static string GetTalioApiDirectoryPath()
    {
        var repositoryRoot = RepositoryRootLocator.TryFind(AppContext.BaseDirectory)
            ?? throw new InvalidOperationException("Could not locate the repository root for Sqloom unit tests.");
        return Path.Combine(
            repositoryRoot,
            "backend",
            "src",
            "Talio.Api");
    }

    private static string GetBackendDirectoryPath()
    {
        var repositoryRoot = RepositoryRootLocator.TryFind(AppContext.BaseDirectory)
            ?? throw new InvalidOperationException("Could not locate the repository root for Sqloom unit tests.");
        return Path.Combine(repositoryRoot, "backend");
    }

    private static string GetBackendSolutionPath()
    {
        var repositoryRoot = RepositoryRootLocator.TryFind(AppContext.BaseDirectory)
            ?? throw new InvalidOperationException("Could not locate the repository root for Sqloom unit tests.");
        return Path.Combine(
            repositoryRoot,
            "backend",
            "Talio.sln");
    }

    private static string GetTalioSqloomProjectPath()
    {
        var repositoryRoot = RepositoryRootLocator.TryFind(AppContext.BaseDirectory)
            ?? throw new InvalidOperationException("Could not locate the repository root for Sqloom unit tests.");
        return Path.Combine(
            repositoryRoot,
            "backend",
            "tests",
            "Talio.Sqloom",
            "Talio.Sqloom.csproj");
    }

    private static string WriteSolutionFilter(string directoryPath, params string[] projectPaths)
    {
        var solutionFilterPath = Path.Combine(directoryPath, "sqloom-targets.slnf");
        var document = JsonSerializer.Serialize(
            new
            {
                solution = new
                {
                    projects = projectPaths,
                },
            });
        File.WriteAllText(
            solutionFilterPath,
            document);
        return solutionFilterPath;
    }
}
