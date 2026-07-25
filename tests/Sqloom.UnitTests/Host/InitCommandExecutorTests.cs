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
            var result = InitCommandExecutor.Execute(
                ["init"],
                root);

            Assert.Equal("codex", result.AgentSelection);
            Assert.True(result.Files.Count >= 1);
            Assert.All(result.Files, file => Assert.Equal(InitFileStatus.Created, file.Status));
            var skillPath = Path.Combine(root, ".agents", "skills", "sqloom", "SKILL.md");
            var commandReferencePath = Path.Combine(root, ".agents", "skills", "sqloom", "references", "commands.md");
            Assert.True(File.Exists(skillPath));
            Assert.True(File.Exists(commandReferencePath));
            var skillText = File.ReadAllText(skillPath);
            Assert.Contains("name: sqloom", skillText, StringComparison.Ordinal);
            Assert.Contains("tests/Sqloom/<app>/<profile>/Harness.cs", skillText, StringComparison.Ordinal);
            Assert.DoesNotContain("artifacts/sqloom/harnesses", skillText, StringComparison.Ordinal);
            Assert.Contains("# Sqloom Command Reference", File.ReadAllText(commandReferencePath), StringComparison.Ordinal);
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
            var result = InitCommandExecutor.Execute(
                ["init", "--agent", "claude"],
                root);

            Assert.Equal("claude", result.AgentSelection);
            Assert.All(result.Files, file => Assert.StartsWith(".claude/skills/sqloom/", file.Path, StringComparison.Ordinal));
            Assert.True(File.Exists(Path.Combine(root, ".claude", "skills", "sqloom", "SKILL.md")));
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
            var result = InitCommandExecutor.Execute(
                ["init", "--agent", "all"],
                root);

            Assert.Equal("all", result.AgentSelection);
            Assert.Equal(["claude", "codex", "copilot"], result.Files.Select(file => file.Agent).Distinct().OrderBy(agent => agent, StringComparer.Ordinal));
            Assert.True(File.Exists(Path.Combine(root, ".agents", "skills", "sqloom", "SKILL.md")));
            Assert.True(File.Exists(Path.Combine(root, ".claude", "skills", "sqloom", "SKILL.md")));
            Assert.True(File.Exists(Path.Combine(root, ".github", "skills", "sqloom", "SKILL.md")));
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
            InitCommandExecutor.Execute(["init"], root);

            var result = InitCommandExecutor.Execute(
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
            var skillPath = Path.Combine(root, ".agents", "skills", "sqloom", "SKILL.md");
            Directory.CreateDirectory(Path.GetDirectoryName(skillPath)!);
            File.WriteAllText(skillPath, "local content");

            var exception = Assert.Throws<ArgumentException>(
                () => InitCommandExecutor.Execute(["init"], root));

            Assert.Contains("Refusing to overwrite existing file '.agents/skills/sqloom/SKILL.md'", exception.Message, StringComparison.Ordinal);
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
            var skillPath = Path.Combine(root, ".agents", "skills", "sqloom", "SKILL.md");
            Directory.CreateDirectory(Path.GetDirectoryName(skillPath)!);
            File.WriteAllText(skillPath, "local content");

            var result = InitCommandExecutor.Execute(
                ["init", "--overwrite"],
                root);

            Assert.Contains(result.Files, file => file.Path == ".agents/skills/sqloom/SKILL.md" && file.Status == InitFileStatus.Overwritten);
            Assert.Contains("name: sqloom", File.ReadAllText(skillPath), StringComparison.Ordinal);
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

            var exception = Assert.Throws<ArgumentException>(
                () => InitCommandExecutor.Execute(["init"], root));

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

            var exception = Assert.Throws<ArgumentException>(
                () => InitCommandExecutor.Execute(["init", "--agent", "cursor"], root));

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
