using System;
using Sqloom.Testing;

namespace Sqloom.Host;

/// <summary>
/// Carries the application harness selected for a host command.
/// </summary>
internal sealed class HostCommandBindings
{
    public ISqloomApplication? Application { get; init; }
}
