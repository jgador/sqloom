using System.Threading;

namespace Sqloom.Host.Tests;

internal static class ConsoleCaptureGate
{
    public static SemaphoreSlim Semaphore { get; } = new(1, 1);
}
