using System;

namespace Sqloom.Host;

/// <summary>
/// Carries options for Sqloom host startup.
/// </summary>
internal sealed class HostStartupOptions
{
    public string[] ApplicationArguments { get; init; } = Array.Empty<string>();

    public string? AppTargetPath { get; init; }

    public string DotNetCommand { get; init; } = "dotnet";

    public bool NoBuild { get; init; }

    public bool ShowHelp { get; init; }

    public bool ShowVersion { get; init; }

    public bool DebugEnabled { get; init; }

    public bool HasTargetSelection =>
        !string.IsNullOrWhiteSpace(AppTargetPath);
}
