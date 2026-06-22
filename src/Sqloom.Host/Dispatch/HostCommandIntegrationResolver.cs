using System;
using System.Collections.Generic;
using Sqloom.Core.Contracts;

namespace Sqloom.Host;

/// <summary>
/// Resolves the app integrations required by a selected host command.
/// </summary>
internal sealed class HostCommandIntegrationResolver
{
    private readonly AppResolver _appResolver;
    private readonly IAppIntegration? _boundAppIntegration;

    public HostCommandIntegrationResolver(AppResolver appResolver)
        : this(appResolver, null)
    {
    }

    public HostCommandIntegrationResolver(IAppIntegration appIntegration)
        : this(new AppResolver(), appIntegration)
    {
    }

    internal HostCommandIntegrationResolver(
        AppResolver appResolver,
        IAppIntegration? boundAppIntegration)
    {
        _appResolver = appResolver ?? throw new ArgumentNullException(nameof(appResolver));
        _boundAppIntegration = boundAppIntegration;
    }

    public HostCommandBindings Resolve(
        HostCommandKind commandKind,
        HostStartupOptions startupOptions)
    {
        ArgumentNullException.ThrowIfNull(startupOptions);

        return commandKind switch
        {
            HostCommandKind.Observe => new HostCommandBindings
            {
                AppIntegration = ResolveObserveIntegration(startupOptions),
            },
            HostCommandKind.Tune => new HostCommandBindings
            {
                AppIntegration = ResolveRequiredIntegration(startupOptions),
            },
            HostCommandKind.Replay => new HostCommandBindings
            {
                AppIntegrations = ResolveReplayIntegrations(startupOptions),
            },
            HostCommandKind.Correlate or HostCommandKind.Advise => new HostCommandBindings
            {
                AppIntegration = _boundAppIntegration,
            },
            _ => throw new ArgumentOutOfRangeException(
                nameof(commandKind),
                commandKind,
                "Sqloom could not resolve app integrations for the selected command kind."),
        };
    }

    public IAppIntegration? ResolveBannerIntegration(HostStartupOptions startupOptions)
    {
        ArgumentNullException.ThrowIfNull(startupOptions);
        return ResolveObserveIntegration(startupOptions);
    }

    private IAppIntegration? ResolveObserveIntegration(HostStartupOptions startupOptions)
    {
        if (_boundAppIntegration is not null)
        {
            return _boundAppIntegration;
        }

        if (!startupOptions.HasTargetSelection)
        {
            return null;
        }

        return _appResolver.Resolve(startupOptions);
    }

    private IAppIntegration ResolveRequiredIntegration(HostStartupOptions startupOptions)
    {
        return _boundAppIntegration
            ?? _appResolver.Resolve(startupOptions);
    }

    private IReadOnlyList<IAppIntegration> ResolveReplayIntegrations(HostStartupOptions startupOptions)
    {
        if (_boundAppIntegration is not null)
        {
            return [_boundAppIntegration];
        }

        return _appResolver.ResolveReplayIntegrations(startupOptions);
    }
}
