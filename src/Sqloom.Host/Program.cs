using System.Threading.Tasks;

namespace Sqloom.Host;

/// <summary>
/// Entrypoint for the packaged Sqloom host tool.
/// </summary>
public static class Program
{
    /// <summary>
    /// Runs the packaged Sqloom command-line tool and returns its process exit code.
    /// </summary>
    public static async Task<int> Main(string[] args)
    {
        return await HostRuntime.RunAsync(args).ConfigureAwait(false);
    }
}
