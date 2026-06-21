using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Sqloom.Core.Execution;
using Sqloom.TestApp.IntegrationTests;
using Xunit;

namespace Sqloom.Host.Tests;

/// <summary>
/// Exercises the Sqloom host when it resolves app integrations through project builds.
/// </summary>
[Collection("ConsoleHostRuntime")]
public sealed class HostProcessLoadingTests
{
    [Fact]
    [Trait("Category", "Integration")]
    public async Task DotNetRun_WithSqloomHostProjectAndSqloomTestAppProject_ReplaysProductCatalogWorkload()
    {
        var backendRoot = GetBackendRoot();
        const string hostProjectPath = @".\tools\Sqloom.Host\Sqloom.Host.csproj";
        const string targetProjectPath = @".\tools\Sqloom.TestApp\Sqloom.TestApp.csproj";

        var result = await RunDotNetAsync(
            backendRoot,
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
        var backendRoot = GetBackendRoot();
        const string hostProjectPath = @".\tools\Sqloom.Host\Sqloom.Host.csproj";
        const string targetProjectPath = @".\tools\Sqloom.TestApp\Sqloom.TestApp.csproj";

        var result = await RunDotNetAsync(
            backendRoot,
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

    private static string GetBackendRoot()
    {
        var repositoryRoot = RepositoryRootLocator.TryFind(AppContext.BaseDirectory)
            ?? throw new InvalidOperationException("Could not locate the repository root for Sqloom integration tests.");
        return Path.Combine(repositoryRoot, "backend");
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
