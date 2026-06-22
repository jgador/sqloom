using System;
using System.Collections.Generic;
using Sqloom.Core.Contracts;

namespace Sqloom.Host;

/// <summary>
/// Carries the app integration bindings selected for a host command.
/// </summary>
internal sealed class HostCommandBindings
{
    public IAppIntegration? AppIntegration { get; init; }

    public IReadOnlyList<IAppIntegration> AppIntegrations { get; init; } = Array.Empty<IAppIntegration>();
}
