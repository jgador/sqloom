using System;
using System.IO;
using Xunit;

namespace Sqloom.Host.Tests;

/// <summary>
/// Exercises Sqloom observe argument parsing.
/// </summary>
public sealed class ObserveArgumentParserTests
{
    [Fact]
    public void GetQueryStoreConnectionString_UsesExplicitConnectionStringSwitch()
    {
        ObserveArgumentParser parser = new();
        const string expectedConnectionString = "Server=localhost;Database=Sqloom;Trusted_Connection=True;";

        var connectionString = parser.GetQueryStoreConnectionString(
            [
                "--read-only-connection-string",
                expectedConnectionString,
            ]);

        Assert.Equal(expectedConnectionString, connectionString);
    }

    [Fact]
    public void GetQueryStoreConnectionString_ReturnsNullWhenExplicitConnectionStringIsMissing()
    {
        ObserveArgumentParser parser = new();

        Assert.Null(parser.GetQueryStoreConnectionString([]));
    }

    [Fact]
    public void Parse_UsesJsonOutputFileOverride()
    {
        ObserveArgumentParser parser = new();
        var currentDirectory = CreateTempDir();
        var jsonOutputPath = Path.Combine(currentDirectory, "query-store.json");

        var arguments = parser.Parse(
            [
                "--json-output-file",
                jsonOutputPath,
            ],
            null,
            "Server=localhost;Database=Sqloom;Trusted_Connection=True;",
            currentDirectory);

        Assert.Equal(jsonOutputPath, arguments.JsonOutputPathOverride, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public void Parse_UsesExpandedDefaultPlanWindow()
    {
        ObserveArgumentParser parser = new();
        var currentDirectory = CreateTempDir();

        var arguments = parser.Parse(
            [],
            null,
            "Server=localhost;Database=Sqloom;Trusted_Connection=True;",
            currentDirectory);

        Assert.Equal(TimeSpan.FromHours(24), arguments.ObservationOptions.LookbackWindow);
        Assert.Equal(100, arguments.ObservationOptions.MaxPlans);
        Assert.Equal(10, arguments.ObservationOptions.MaxWaits);
        Assert.Equal(30, arguments.ObservationOptions.CommandTimeoutSeconds);
    }

    [Fact]
    public void Parse_RejectsLegacyJsonOutSwitch()
    {
        ObserveArgumentParser parser = new();
        var currentDirectory = CreateTempDir();

        var exception = Assert.Throws<ArgumentException>(
            () => parser.Parse(
                [
                    "--json-out",
                    Path.Combine(currentDirectory, "query-store.json"),
                ],
                null,
                "Server=localhost;Database=Sqloom;Trusted_Connection=True;",
                currentDirectory));

        Assert.Contains("Unsupported switch", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("--json-out", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static string CreateTempDir()
    {
        var directory = Path.Combine(
            Path.GetTempPath(),
            "sqloom-host-command-line-tests",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        return directory;
    }
}
