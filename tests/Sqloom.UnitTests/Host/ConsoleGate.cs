using System.Threading;

namespace Sqloom.Host.Tests;

internal static class ConsoleGate
{
    public static SemaphoreSlim Semaphore { get; } = new(1, 1);
}
