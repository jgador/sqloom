using System;
using System.IO;
using Sqloom.Core.Execution;

namespace Sqloom.TestApp.Harness;

/// <summary>
/// Resolves the optional SQL seed script path used for post-DACPAC sample replay bootstrap.
/// </summary>
internal static class TestAppReplaySqlServerSeedSqlPathResolver
{
    public static string? ResolvePathOrNull(ReplayLaunchOptions? launchOptions)
    {
        var seedSqlPath = launchOptions?.SqlServerSeedSqlPath;
        if (string.IsNullOrWhiteSpace(seedSqlPath))
        {
            return null;
        }

        var fullSeedSqlPath = Path.GetFullPath(seedSqlPath);
        if (!File.Exists(fullSeedSqlPath))
        {
            throw new ArgumentException(
                $"The Sqloom Test App replay SQL seed script '{fullSeedSqlPath}' does not exist.");
        }

        return fullSeedSqlPath;
    }
}
