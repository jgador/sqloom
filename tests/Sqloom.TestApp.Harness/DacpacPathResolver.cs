using System;
using System.IO;
using Sqloom.Core.Execution;

namespace Sqloom.TestApp.Harness;

/// <summary>
/// Resolves the sample app DACPAC path required for SQL Server-backed replay bootstrap.
/// </summary>
internal static class DacpacPathResolver
{
    public static string ResolveRequiredPath(ReplayLaunchOptions? launchOptions)
    {
        var dacpacPath = launchOptions?.DacpacPath;
        if (string.IsNullOrWhiteSpace(dacpacPath))
        {
            throw new ArgumentException(
                "Sqloom Test App replay requires a SQL Server DACPAC path when SQL Server bootstrap is requested. Pass --sqlserver-dacpac-file <path> or set ReplayRunnerOptions.ReplayLaunchOptions.DacpacPath when calling the replay runner directly.");
        }

        var fullDacpacPath = Path.GetFullPath(dacpacPath);
        if (!File.Exists(fullDacpacPath))
        {
            throw new ArgumentException(
                $"The Sqloom Test App replay SQL Server DACPAC '{fullDacpacPath}' does not exist.");
        }

        return fullDacpacPath;
    }
}
