using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Xunit;

namespace Sqloom.Host.Tests;

/// <summary>
/// Exercises the Sqloom host when it resolves a consumer-style C# file harness.
/// </summary>
[Collection("ConsoleHostRuntime")]
public sealed class HostProcessTests
{
    [Fact]
    [Trait("Category", "Integration")]
    public async Task WithHostAndConsumerHarness_ReportsMissingQueryData()
    {
        var repositoryRoot = SqloomTestAppPaths.GetRepositoryRoot();
        const string hostProjectPath = @".\src\Sqloom.Host\Sqloom.Host.csproj";
        const string targetFilePath = @".\tests\Sqloom\Sqloom.TestApp\default\Harness.cs";

        var result = await RunDotNetAsync(
            repositoryRoot,
            [
                "run",
                "--project",
                hostProjectPath,
                "--",
                "replay",
                targetFilePath,
                "--dotnet-command",
                "dotnet",
                "--target",
                SampleCatalogReplayScenario.OperationKey,
                "--replay-data-agent",
                "off",
            ]);

        Assert.True(
            result.ExitCode == 1,
            FormatFailureMessage(result));
        var output = result.StandardOutput + result.StandardError;
        Assert.Contains("Sqloom host", result.StandardOutput, StringComparison.Ordinal);
        Assert.Contains("App: Sqloom Test App", result.StandardOutput, StringComparison.Ordinal);
        Assert.Contains(
            "missing required query parameter 'categoryId'",
            output,
            StringComparison.Ordinal);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task WithLeadingTargetPath_FailsWithoutStageVerb()
    {
        var repositoryRoot = SqloomTestAppPaths.GetRepositoryRoot();
        const string hostProjectPath = @".\src\Sqloom.Host\Sqloom.Host.csproj";
        const string targetFilePath = @".\tests\Sqloom\Sqloom.TestApp\default\Harness.cs";

        var result = await RunDotNetAsync(
            repositoryRoot,
            [
                "run",
                "--project",
                hostProjectPath,
                "--",
                targetFilePath,
                "--target",
                SampleCatalogReplayScenario.OperationKey,
            ]);

        Assert.True(
            result.ExitCode != 0,
            FormatFailureMessage(result));
        Assert.Contains("explicit stage verb", result.StandardError, StringComparison.OrdinalIgnoreCase);
    }

    private static async Task<DotNetCommandResult> RunDotNetAsync(
        string workingDirectory,
        string[] arguments)
    {
        ProcessStartInfo startInfo = new("dotnet")
        {
            CreateNoWindow = true,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false,
            WorkingDirectory = workingDirectory,
        };

        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using Process process = new()
        {
            StartInfo = startInfo,
        };

        if (!process.Start())
        {
            throw new InvalidOperationException("Failed to start dotnet for the Sqloom host process test.");
        }

        var standardOutputTask = process.StandardOutput.ReadToEndAsync();
        var standardErrorTask = process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync().ConfigureAwait(false);
        await Task.WhenAll(
                standardOutputTask,
                standardErrorTask)
            .ConfigureAwait(false);

        return new DotNetCommandResult(
            process.ExitCode,
            standardOutputTask.Result,
            standardErrorTask.Result);
    }

    private static string FormatFailureMessage(DotNetCommandResult result)
    {
        return
            $"ExitCode: {result.ExitCode}{Environment.NewLine}" +
            $"StdOut:{Environment.NewLine}{result.StandardOutput}{Environment.NewLine}" +
            $"StdErr:{Environment.NewLine}{result.StandardError}";
    }

    private sealed record DotNetCommandResult(
        int ExitCode,
        string StandardOutput,
        string StandardError);
}
