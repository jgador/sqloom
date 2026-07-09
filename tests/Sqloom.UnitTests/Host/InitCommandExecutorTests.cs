using System;
using System.IO;
using System.Linq;
using Sqloom.Tests;
using Xunit;

namespace Sqloom.Host.Tests;

/// <summary>
/// Verifies the repository guardrail and skill-bundle file writes performed by the init command.
/// </summary>
public sealed class InitCommandExecutorTests
{
    [Fact]
    public void ScaffoldsCodexSkillBundleByDefault()
    {
        var root = CreateRepositoryRoot();
        try
        {
            InitCommandExecutor executor = new();

            var result = executor.Execute(
                ["init"],
                root);

            Assert.Equal("codex", result.AgentSelection);
            Assert.True(result.Files.Count >= 1);
            Assert.All(result.Files, file => Assert.Equal(InitFileStatus.Created, file.Status));
            var skillPath = Path.Combine(root, ".agents", "skills", "sqloom-harness", "SKILL.md");
            Assert.True(File.Exists(skillPath));
            Assert.Contains("name: sqloom-harness", File.ReadAllText(skillPath), StringComparison.Ordinal);
        }
        finally
        {
            DeleteTestRoot(root);
        }
    }

    [Fact]
    public void ScaffoldsSelectedAgentRoot()
    {
        var root = CreateRepositoryRoot();
        try
        {
            InitCommandExecutor executor = new();

            var result = executor.Execute(
                ["init", "--agent", "claude"],
                root);

            Assert.Equal("claude", result.AgentSelection);
            Assert.All(result.Files, file => Assert.StartsWith(".claude/skills/sqloom-harness/", file.Path, StringComparison.Ordinal));
            Assert.True(File.Exists(Path.Combine(root, ".claude", "skills", "sqloom-harness", "SKILL.md")));
            Assert.False(Directory.Exists(Path.Combine(root, ".agents")));
        }
        finally
        {
            DeleteTestRoot(root);
        }
    }

    [Fact]
    public void ScaffoldsAllAgentRoots()
    {
        var root = CreateRepositoryRoot();
        try
        {
            InitCommandExecutor executor = new();

            var result = executor.Execute(
                ["init", "--agent", "all"],
                root);

            Assert.Equal("all", result.AgentSelection);
            Assert.Equal(["claude", "codex", "copilot"], result.Files.Select(file => file.Agent).Distinct().OrderBy(agent => agent, StringComparer.Ordinal));
            Assert.True(File.Exists(Path.Combine(root, ".agents", "skills", "sqloom-harness", "SKILL.md")));
            Assert.True(File.Exists(Path.Combine(root, ".claude", "skills", "sqloom-harness", "SKILL.md")));
            Assert.True(File.Exists(Path.Combine(root, ".github", "skills", "sqloom-harness", "SKILL.md")));
        }
        finally
        {
            DeleteTestRoot(root);
        }
    }

    [Fact]
    public void LeavesIdenticalFilesUnchanged()
    {
        var root = CreateRepositoryRoot();
        try
        {
            InitCommandExecutor executor = new();
            executor.Execute(["init"], root);

            var result = executor.Execute(
                ["init"],
                root);

            Assert.All(result.Files, file => Assert.Equal(InitFileStatus.Unchanged, file.Status));
        }
        finally
        {
            DeleteTestRoot(root);
        }
    }

    [Fact]
    public void RejectsExistingDifferentFileWithoutOverwrite()
    {
        var root = CreateRepositoryRoot();
        try
        {
            var skillPath = Path.Combine(root, ".agents", "skills", "sqloom-harness", "SKILL.md");
            Directory.CreateDirectory(Path.GetDirectoryName(skillPath)!);
            File.WriteAllText(skillPath, "local content");
            InitCommandExecutor executor = new();

            var exception = Assert.Throws<ArgumentException>(
                () => executor.Execute(["init"], root));

            Assert.Contains("Refusing to overwrite existing file '.agents/skills/sqloom-harness/SKILL.md'", exception.Message, StringComparison.Ordinal);
            Assert.Contains("--overwrite", exception.Message, StringComparison.Ordinal);
        }
        finally
        {
            DeleteTestRoot(root);
        }
    }

    [Fact]
    public void OverwritesExistingDifferentFileWhenRequested()
    {
        var root = CreateRepositoryRoot();
        try
        {
            var skillPath = Path.Combine(root, ".agents", "skills", "sqloom-harness", "SKILL.md");
            Directory.CreateDirectory(Path.GetDirectoryName(skillPath)!);
            File.WriteAllText(skillPath, "local content");
            InitCommandExecutor executor = new();

            var result = executor.Execute(
                ["init", "--overwrite"],
                root);

            Assert.Contains(result.Files, file => file.Path == ".agents/skills/sqloom-harness/SKILL.md" && file.Status == InitFileStatus.Overwritten);
            Assert.Contains("name: sqloom-harness", File.ReadAllText(skillPath), StringComparison.Ordinal);
        }
        finally
        {
            DeleteTestRoot(root);
        }
    }

    [Fact]
    public void RequiresGitMarkerInCurrentDirectory()
    {
        var root = CreateTestRoot();
        try
        {
            InitCommandExecutor executor = new();

            var exception = Assert.Throws<ArgumentException>(
                () => executor.Execute(["init"], root));

            Assert.Contains("Current directory must be a Git repository root", exception.Message, StringComparison.Ordinal);
            Assert.Contains("Run sqloom init from the repository root.", exception.Message, StringComparison.Ordinal);
        }
        finally
        {
            DeleteTestRoot(root);
        }
    }

    [Fact]
    public void RejectsUnknownAgent()
    {
        var root = CreateRepositoryRoot();
        try
        {
            InitCommandExecutor executor = new();

            var exception = Assert.Throws<ArgumentException>(
                () => executor.Execute(["init", "--agent", "cursor"], root));

            Assert.Contains("Unknown agent 'cursor'", exception.Message, StringComparison.Ordinal);
            Assert.Contains("codex, claude, copilot, all", exception.Message, StringComparison.Ordinal);
        }
        finally
        {
            DeleteTestRoot(root);
        }
    }

    private static string CreateRepositoryRoot()
    {
        var root = CreateTestRoot();
        Directory.CreateDirectory(Path.Combine(root, ".git"));
        return root;
    }

    private static string CreateTestRoot()
    {
        var root = Path.Combine(
            RepositoryPaths.GetRepositoryRoot(),
            "artifacts",
            "init-command-tests",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        return root;
    }

    private static void DeleteTestRoot(string root)
    {
        if (Directory.Exists(root))
        {
            Directory.Delete(root, recursive: true);
        }
    }
}
