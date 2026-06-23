using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using Sqloom.Tests;
using Xunit;

namespace Sqloom.Host.Tests;

/// <summary>
/// Exercises Sqloom harness resolution.
/// </summary>
public sealed class AppResolverTests
{
    [Fact]
    public void Resolve_LoadsApplicationFromExplicitHarnessProjectPathWithoutBuild()
    {
        AppResolver resolver = new();
        HostStartupOptions startupOptions = new()
        {
            AppTargetPath = SqloomRepositoryPaths.GetTestAppApplicationProjectPath(),
            NoBuild = true,
        };

        var application = resolver.Resolve(startupOptions);
        var manifest = application.Describe(new Sqloom.Testing.SqloomApplicationContext
        {
            CurrentDirectory = SqloomRepositoryPaths.GetRepositoryRoot(),
        });

        Assert.Equal("Sqloom Test App", manifest.Name);
        Assert.Equal("Sqloom.TestApp.Harness.TestAppApplication", application.GetType().FullName);
    }

    [Fact]
    public void Resolve_ThrowsWhenTargetDoesNotContainSqloomApplication()
    {
        AppResolver resolver = new();
        HostStartupOptions startupOptions = new()
        {
            AppTargetPath = SqloomRepositoryPaths.GetTestAppProjectPath(),
            NoBuild = true,
        };

        var exception = Assert.Throws<AppResolutionException>(
            () => resolver.Resolve(startupOptions));

        Assert.Contains("does not contain an ISqloomApplication implementation", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Resolve_ThrowsWhenHarnessProjectContainsMultipleApplications()
    {
        var tempDirectoryPath = CreateTempDirectory();

        try
        {
            var projectPath = WriteHarnessProject(
                tempDirectoryPath,
                """
                public sealed class FirstHarnessApplication : ISqloomApplication
                {
                    public SqloomApplicationManifest Describe(SqloomApplicationContext context)
                    {
                        return new SqloomApplicationManifest
                        {
                            Name = "First",
                            OpenApiDocumentPath = System.IO.Path.GetFullPath("openapi.json"),
                            ReplayProfile = new ReplayProfile(),
                        };
                    }

                    public ValueTask<ISqloomApplicationSession> StartAsync(
                        SqloomApplicationContext context,
                        CancellationToken cancellationToken = default)
                    {
                        throw new NotSupportedException();
                    }
                }

                public sealed class SecondHarnessApplication : ISqloomApplication
                {
                    public SqloomApplicationManifest Describe(SqloomApplicationContext context)
                    {
                        return new SqloomApplicationManifest
                        {
                            Name = "Second",
                            OpenApiDocumentPath = System.IO.Path.GetFullPath("openapi.json"),
                            ReplayProfile = new ReplayProfile(),
                        };
                    }

                    public ValueTask<ISqloomApplicationSession> StartAsync(
                        SqloomApplicationContext context,
                        CancellationToken cancellationToken = default)
                    {
                        throw new NotSupportedException();
                    }
                }
                """);
            BuildProject(projectPath, tempDirectoryPath);

            AppResolver resolver = new();
            HostStartupOptions startupOptions = new()
            {
                AppTargetPath = projectPath,
                NoBuild = true,
            };

            var exception = Assert.Throws<AppResolutionException>(
                () => resolver.Resolve(startupOptions));

            Assert.Contains("multiple public ISqloomApplication implementations", exception.Message, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("FirstHarnessApplication", exception.Message, StringComparison.Ordinal);
            Assert.Contains("SecondHarnessApplication", exception.Message, StringComparison.Ordinal);
        }
        finally
        {
            DeleteDirectoryIfExists(tempDirectoryPath);
        }
    }

    [Fact]
    public void Resolve_DeduplicatesRepeatedProjectsFromSolutionFilter()
    {
        AppResolver resolver = new();
        var tempDirectoryPath = CreateTempDirectory();

        try
        {
            var solutionFilterPath = WriteSolutionFilter(
                tempDirectoryPath,
                SqloomRepositoryPaths.GetTestAppApplicationProjectPath(),
                SqloomRepositoryPaths.GetTestAppApplicationProjectPath());
            HostStartupOptions startupOptions = new()
            {
                AppTargetPath = solutionFilterPath,
                NoBuild = true,
            };

            var application = resolver.Resolve(startupOptions);
            var manifest = application.Describe(new Sqloom.Testing.SqloomApplicationContext
            {
                CurrentDirectory = SqloomRepositoryPaths.GetRepositoryRoot(),
            });

            Assert.Equal("Sqloom Test App", manifest.Name);
        }
        finally
        {
            DeleteDirectoryIfExists(tempDirectoryPath);
        }
    }

    [Fact]
    public void ResolveAssemblyPath_WithHarnessProjectPathWithoutBuild_ReturnsBuildOutputPath()
    {
        AppResolver resolver = new();
        HostStartupOptions startupOptions = new()
        {
            AppTargetPath = SqloomRepositoryPaths.GetTestAppApplicationProjectPath(),
            NoBuild = true,
        };

        var assemblyPath = resolver.ResolveAssemblyPath(startupOptions);

        Assert.Equal(
            SqloomRepositoryPaths.GetExpectedTestAppApplicationBuildOutputPath(),
            assemblyPath,
            StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public void Resolve_ThrowsWhenProjectPathIsMissing()
    {
        AppResolver resolver = new();
        HostStartupOptions startupOptions = new()
        {
            AppTargetPath = Path.Combine(
                Path.GetTempPath(),
                "sqloom-tests",
                Guid.NewGuid().ToString("N"),
                "MissingApp.csproj"),
        };

        var exception = Assert.Throws<AppResolutionException>(
            () => resolver.Resolve(startupOptions));

        Assert.Contains("does not exist", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Resolve_ThrowsWhenTargetPathIsMissing()
    {
        AppResolver resolver = new();

        var exception = Assert.Throws<AppResolutionException>(
            () => resolver.Resolve(new HostStartupOptions()));

        Assert.Contains("requires an explicit harness target path", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static string WriteHarnessProject(
        string directoryPath,
        string applicationSource)
    {
        var projectPath = Path.Combine(directoryPath, "TempHarness.csproj");
        File.WriteAllText(
            projectPath,
            $$"""
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net10.0</TargetFramework>
                <ImplicitUsings>disable</ImplicitUsings>
                <Nullable>enable</Nullable>
              </PropertyGroup>
              <ItemGroup>
                <ProjectReference Include="{{SqloomRepositoryPaths.GetTestingProjectPath()}}" />
              </ItemGroup>
            </Project>
            """);
        File.WriteAllText(
            Path.Combine(directoryPath, "HarnessApplications.cs"),
            $$"""
            using System;
            using System.Threading;
            using System.Threading.Tasks;
            using Sqloom.Core.Execution;
            using Sqloom.Testing;

            namespace TempHarness;

            {{applicationSource}}
            """);

        return projectPath;
    }

    private static string WriteSolutionFilter(string directoryPath, params string[] projectPaths)
    {
        var solutionFilterPath = Path.Combine(directoryPath, "sqloom-targets.slnf");
        var document = JsonSerializer.Serialize(
            new
            {
                solution = new
                {
                    projects = projectPaths,
                },
            });
        File.WriteAllText(
            solutionFilterPath,
            document);
        return solutionFilterPath;
    }

    private static void BuildProject(
        string projectPath,
        string workingDirectory)
    {
        ProcessStartInfo startInfo = new("dotnet")
        {
            CreateNoWindow = true,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false,
            WorkingDirectory = workingDirectory,
        };
        startInfo.ArgumentList.Add("build");
        startInfo.ArgumentList.Add(projectPath);
        startInfo.ArgumentList.Add("--tl:off");
        startInfo.ArgumentList.Add("--nologo");
        startInfo.ArgumentList.Add("-clp:ErrorsOnly;NoSummary");

        using Process process = new()
        {
            StartInfo = startInfo,
        };
        if (!process.Start())
        {
            throw new InvalidOperationException("Failed to start dotnet while building a temporary harness project.");
        }

        var standardOutput = process.StandardOutput.ReadToEnd();
        var standardError = process.StandardError.ReadToEnd();
        process.WaitForExit();
        Assert.True(
            process.ExitCode == 0,
            $"Temp harness build failed.{Environment.NewLine}StdOut:{Environment.NewLine}{standardOutput}{Environment.NewLine}StdErr:{Environment.NewLine}{standardError}");
    }

    private static string CreateTempDirectory()
    {
        var directoryPath = Path.Combine(
            Path.GetTempPath(),
            "sqloom-tests",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directoryPath);
        return directoryPath;
    }

    private static void DeleteDirectoryIfExists(string directoryPath)
    {
        if (Directory.Exists(directoryPath))
        {
            try
            {
                Directory.Delete(
                    directoryPath,
                    recursive: true);
            }
            catch (UnauthorizedAccessException)
            {
            }
        }
    }
}
