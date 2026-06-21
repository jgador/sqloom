using System;
using System.Collections.Generic;
using System.Globalization;

namespace Sqloom.Host;

/// <summary>
/// Provides shared switch parsing and validation helpers for Sqloom commands.
/// </summary>
internal static class CommandArgumentSupport
{
    public static bool HasSwitch(string[] args, string switchName)
    {
        foreach (var arg in args)
        {
            if (string.Equals(arg, switchName, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    public static string? GetArgumentValue(string[] args, string switchName)
    {
        for (var index = 0; index < args.Length - 1; index++)
        {
            if (string.Equals(args[index], switchName, StringComparison.OrdinalIgnoreCase))
            {
                return args[index + 1];
            }
        }

        return null;
    }

    public static string GetRequiredArgumentValue(string[] args, string switchName)
    {
        return GetArgumentValue(args, switchName)
            ?? throw new ArgumentException($"Missing required argument {switchName}.");
    }

    public static int? GetIntArgumentValue(string[] args, string switchName)
    {
        var value = GetArgumentValue(args, switchName);
        if (value is null)
        {
            return null;
        }

        if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedValue))
        {
            throw new ArgumentException($"The value for {switchName} must be an integer.");
        }

        return parsedValue;
    }

    public static double? GetDoubleArgumentValue(string[] args, string switchName)
    {
        var value = GetArgumentValue(args, switchName);
        if (value is null)
        {
            return null;
        }

        if (!double.TryParse(
                value,
                NumberStyles.Float | NumberStyles.AllowThousands,
                CultureInfo.InvariantCulture,
                out var parsedValue))
        {
            throw new ArgumentException($"The value for {switchName} must be numeric.");
        }

        return parsedValue;
    }

    public static void ValidateArguments(
        string[] args,
        HostCommandKind commandKind,
        ISet<string> supportedSwitches,
        ISet<string> valueSwitches)
    {
        var commandVerb = CommandRegistry.GetCommandVerb(commandKind);
        var leadingVerb = CommandRegistry.GetLeadingVerb(args);
        if (leadingVerb is not null && leadingVerb != commandKind)
        {
            throw new ArgumentException(
                $"Unexpected Sqloom stage verb '{args[0]}'. Use '{commandVerb}' with its supported switches only.");
        }

        for (var index = 0; index < args.Length; index++)
        {
            var argument = args[index];
            if (index == 0 && leadingVerb == commandKind)
            {
                continue;
            }

            if (!IsSwitch(argument))
            {
                throw new ArgumentException(
                    $"Unexpected argument '{argument}' for '{commandVerb}'. Sqloom now accepts only the explicit stage verb, the verb-scoped target path, and named switches.");
            }

            if (!supportedSwitches.Contains(argument))
            {
                throw new ArgumentException(
                    $"Unsupported switch '{argument}' for '{commandVerb}'.");
            }

            if (!valueSwitches.Contains(argument))
            {
                continue;
            }

            if (index + 1 >= args.Length || IsSwitch(args[index + 1]))
            {
                throw new ArgumentException($"Missing required value for {argument}.");
            }

            index++;
        }
    }

    public static bool IsSwitch(string value)
    {
        return value.StartsWith("--", StringComparison.Ordinal);
    }
}
