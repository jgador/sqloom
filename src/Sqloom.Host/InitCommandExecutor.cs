using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Sqloom.Host;

/// <summary>
/// Scaffolds the embedded Sqloom harness skill bundle into an agent-specific repository folder.
/// </summary>
internal sealed class InitCommandExecutor
{
    private const string DefaultAgent = "codex";
    private const string ResourcePrefix = "Sqloom.Host.InitAssets/skills/sqloom/";

    private static readonly InitAgentTarget[] AgentTargets =
    [
        new("codex", ".agents/skills/sqloom"),
        new("claude", ".claude/skills/sqloom"),
        new("copilot", ".github/skills/sqloom"),
    ];

    private readonly Assembly _assembly;

    public InitCommandExecutor()
        : this(typeof(InitCommandExecutor).Assembly)
    {
    }

    internal InitCommandExecutor(Assembly assembly)
    {
        _assembly = assembly ?? throw new ArgumentNullException(nameof(assembly));
    }

    public InitResult Execute(
        string[] args,
        string currentDirectory)
    {
        ArgumentNullException.ThrowIfNull(args);
        ArgumentException.ThrowIfNullOrWhiteSpace(currentDirectory);

        var repositoryRoot = Path.GetFullPath(currentDirectory);
        if (!HasCurrentDirectoryGitMarker(repositoryRoot))
        {
            throw new ArgumentException(
                "Current directory must be a Git repository root containing a .git directory or file. Run sqloom init from the repository root.");
        }

        var options = Parse(args);
        var targets = ResolveTargets(options.AgentSelection);
        var assets = LoadAssets(_assembly);
        var files = new List<InitFileResult>(targets.Count * assets.Count);

        foreach (var target in targets)
        {
            foreach (var asset in assets)
            {
                var displayPath = $"{target.BundleRoot}/{asset.RelativePath}";
                var outputPath = CombineRelative(repositoryRoot, displayPath);
                var status = WriteAsset(
                    displayPath,
                    outputPath,
                    asset.Content,
                    options.Overwrite);
                files.Add(new InitFileResult(target.Agent, displayPath, status));
            }
        }

        return new InitResult(
            options.AgentSelection,
            repositoryRoot,
            files);
    }

