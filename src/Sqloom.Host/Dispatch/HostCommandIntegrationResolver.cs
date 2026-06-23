using System;
using System.Threading;
using System.Threading.Tasks;
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

    public async Task<HostCommandBindings> ResolveAsync(
        HostCommandKind commandKind,
        HostStartupOptions startupOptions,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(startupOptions);

        switch (commandKind)
        {
            case HostCommandKind.Observe:
                return new HostCommandBindings
                {
                    Application = await ResolveObserveApplicationAsync(
                            startupOptions,
                            cancellationToken)
                        .ConfigureAwait(false),
                };
            case HostCommandKind.Tune:
            case HostCommandKind.Replay:
                return new HostCommandBindings
                {
                    Application = await ResolveRequiredApplicationAsync(
                            startupOptions,
                            cancellationToken)
                        .ConfigureAwait(false),
                };
            case HostCommandKind.Correlate:
            case HostCommandKind.Advise:
                return new HostCommandBindings
                {
                    Application = _boundApplication,
                };
            default:
                throw new ArgumentOutOfRangeException(
                nameof(commandKind),
                commandKind,
                "Sqloom could not resolve an app harness for the selected command kind.");
        }
    }

    public Task<ISqloomApplication?> ResolveBannerApplicationAsync(
        HostStartupOptions startupOptions,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(startupOptions);
        return ResolveObserveApplicationAsync(startupOptions, cancellationToken);
    }

    private async Task<ISqloomApplication?> ResolveObserveApplicationAsync(
        HostStartupOptions startupOptions,
        CancellationToken cancellationToken)
    {
        if (_boundApplication is not null)
        {
            return _boundApplication;
        }

        if (!startupOptions.HasTargetSelection)
        {
            return null;
        }

        return await _appResolver
            .ResolveAsync(startupOptions, cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task<ISqloomApplication> ResolveRequiredApplicationAsync(
        HostStartupOptions startupOptions,
        CancellationToken cancellationToken)
    {
        if (_boundApplication is not null)
        {
            return _boundApplication;
        }

        return await _appResolver
            .ResolveAsync(startupOptions, cancellationToken)
            .ConfigureAwait(false);
    }
}
