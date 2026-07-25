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

        if (string.Equals(args[0], "help", StringComparison.OrdinalIgnoreCase))
        {
            return HostCommandKind.Help;
        }

        return CommandCatalog.Find(args[0])?.Kind;
    }

    internal static string GetCommandVerb(HostCommandKind commandKind)
    {
        return CommandCatalog.GetRequired(commandKind).Verb;
    }
}
