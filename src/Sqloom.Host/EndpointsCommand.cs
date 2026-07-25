using System;
using System.Threading.Tasks;
using Sqloom.Host.Replay;
using Sqloom.Pipeline.Artifacts;

namespace Sqloom.Host;

/// <summary>
/// Discovers replayable ASP.NET Core controller endpoints without starting a harness.
/// </summary>
internal sealed class EndpointsCommand
    : ICommandHandler
{
    private readonly EndpointCatalogLoader _catalogLoader = new();
    private readonly EndpointSourceProjectResolver _sourceProjectResolver = new();

    public HostCommandKind CommandKind => HostCommandKind.Endpoints;

    public async Task<int> ExecuteAsync(CommandExecutionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        CommandArgumentSupport.ValidateArguments(context.Arguments, HostCommandKind.Endpoints);
        var sourceProjectPath = _sourceProjectResolver.Resolve(
            context.Arguments,
            context.StartupOptions,
            context.CurrentDirectory);
        var operations = await _catalogLoader
            .LoadAsync(sourceProjectPath)
            .ConfigureAwait(false);
        var jsonOutputPath = CommandArgumentSupport.GetArgumentValue(
            context.Arguments,
            "--json-output-file");
        if (!string.IsNullOrWhiteSpace(jsonOutputPath))
        {
            jsonOutputPath = System.IO.Path.GetFullPath(
                jsonOutputPath,
                context.CurrentDirectory);
            await JsonFileWriter
                .WriteAsync(jsonOutputPath, operations, default)
                .ConfigureAwait(false);
        }

        context.ConsoleWriter.PrintEndpointCatalog(
            sourceProjectPath,
            operations,
            jsonOutputPath);
        return 0;
    }
}