    private static InitCommandOptions Parse(string[] args)
    {
        var agentSelection = DefaultAgent;
        var overwrite = false;
        var agentSet = false;

        for (var index = 0; index < args.Length; index++)
        {
            var argument = args[index];
            if (index == 0 && string.Equals(argument, "init", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (string.Equals(argument, "--overwrite", StringComparison.OrdinalIgnoreCase))
            {
                overwrite = true;
                continue;
            }

            if (string.Equals(argument, "--agent", StringComparison.OrdinalIgnoreCase))
            {
                if (agentSet)
                {
                    throw new ArgumentException(
                        "Sqloom received multiple values for --agent. Keep only one agent selection.");
                }

                if (index + 1 >= args.Length
                    || CommandArgumentSupport.IsSwitch(args[index + 1])
                    || string.IsNullOrWhiteSpace(args[index + 1]))
                {
                    throw new ArgumentException(
                        $"Missing required value for --agent. Supported values: {SupportedAgentsText()}.");
                }

                agentSelection = args[++index].ToLowerInvariant();
                if (!IsSupportedAgent(agentSelection))
                {
                    throw new ArgumentException(
                        $"Unknown agent '{agentSelection}'. Supported values: {SupportedAgentsText()}.");
                }

                agentSet = true;
                continue;
            }

            if (CommandArgumentSupport.IsSwitch(argument))
            {
                throw new ArgumentException(
                    $"Unsupported switch '{argument}' for 'init'. Use sqloom init [--agent codex|claude|copilot|all] [--overwrite].");
            }

            throw new ArgumentException(
                $"Unexpected argument '{argument}' for 'init'. Use --agent codex, --agent claude, --agent copilot, or --agent all.");
        }

        return new InitCommandOptions(
            agentSelection,
            overwrite);
    }

    private static bool HasCurrentDirectoryGitMarker(string repositoryRoot)
    {
        var gitPath = Path.Combine(repositoryRoot, ".git");
        return Directory.Exists(gitPath) || File.Exists(gitPath);
    }

    private static IReadOnlyList<InitAgentTarget> ResolveTargets(string agentSelection)
    {
        if (string.Equals(agentSelection, "all", StringComparison.Ordinal))
        {
            return AgentTargets;
        }

        foreach (var target in AgentTargets)
        {
            if (string.Equals(target.Agent, agentSelection, StringComparison.Ordinal))
            {
                return [target];
            }
        }

        throw new ArgumentException(
            $"Unknown agent '{agentSelection}'. Supported values: {SupportedAgentsText()}.");
    }

    private static IReadOnlyList<InitAsset> LoadAssets(Assembly assembly)
    {
        var assets = new List<InitAsset>();
        foreach (var resourceName in assembly
                     .GetManifestResourceNames()
                     .OrderBy(resourceName => resourceName, StringComparer.Ordinal))
        {
            if (!resourceName.StartsWith(ResourcePrefix, StringComparison.Ordinal))
            {
                continue;
            }

            var relativePath = resourceName[ResourcePrefix.Length..].Replace('\\', '/');
            if (string.IsNullOrWhiteSpace(relativePath))
            {
                continue;
            }

            using var stream = assembly.GetManifestResourceStream(resourceName)
                ?? throw new InvalidOperationException($"Embedded init asset '{resourceName}' could not be opened.");
            using var buffer = new MemoryStream();
            stream.CopyTo(buffer);
            assets.Add(new InitAsset(relativePath, buffer.ToArray()));
        }

        if (assets.Count == 0)
        {
            throw new InvalidOperationException("No embedded Sqloom init assets were found.");
        }

        return assets;
    }

    private static InitFileStatus WriteAsset(
        string displayPath,
        string outputPath,
        byte[] content,
        bool overwrite)
    {
        if (File.Exists(outputPath))
        {
            var existing = File.ReadAllBytes(outputPath);
            if (existing.SequenceEqual(content))
            {
                return InitFileStatus.Unchanged;
            }

            if (!overwrite)
            {
                throw new ArgumentException(
                    $"Refusing to overwrite existing file '{displayPath}'. Rerun with --overwrite to replace scaffolded Sqloom harness skill files.");
            }

            File.WriteAllBytes(outputPath, content);
            return InitFileStatus.Overwritten;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
        File.WriteAllBytes(outputPath, content);
        return InitFileStatus.Created;
    }

    private static string CombineRelative(
        string root,
        string relativePath)
    {
        var path = root;
        foreach (var segment in relativePath.Replace('\\', '/').Split('/', StringSplitOptions.RemoveEmptyEntries))
        {
            path = Path.Combine(path, segment);
        }

        return path;
    }

    private static bool IsSupportedAgent(string agent)
    {
        return string.Equals(agent, "all", StringComparison.Ordinal)
            || AgentTargets.Any(target => string.Equals(target.Agent, agent, StringComparison.Ordinal));
    }

    private static string SupportedAgentsText()
    {
        return "codex, claude, copilot, all";
    }

    /// <summary>
    /// Maps a supported agent name to the repository-relative skill bundle root it owns.
    /// </summary>
    private sealed record InitAgentTarget(string Agent, string BundleRoot);

    /// <summary>
    /// Represents one packaged init resource after converting its embedded name to a bundle-relative path.
    /// </summary>
    private sealed record InitAsset(string RelativePath, byte[] Content);

    private sealed record InitCommandOptions(
        string AgentSelection,
        bool Overwrite);
}

/// <summary>
/// Describes the files scaffolded by the init command across one or more agent targets.
/// </summary>
internal sealed record InitResult(
    string AgentSelection,
    string RepositoryRoot,
    IReadOnlyList<InitFileResult> Files);

/// <summary>
/// Describes one scaffolded skill-bundle file and whether the command created, preserved, or replaced it.
/// </summary>
internal sealed record InitFileResult(
    string Agent,
    string Path,
    InitFileStatus Status);

/// <summary>
/// Describes how init handled one scaffolded file on disk.
/// </summary>
internal enum InitFileStatus
{
    Created,
    Unchanged,
    Overwritten,
}
