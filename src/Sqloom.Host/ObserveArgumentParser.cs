using System;
using System.Collections.Generic;
using Sqloom.QueryStore.QueryStore;
using Sqloom.Testing;

namespace Sqloom.Host;

/// <summary>
/// Parses and validates the Sqloom observe command arguments.
/// </summary>
internal sealed class ObserveArgumentParser
{
    private static readonly HashSet<string> SupportedSwitches = new(StringComparer.OrdinalIgnoreCase)
    {
        "--read-only-connection-string",
        "--lookback-hours",
        "--max-plans",
        "--max-waits",
        "--command-timeout-seconds",
        "--json-output-file",
        "--app-only",
        "--show-classification",
    };

    private static readonly HashSet<string> ValueSwitches = new(StringComparer.OrdinalIgnoreCase)
    {
        "--read-only-connection-string",
        "--lookback-hours",
        "--max-plans",
        "--max-waits",
        "--command-timeout-seconds",
        "--json-output-file",
    };

    public string? GetQueryStoreConnectionString(string[] args)
    {
        return CommandArgumentSupport.GetArgumentValue(args, "--read-only-connection-string");
    }

    public ObserveArguments Parse(
        string[] args,
        SqloomApplicationDescriptor? descriptor,
        string readOnlyConnectionString,
        string currentDirectory)
    {
        CommandArgumentSupport.ValidateArguments(
            args,
            HostCommandKind.Observe,
            SupportedSwitches,
            ValueSwitches);

        QueryStoreObservationOptions observationOptions = new()
        {
            LookbackWindow = TimeSpan.FromHours(CommandArgumentSupport.GetDoubleArgumentValue(args, "--lookback-hours") ?? 24d),
            MaxPlans = CommandArgumentSupport.GetIntArgumentValue(args, "--max-plans") ?? 100,
            MaxWaits = CommandArgumentSupport.GetIntArgumentValue(args, "--max-waits") ?? 10,
            CommandTimeoutSeconds = CommandArgumentSupport.GetIntArgumentValue(args, "--command-timeout-seconds") ?? 30,
        };
        var appOnly = CommandArgumentSupport.HasSwitch(args, "--app-only");
        var showClassification = appOnly || CommandArgumentSupport.HasSwitch(args, "--show-classification");

        return new ObserveArguments
        {
            ReadOnlyConnectionString = readOnlyConnectionString,
            ObservationOptions = observationOptions,
            BaseWorkloadProfile = descriptor?.QueryStoreWorkloadProfile
                ?? QueryStoreWorkloadProfile.Empty,
            AppOnly = appOnly,
            ShowClassification = showClassification,
            JsonOutputPathOverride = CommandArgumentSupport.GetArgumentValue(args, "--json-output-file"),
            CurrentDirectory = currentDirectory,
        };
    }
}
