using System;
using System.Collections.Generic;
using Sqloom.Pipeline.QueryStore;
using Sqloom.Testing;

namespace Sqloom.Host;

/// <summary>
/// Parses and validates the Sqloom observe command arguments.
/// </summary>
internal sealed class ObserveArgumentParser
{
    public string? GetQueryStoreConnectionString(string[] args)
    {
        return CommandArgumentSupport.GetArgumentValue(args, "--read-only-connection-string");
    }

    public ObserveArguments Parse(
        string[] args,
        SqloomApplicationManifest? manifest,
        string readOnlyConnectionString,
        string currentDirectory)
    {
        CommandArgumentSupport.ValidateArguments(args, HostCommandKind.Observe);

        QueryStoreOptions observationOptions = new()
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
            ReadOnlyConnection = readOnlyConnectionString,
            ObservationOptions = observationOptions,
            BaseWorkloadProfile = manifest?.WorkloadProfile
                ?? WorkloadProfile.Empty,
            AppOnly = appOnly,
            ShowClassification = showClassification,
            JsonOutputPathOverride = CommandArgumentSupport.GetArgumentValue(args, "--json-output-file"),
            CurrentDirectory = currentDirectory,
        };
    }
}
