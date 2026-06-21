using System;
using System.Collections.Generic;
using System.Linq;

namespace Sqloom.Host;

/// <summary>
/// Maps Sqloom stage verbs to their registered handlers.
/// </summary>
internal sealed class CommandRegistry
{
    private readonly IReadOnlyDictionary<HostCommandKind, ICommandHandler> _handlers;

    public CommandRegistry(params ICommandHandler[] handlers)
        : this((IEnumerable<ICommandHandler>)handlers)
    {
    }

    public CommandRegistry(IEnumerable<ICommandHandler> handlers)
    {
        ArgumentNullException.ThrowIfNull(handlers);

        _handlers = handlers.ToDictionary(
            static handler => handler.CommandKind,
            static handler => handler);
    }

    public ICommandHandler GetRequiredHandler(HostCommandKind commandKind)
    {
        if (_handlers.TryGetValue(commandKind, out var handler))
        {
            return handler;
        }

        throw new ArgumentOutOfRangeException(
            nameof(commandKind),
            commandKind,
            "Sqloom does not have a handler for the selected command kind.");
    }

    public static HostCommandKind GetCommandKind(string[] args)
    {
        if (args.Length == 0)
        {
            return HostCommandKind.None;
        }

        if (CommandArgumentSupport.HasSwitch(args, "--help"))
        {
            return HostCommandKind.Help;
        }

        var leadingVerb = GetLeadingVerb(args);
        if (leadingVerb is not null)
        {
            return leadingVerb.Value;
        }

        return HostCommandKind.None;
    }

    internal static HostCommandKind? GetLeadingVerb(string[] args)
    {
        if (args.Length == 0 || CommandArgumentSupport.IsSwitch(args[0]))
        {
            return null;
        }

        return args[0].ToLowerInvariant() switch
        {
            "help" => HostCommandKind.Help,
            "observe" => HostCommandKind.Observe,
            "tune" => HostCommandKind.Tune,
            "replay" => HostCommandKind.Replay,
            "correlate" => HostCommandKind.Correlate,
            "advise" => HostCommandKind.Advise,
            _ => null,
        };
    }

    internal static string GetCommandVerb(HostCommandKind commandKind)
    {
        return commandKind switch
        {
            HostCommandKind.Observe => "observe",
            HostCommandKind.Tune => "tune",
            HostCommandKind.Replay => "replay",
            HostCommandKind.Correlate => "correlate",
            HostCommandKind.Advise => "advise",
            _ => throw new ArgumentOutOfRangeException(nameof(commandKind), commandKind, null),
        };
    }
}
