using System;
using Sqloom.Testing;

namespace Sqloom.Host;

/// <summary>
/// Carries the application harness selected for a host command.
/// </summary>
internal sealed class HostCommandBindings
{
    /// <summary>
    /// Null means the command is intentionally running without a resolved harness application.
    /// </summary>
    public ISqloomApplication? Application { get; init; }
}
