using System;
using Sqloom.Testing;

namespace Sqloom.Host;

/// <summary>
/// Resolves the application harness required by a selected host command.
/// </summary>
internal sealed class HostCommandIntegrationResolver
{
    private readonly AppResolver _appResolver;
    private readonly ISqloomApplication? _boundApplication;

    public HostCommandIntegrationResolver(AppResolver appResolver)
        : this(appResolver, null)
    {
    }

    public HostCommandIntegrationResolver(ISqloomApplication application)
        : this(new AppResolver(), application)
    {
    }

    internal HostCommandIntegrationResolver(
        AppResolver appResolver,
        ISqloomApplication? boundApplication)
    {
        _appResolver = appResolver ?? throw new ArgumentNullException(nameof(appResolver));
        _boundApplication = boundApplication;
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
                Application = ResolveObserveApplication(startupOptions),
            },
            HostCommandKind.Tune => new HostCommandBindings
            {
                Application = ResolveRequiredApplication(startupOptions),
            },
            HostCommandKind.Replay => new HostCommandBindings
            {
                Application = ResolveRequiredApplication(startupOptions),
            },
            HostCommandKind.Correlate or HostCommandKind.Advise => new HostCommandBindings
            {
                Application = _boundApplication,
            },
            _ => throw new ArgumentOutOfRangeException(
                nameof(commandKind),
                commandKind,
                "Sqloom could not resolve an app harness for the selected command kind."),
        };
    }

    public ISqloomApplication? ResolveBannerApplication(HostStartupOptions startupOptions)
    {
        ArgumentNullException.ThrowIfNull(startupOptions);
        return ResolveObserveApplication(startupOptions);
    }

    private ISqloomApplication? ResolveObserveApplication(HostStartupOptions startupOptions)
    {
        if (_boundApplication is not null)
        {
            return _boundApplication;
        }

        if (!startupOptions.HasTargetSelection)
        {
            return null;
        }

        return _appResolver.Resolve(startupOptions);
    }

    private ISqloomApplication ResolveRequiredApplication(HostStartupOptions startupOptions)
    {
        return _boundApplication
            ?? _appResolver.Resolve(startupOptions);
    }
}
