using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Sqloom.Tests;
using Xunit;

namespace Sqloom.Host.Tests;

/// <summary>
/// Exercises Sqloom harness resolution.
/// </summary>
public sealed class AppResolverTests
{
    [Fact]
    public async Task Resolve_LoadsExplicitHarnessProjectWithoutBuild()
    {
        var tempDirectoryPath = CreateTempDir();

        try
        {
            var projectPath = WriteAndBuildSingleApplicationProject(tempDirectoryPath);
            AppResolver resolver = new();
            HostStartupOptions startupOptions = new()
            {
                AppTargetPath = projectPath,
                NoBuild = true,
            };

            var application = await resolver.ResolveAsync(startupOptions);
            var manifest = application.Describe(new Sqloom.Testing.SqloomApplicationContext
            {
                CurrentDirectory = RepositoryPaths.GetRepositoryRoot(),
            });

            Assert.Equal("Temporary Harness", manifest.Name);
            Assert.Equal("TempHarness.HarnessApplication", application.GetType().FullName);
        }
        finally
        {
            DeleteDirectoryIfExists(tempDirectoryPath);
        }
    }

    [Fact]
    public async Task Resolve_ThrowsWhenTargetDoesNotContainSqloomApplication()
    {
        AppResolver resolver = new();
        HostStartupOptions startupOptions = new()
        {
            AppTargetPath = RepositoryPaths.GetTestAppProjectPath(),
            NoBuild = true,
        };

        var exception = await Assert.ThrowsAsync<AppResolutionException>(
            () => resolver.ResolveAsync(startupOptions));

        Assert.Contains("does not contain an ISqloomApplication implementation", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Resolve_ThrowsWhenHarnessProjectContainsMultipleApplications()
    {
        var tempDirectoryPath = CreateTempDir();

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

            var exception = await Assert.ThrowsAsync<AppResolutionException>(
                () => resolver.ResolveAsync(startupOptions));

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
    public async Task Resolve_DeduplicatesRepeatedProjectsFromSolutionFilter()
    {
        AppResolver resolver = new();
        var tempDirectoryPath = CreateTempDir();

        try
        {
            var projectPath = WriteAndBuildSingleApplicationProject(tempDirectoryPath);
            var solutionFilterPath = WriteSolutionFilter(
                tempDirectoryPath,
                projectPath,
                projectPath);
            HostStartupOptions startupOptions = new()
            {
                AppTargetPath = solutionFilterPath,
                NoBuild = true,
            };

            var application = await resolver.ResolveAsync(startupOptions);
            var manifest = application.Describe(new Sqloom.Testing.SqloomApplicationContext
            {
                CurrentDirectory = RepositoryPaths.GetRepositoryRoot(),
            });

            Assert.Equal("Temporary Harness", manifest.Name);
        }
        finally
        {
            DeleteDirectoryIfExists(tempDirectoryPath);
        }
    }

    [Fact]
    public async Task ResolveAssemblyPath_WithoutBuild_ReturnsBuildOutputPath()
    {
        var tempDirectoryPath = CreateTempDir();

        try
        {
            var projectPath = WriteAndBuildSingleApplicationProject(tempDirectoryPath);
            AppResolver resolver = new();
            HostStartupOptions startupOptions = new()
            {
                AppTargetPath = projectPath,
                NoBuild = true,
            };

            var assemblyPath = await resolver.ResolveAssemblyPathAsync(startupOptions);

            Assert.Equal(
                Path.Combine(
                    tempDirectoryPath,
                    "bin",
                    "Debug",
                    "net10.0",
                    $"{Path.GetFileNameWithoutExtension(projectPath)}.dll"),
                assemblyPath,
                StringComparer.OrdinalIgnoreCase);
        }
        finally
        {
            DeleteDirectoryIfExists(tempDirectoryPath);
        }
    }

    [Fact]
    public async Task Resolve_LoadsExplicitCSharpFileApplication()
    {
        AppResolver resolver = new();
        HostStartupOptions startupOptions = new()
        {
            AppTargetPath = RepositoryPaths.GetSampleApplicationFilePath(),
        };

        var application = await resolver.ResolveAsync(startupOptions);
        var manifest = application.Describe(new Sqloom.Testing.SqloomApplicationContext
        {
            CurrentDirectory = RepositoryPaths.GetRepositoryRoot(),
        });

        Assert.Equal("Sqloom Test App", manifest.Name);
        Assert.Equal("SqloomTestApplication", application.GetType().FullName);
    }

    [Fact]
    public async Task Resolve_CSharpFileSharesEntityFrameworkCoreDependencies()
    {
        AppResolver resolver = new();
        HostStartupOptions startupOptions = new()
        {
            AppTargetPath = RepositoryPaths.GetSampleApplicationFilePath(),
        };
        var application = await resolver.ResolveAsync(startupOptions);
        await using var session = await application.StartAsync(new Sqloom.Testing.SqloomApplicationContext
        {
            CurrentDirectory = RepositoryPaths.GetRepositoryRoot(),
        });
        var captureCollector = session.ReplayHost.Services.GetService(
            typeof(Sqloom.Testing.AspNetCore.ReplaySqlCaptureCollector)) as
            Sqloom.Testing.AspNetCore.ReplaySqlCaptureCollector;
        Assert.NotNull(captureCollector);
        Sqloom.Pipeline.Execution.EndpointReplayRequest request = new()
        {
            OperationKey = "GET /api/products/by-category",
            HttpMethod = "GET",
            Route = "/api/products/by-category",
            RelativePathAndQuery = "/api/products/by-category?categoryId=1&minPrice=9.99",
        };

        var result = await Sqloom.Host.Replay.ReplayRequestExecutor.ExecuteAsync(
            session.ReplayHost.Client,
            captureCollector,
            request,
            accessToken: "sqloom-test-app-token",
            artifactPath: "operation.json",
            TestContext.Current.CancellationToken);

        Assert.Null(result.ErrorMessage);
        Assert.Equal("replayed", result.Status);
        Assert.Equal(200, result.HttpStatusCode);
    }

    [Fact]
    public async Task Resolve_CSharpFileWithNoBuild_Throws()
    {
        AppResolver resolver = new();
        HostStartupOptions startupOptions = new()
        {
            AppTargetPath = RepositoryPaths.GetSampleApplicationFilePath(),
            NoBuild = true,
        };

        var exception = await Assert.ThrowsAsync<AppResolutionException>(
            () => resolver.ResolveAsync(startupOptions));

        Assert.Contains("always built", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("--no-build", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ResolveAssemblyPath_CSharpFileBuildsToUniqueTemporaryAssembly()
    {
        AppResolver resolver = new();
        HostStartupOptions startupOptions = new()
        {
            AppTargetPath = RepositoryPaths.GetSampleApplicationFilePath(),
        };

        var firstAssemblyPath = await resolver.ResolveAssemblyPathAsync(startupOptions);
        var secondAssemblyPath = await resolver.ResolveAssemblyPathAsync(startupOptions);

        try
        {
            Assert.NotEqual(firstAssemblyPath, secondAssemblyPath);
            Assert.True(File.Exists(firstAssemblyPath));
            Assert.True(File.Exists(secondAssemblyPath));
            Assert.True(File.Exists(Path.ChangeExtension(firstAssemblyPath, ".deps.json")));
            Assert.True(File.Exists(Path.ChangeExtension(secondAssemblyPath, ".deps.json")));
            Assert.True(Directory.Exists(Path.Combine(Path.GetDirectoryName(firstAssemblyPath)!, "build")));
            Assert.True(Directory.Exists(Path.Combine(Path.GetDirectoryName(secondAssemblyPath)!, "build")));
        }
        finally
        {
            DeleteDirectoryIfExists(Path.GetDirectoryName(firstAssemblyPath)!);
            DeleteDirectoryIfExists(Path.GetDirectoryName(secondAssemblyPath)!);
        }
    }

    [Fact]
    public async Task Resolve_SameNamedCSharpFilesLoadInIsolatedContexts()
    {
        var firstDirectoryPath = CreateTempDir();
        var secondDirectoryPath = CreateTempDir();
        string? firstOutputDirectory = null;
        string? secondOutputDirectory = null;
        try
        {
            var firstSourcePath = WriteCSharpHarness(
                firstDirectoryPath,
                "First file harness");
            var secondSourcePath = WriteCSharpHarness(
                secondDirectoryPath,
                "Second file harness");
            AppResolver resolver = new();

            var firstApplication = await resolver.ResolveAsync(new HostStartupOptions
            {
                AppTargetPath = firstSourcePath,
            });
            var secondApplication = await resolver.ResolveAsync(new HostStartupOptions
            {
                AppTargetPath = secondSourcePath,
            });
            firstOutputDirectory = Path.GetDirectoryName(firstApplication.GetType().Assembly.Location);
            secondOutputDirectory = Path.GetDirectoryName(secondApplication.GetType().Assembly.Location);

            Assert.Equal(
                "First file harness",
                firstApplication.Describe(new Sqloom.Testing.SqloomApplicationContext()).Name);
            Assert.Equal(
                "Second file harness",
                secondApplication.Describe(new Sqloom.Testing.SqloomApplicationContext()).Name);
            Assert.NotEqual(
                firstApplication.GetType().Assembly.Location,
                secondApplication.GetType().Assembly.Location);
        }
        finally
        {
            DeleteDirectoryIfExists(firstDirectoryPath);
            DeleteDirectoryIfExists(secondDirectoryPath);
            DeleteDirectoryIfExists(firstOutputDirectory);
            DeleteDirectoryIfExists(secondOutputDirectory);
        }
    }

    [Fact]
    public async Task ResolveAssemblyPath_CSharpFileHonorsLiteralAssemblyName()
    {
        var tempDirectoryPath = CreateTempDir();
        string? outputDirectory = null;
        try
        {
            var sourcePath = WriteCSharpHarness(
                tempDirectoryPath,
                "Custom assembly file harness",
                assemblyName: "Custom.FileHarness");
            AppResolver resolver = new();

            var assemblyPath = await resolver.ResolveAssemblyPathAsync(new HostStartupOptions
            {
                AppTargetPath = sourcePath,
            });
            outputDirectory = Path.GetDirectoryName(assemblyPath);

            Assert.Equal(
                "Custom.FileHarness.dll",
                Path.GetFileName(assemblyPath));
            Assert.True(File.Exists(assemblyPath));
            Assert.True(File.Exists(Path.ChangeExtension(assemblyPath, ".deps.json")));
        }
        finally
        {
            DeleteDirectoryIfExists(tempDirectoryPath);
            DeleteDirectoryIfExists(outputDirectory);
        }
    }

    [Theory]
    [InlineData("9.0.311", true, 9)]
    [InlineData("10.0.100-preview.7", true, 10)]
    [InlineData("11.0.0", true, 11)]
    [InlineData("SDK warning\r\n10.0.301", true, 10)]
    [InlineData("not-a-version", false, 0)]
    public void TryGetFileBasedAppSdkMajorVersion_ParsesSupportedVersions(
        string value,
        bool expectedResult,
        int expectedMajorVersion)
    {
        var result = AppProjectResolver.TryGetFileBasedAppSdkMajorVersion(
            value,
            out var majorVersion);

        Assert.Equal(expectedResult, result);
        Assert.Equal(expectedMajorVersion, majorVersion);
    }

    [Theory]
    [InlineData("9.0.311")]
    [InlineData("not-a-version")]
    public void ValidateFileBasedAppSdkVersion_RejectsUnsupportedVersions(string reportedVersion)
    {
        var exception = Assert.Throws<AppResolutionException>(
            () => AppProjectResolver.ValidateFileBasedAppSdkVersion(
                "custom-dotnet",
                reportedVersion));

        Assert.Contains("SDK 10 or later", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("custom-dotnet", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ValidateFileBasedAppSdkVersion_AcceptsVersion10()
    {
        AppProjectResolver.ValidateFileBasedAppSdkVersion(
            "custom-dotnet",
            "10.0.301");
    }

    [Fact]
    public async Task Resolve_CSharpFileUsesSelectedDotNetCommand()
    {
        var dotNetCommand = $"missing-dotnet-{Guid.NewGuid():N}";
        AppResolver resolver = new();
        HostStartupOptions startupOptions = new()
        {
            AppTargetPath = RepositoryPaths.GetSampleApplicationFilePath(),
            DotNetCommand = dotNetCommand,
        };

        var exception = await Assert.ThrowsAsync<AppResolutionException>(
            () => resolver.ResolveAsync(startupOptions));

        Assert.Contains(dotNetCommand, exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Resolve_ThrowsWhenProjectPathIsMissing()
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

        var exception = await Assert.ThrowsAsync<AppResolutionException>(
            () => resolver.ResolveAsync(startupOptions));

        Assert.Contains("does not exist", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Resolve_ThrowsWhenCSharpFilePathIsMissing()
    {
        AppResolver resolver = new();
        HostStartupOptions startupOptions = new()
        {
            AppTargetPath = Path.Combine(
                Path.GetTempPath(),
                "sqloom-tests",
                Guid.NewGuid().ToString("N"),
                "MissingHarness.cs"),
        };

        var exception = await Assert.ThrowsAsync<AppResolutionException>(
            () => resolver.ResolveAsync(startupOptions));

        Assert.Contains("does not exist", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Resolve_DirectoryDoesNotDiscoverCSharpFiles()
    {
        var tempDirectoryPath = CreateTempDir();
        try
        {
            File.WriteAllText(
                Path.Combine(tempDirectoryPath, "Harness.cs"),
                "public sealed class Harness;");
            AppResolver resolver = new();
            HostStartupOptions startupOptions = new()
            {
                AppTargetPath = tempDirectoryPath,
            };

            var exception = await Assert.ThrowsAsync<AppResolutionException>(
                () => resolver.ResolveAsync(startupOptions));

            Assert.Contains("did not resolve", exception.Message, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            DeleteDirectoryIfExists(tempDirectoryPath);
        }
    }

    [Fact]
    public async Task Resolve_ThrowsWhenTargetPathIsMissing()
    {
        AppResolver resolver = new();

        var exception = await Assert.ThrowsAsync<AppResolutionException>(
            () => resolver.ResolveAsync(new HostStartupOptions()));

        Assert.Contains("requires an explicit harness target path", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static string WriteHarnessProject(
        string directoryPath,
        string applicationSource)
    {
        var projectPath = Path.Combine(
            directoryPath,
            $"TempHarness{Guid.NewGuid():N}.csproj");
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
                <ProjectReference Include="{{RepositoryPaths.GetTestingProjectPath()}}" />
              </ItemGroup>
            </Project>
            """);
        File.WriteAllText(
            Path.Combine(directoryPath, "HarnessApplications.cs"),
            $$"""
            using System;
            using System.Threading;
            using System.Threading.Tasks;
            using Sqloom.Pipeline.Execution;
            using Sqloom.Testing;

            namespace TempHarness;

            {{applicationSource}}
            """);

        return projectPath;
    }

    private static string WriteAndBuildSingleApplicationProject(string directoryPath)
    {
        var projectPath = WriteHarnessProject(
            directoryPath,
            """
            public sealed class HarnessApplication : ISqloomApplication
            {
                public SqloomApplicationManifest Describe(SqloomApplicationContext context)
                {
                    return new SqloomApplicationManifest
                    {
                        Name = "Temporary Harness",
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
        BuildProject(projectPath, directoryPath);
        return projectPath;
    }

    private static string WriteCSharpHarness(
        string directoryPath,
        string applicationName,
        string? assemblyName = null)
    {
        var testingProjectPath = RepositoryPaths
            .GetTestingProjectPath()
            .Replace('\\', '/');
        var sourcePath = Path.Combine(
            directoryPath,
            "Harness.cs");
        var assemblyNameDirective = string.IsNullOrWhiteSpace(assemblyName)
            ? string.Empty
            : $"#:property AssemblyName={assemblyName}{Environment.NewLine}";
        File.WriteAllText(
            sourcePath,
            $$"""
            #:property TargetFramework=net10.0
            {{assemblyNameDirective}}#:project {{testingProjectPath}}

            using System;
            using System.Threading;
            using System.Threading.Tasks;
            using Sqloom.Pipeline.Execution;
            using Sqloom.Pipeline.QueryStore;
            using Sqloom.Testing;

            internal static class Program
            {
                private static void Main()
                {
                }
            }

            public sealed class HarnessApplication : ISqloomApplication
            {
                public SqloomApplicationManifest Describe(SqloomApplicationContext context)
                {
                    return new SqloomApplicationManifest
                    {
                        Name = "{{applicationName}}",
                        ReplayProfile = new ReplayProfile(),
                        WorkloadProfile = new WorkloadProfile
                        {
                            Name = "TempHarness",
                        },
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

        return sourcePath;
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

    private static string CreateTempDir()
    {
        var directoryPath = Path.Combine(
            Path.GetTempPath(),
            "sqloom-tests",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directoryPath);
        return directoryPath;
    }

    private static void DeleteDirectoryIfExists(string? directoryPath)
    {
        if (!string.IsNullOrWhiteSpace(directoryPath)
            && Directory.Exists(directoryPath))
        {
            try
            {
                Directory.Delete(
                    directoryPath,
                    recursive: true);
            }
            catch (Exception exception) when (
                exception is IOException
                    or UnauthorizedAccessException)
            {
            }
        }
    }
}
