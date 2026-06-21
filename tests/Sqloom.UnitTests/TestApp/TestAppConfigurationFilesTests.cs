using System;
using System.IO;
using System.Text.Json;
using Sqloom.Tests;
using Xunit;

namespace Sqloom.TestApp.Tests;

/// <summary>
/// Locks down the checked-in configuration files for the sample Sqloom test app.
/// </summary>
public sealed class TestAppConfigurationFilesTests
{
    [Fact]
    public void AppSettingsJson_ConfiguresLocalAdventureWorksConnection()
    {
        var projectDirectory = GetProjectDirectory();
        var appSettingsPath = Path.Combine(projectDirectory, "appsettings.json");

        using var document = JsonDocument.Parse(File.ReadAllText(appSettingsPath));
        var defaultConnection = document.RootElement
            .GetProperty("ConnectionStrings")
            .GetProperty("DefaultConnection")
            .GetString();

        Assert.False(string.IsNullOrWhiteSpace(defaultConnection));
        Assert.Contains("AdventureWorksLT2025", defaultConnection!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AppSettingsEnvironmentFiles_Exist()
    {
        var projectDirectory = GetProjectDirectory();

        Assert.True(File.Exists(Path.Combine(projectDirectory, "appsettings.Development.json")));
        Assert.True(File.Exists(Path.Combine(projectDirectory, "appsettings.Production.json")));
    }

    [Fact]
    public void LaunchSettings_UsesDevelopmentEnvironmentByDefault()
    {
        var projectDirectory = GetProjectDirectory();
        var launchSettingsPath = Path.Combine(
            projectDirectory,
            "Properties",
            "launchSettings.json");

        using var document = JsonDocument.Parse(File.ReadAllText(launchSettingsPath));
        var environmentName = document.RootElement
            .GetProperty("profiles")
            .GetProperty("Sqloom.TestApp")
            .GetProperty("environmentVariables")
            .GetProperty("ASPNETCORE_ENVIRONMENT")
            .GetString();

        Assert.Equal("Development", environmentName);
    }

    private static string GetProjectDirectory()
    {
        return SqloomRepositoryPaths.GetTestAppProjectDirectory();
    }
}
