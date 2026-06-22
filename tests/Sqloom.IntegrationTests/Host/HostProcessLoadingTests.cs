using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Sqloom.TestApp.Harness;
using Xunit;

namespace Sqloom.Host.Tests;

/// <summary>
/// Exercises the Sqloom host when it resolves app harnesses through project builds.
/// </summary>
[Collection("ConsoleHostRuntime")]
public sealed class HostProcessLoadingTests
{
    [Fact]
    [Trait("Category", "Integration")]
    public async Task DotNetRun_WithSqloomHostProjectAndSqloomTestAppProject_ReplaysProductCatalogWorkload()
    {
        var repositoryRoot = SqloomTestAppPaths.GetRepositoryRoot();
        const string hostProjectPath = @".\src\Sqloom.Host\Sqloom.Host.csproj";
        const string targetProjectPath = @".\tests\Sqloom.TestApp.Harness\Sqloom.TestApp.Harness.csproj";

        var result = await RunDotNetAsync(
            repositoryRoot,
            [
                "run",
                "--project",
                hostProjectPath,
                "--",
                "replay",
                targetProjectPath,
                "--dotnet-command",
                "dotnet",
                "--target",
                TestAppProductCatalogScenario.OperationKey,
            ]);

        Assert.True(
            result.ExitCode == 0,
            FormatFailureMessage(result));
        Assert.Contains("Sqloom host", result.StandardOutput, StringComparison.Ordinal);
        Assert.Contains("App: Sqloom Test App", result.StandardOutput, StringComparison.Ordinal);
        Assert.Contains("Replay summary:", result.StandardOutput, StringComparison.Ordinal);
        Assert.Contains(
            $"{TestAppProductCatalogScenario.OperationKey}: status=replayed, http=200",
            result.StandardOutput,
            StringComparison.Ordinal);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task DotNetRun_WithSqloomHostProjectAndLeadingTargetPath_FailsWithoutStageVerb()
    {
        var repositoryRoot = SqloomTestAppPaths.GetRepositoryRoot();
        const string hostProjectPath = @".\src\Sqloom.Host\Sqloom.Host.csproj";
        const string targetProjectPath = @".\tests\Sqloom.TestApp.Harness\Sqloom.TestApp.Harness.csproj";

        var result = await RunDotNetAsync(
            repositoryRoot,
            [
                "run",
                "--project",
                hostProjectPath,
                "--",
                targetProjectPath,
                "--target",
                TestAppProductCatalogScenario.OperationKey,
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
