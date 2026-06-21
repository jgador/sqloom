using System;
using System.IO;
using System.IO.Pipes;
using System.Runtime.CompilerServices;
using Xunit;

namespace Sqloom.Host.Tests;

[AttributeUsage(AttributeTargets.Method)]
internal sealed class RequiresDockerFactAttribute : FactAttribute
{
    public RequiresDockerFactAttribute(
        [CallerFilePath] string? sourceFilePath = null,
        [CallerLineNumber] int sourceLineNumber = -1)
        : base(sourceFilePath, sourceLineNumber)
    {
        if (!DockerAvailabilityProbe.IsAvailable())
        {
            Skip = "Docker must be running to execute this Testcontainers-based integration test.";
        }
    }

    private static class DockerAvailabilityProbe
    {
        public static bool IsAvailable()
        {
            if (OperatingSystem.IsWindows())
            {
                try
                {
                    using var pipe = new NamedPipeClientStream(
                        ".",
                        "docker_engine",
                        PipeDirection.InOut,
                        PipeOptions.Asynchronous);
                    pipe.Connect(200);
                    return true;
                }
                catch
                {
                    return false;
                }
            }

            if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
            {
                return File.Exists("/var/run/docker.sock");
            }

            return false;
        }
    }
}
