using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using Sqloom.Tests;
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

        var projectSelection = resolver.ResolveProjectSelection(SqloomRepositoryPaths.GetTestAppProjectPath());

        Assert.Equal(
            SqloomRepositoryPaths.GetTestAppProjectPath(),
            projectSelection.RequestedTargetPath,
            StringComparer.OrdinalIgnoreCase);
        Assert.Equal(
            SqloomRepositoryPaths.GetTestAppProjectPath(),
            projectSelection.TargetProjectPath,
            StringComparer.OrdinalIgnoreCase);
        Assert.Equal(
            SqloomRepositoryPaths.GetTestAppIntegrationProjectPath(),
            projectSelection.IntegrationProjectPath,
            StringComparer.OrdinalIgnoreCase);
        Assert.False(projectSelection.UsesResolvedTargetProject);
        Assert.True(projectSelection.UsesCompanionIntegrationProject);
    }

    [Fact]
    public void ResolveProjectSelection_UsesProjectFromDirectoryTarget()
    {
        AppProjectResolver resolver = new();
        var targetDirectoryPath = SqloomRepositoryPaths.GetTestAppProjectDirectory();

        var projectSelection = resolver.ResolveProjectSelection(targetDirectoryPath);

        Assert.Equal(
            targetDirectoryPath,
            projectSelection.RequestedTargetPath,
            StringComparer.OrdinalIgnoreCase);
        Assert.Equal(
            SqloomRepositoryPaths.GetTestAppProjectPath(),
            projectSelection.TargetProjectPath,
            StringComparer.OrdinalIgnoreCase);
        Assert.Equal(
            SqloomRepositoryPaths.GetTestAppIntegrationProjectPath(),
            projectSelection.IntegrationProjectPath,
            StringComparer.OrdinalIgnoreCase);
        Assert.True(projectSelection.UsesResolvedTargetProject);
        Assert.True(projectSelection.UsesCompanionIntegrationProject);
    }

    [Fact]
    public void ResolveProjectSelection_UsesSqloomCapableProjectFromRepositoryRootDirectory()
    {
        AppProjectResolver resolver = new();
        var targetDirectoryPath = SqloomRepositoryPaths.GetRepositoryRoot();

        var projectSelection = resolver.ResolveProjectSelection(targetDirectoryPath);

        Assert.Equal(
            targetDirectoryPath,
            projectSelection.RequestedTargetPath,
            StringComparer.OrdinalIgnoreCase);
        Assert.Equal(
            SqloomRepositoryPaths.GetTestAppProjectPath(),
            projectSelection.TargetProjectPath,
            StringComparer.OrdinalIgnoreCase);
        Assert.Equal(
            SqloomRepositoryPaths.GetTestAppIntegrationProjectPath(),
            projectSelection.IntegrationProjectPath,
            StringComparer.OrdinalIgnoreCase);
        Assert.True(projectSelection.UsesResolvedTargetProject);
        Assert.True(projectSelection.UsesCompanionIntegrationProject);
    }

    [Fact]
    public void ResolveProjectSelection_UsesSqloomCapableProjectFromSolutionTarget()
    {
        AppProjectResolver resolver = new();
        var solutionPath = SqloomRepositoryPaths.GetSolutionPath();

        var projectSelection = resolver.ResolveProjectSelection(solutionPath);

        Assert.Equal(
            solutionPath,
            projectSelection.RequestedTargetPath,
            StringComparer.OrdinalIgnoreCase);
        Assert.Equal(
            SqloomRepositoryPaths.GetTestAppProjectPath(),
            projectSelection.TargetProjectPath,
            StringComparer.OrdinalIgnoreCase);
        Assert.Equal(
            SqloomRepositoryPaths.GetTestAppIntegrationProjectPath(),
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
            AppTargetPath = SqloomRepositoryPaths.GetTestAppProjectPath(),
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
    public void ResolveReplayIntegrations_DeduplicatesRepeatedProjectsFromSolutionFilter()
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
                SqloomRepositoryPaths.GetTestAppProjectPath(),
                SqloomRepositoryPaths.GetTestAppProjectPath());
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
                appName => Assert.Equal("Sqloom Test App", appName));
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
            AppTargetPath = SqloomRepositoryPaths.GetTestAppProjectPath(),
            NoBuild = true,
        };

        var assemblyPath = resolver.ResolveAssemblyPath(startupOptions);

        Assert.Equal(
            SqloomRepositoryPaths.GetExpectedTestAppIntegrationBuildOutputPath(),
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
