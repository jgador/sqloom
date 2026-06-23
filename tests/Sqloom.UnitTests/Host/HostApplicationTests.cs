using System;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace Sqloom.Host.Tests;

/// <summary>
/// Exercises Sqloom host application dispatch.
/// </summary>
public sealed class HostApplicationTests
{
    [Fact]
    public async Task RunAsync_WithAdviseVerb_InvokesMatchingHandler()
    {
        StubCommandHandler handler = new(HostCommandKind.Advise, 17);
        HostApplication application = new(
            new AppResolver(),
            new HostConsoleWriter(),
            new CommandRegistry(handler));
        HostStartupOptions startupOptions = new()
        {
            ApplicationArguments = ["advise"],
        };

        var result = await application.RunAsync(
            startupOptions,
            Directory.GetCurrentDirectory());

        Assert.Equal(17, result);
        Assert.NotNull(handler.LastContext);
        Assert.Equal("advise", Assert.Single(handler.LastContext!.Arguments));
    }

    [Fact]
    public async Task RunAsync_WithTuneVerb_InvokesMatchingHandler()
    {
        var applicationHarness = new MultipleTestApplicationA();
        StubCommandHandler handler = new(HostCommandKind.Tune, 23);
        HostApplication application = new(
            applicationHarness,
            new HostConsoleWriter(),
            new CommandRegistry(handler));
        HostStartupOptions startupOptions = new()
        {
            ApplicationArguments =
            [
                "tune",
                "--read-only-connection-string",
                "Server=localhost;Database=Sqloom;Trusted_Connection=True;",
            ],
        };

        var result = await application.RunAsync(
            startupOptions,
            Directory.GetCurrentDirectory());

        Assert.Equal(23, result);
        Assert.NotNull(handler.LastContext);
        Assert.Same(applicationHarness, handler.LastContext!.Application);
        Assert.Collection(
            handler.LastContext!.Arguments,
            item => Assert.Equal("tune", item),
            item => Assert.Equal("--read-only-connection-string", item),
            item => Assert.Equal("Server=localhost;Database=Sqloom;Trusted_Connection=True;", item));
    }

    [Fact]
    public async Task RunAsync_WithDebugEnabled_DispatchesEnabledDebugWriter()
    {
        StubCommandHandler handler = new(HostCommandKind.Advise, 19);
        HostApplication application = new(
            new AppResolver(),
            new HostConsoleWriter(),
            new CommandRegistry(handler));
        HostStartupOptions startupOptions = new()
        {
            ApplicationArguments = ["advise"],
            DebugEnabled = true,
        };

        var result = await application.RunAsync(
            startupOptions,
            Directory.GetCurrentDirectory());

        Assert.Equal(19, result);
        Assert.NotNull(handler.LastContext);
        Assert.True(handler.LastContext!.DebugWriter.IsEnabled);
    }

    [Fact]
    public async Task RunAsync_WithReplayVerb_UsesBoundApplication()
    {
        var applicationHarness = new MultipleTestApplicationA();
        StubCommandHandler handler = new(HostCommandKind.Replay, 29);
        HostApplication application = new(
            applicationHarness,
            new HostConsoleWriter(),
            new CommandRegistry(handler));
        HostStartupOptions startupOptions = new()
        {
            ApplicationArguments = ["replay"],
        };

        var result = await application.RunAsync(
            startupOptions,
            Directory.GetCurrentDirectory());

        Assert.Equal(29, result);
        Assert.NotNull(handler.LastContext);
        Assert.Same(applicationHarness, handler.LastContext!.Application);
    }

    [Fact]
    public async Task RunAsync_WithoutCommand_PrintsNoCommandHint()
    {
        HostApplication application = new(
            new AppResolver(),
            new HostConsoleWriter());
        HostStartupOptions startupOptions = new();
        var originalOut = Console.Out;
        using StringWriter stdOut = new();

        await ConsoleCaptureGate.Semaphore.WaitAsync();
        try
        {
            Console.SetOut(stdOut);

            var result = await application.RunAsync(
                startupOptions,
                Directory.GetCurrentDirectory());

            Assert.Equal(0, result);
            Assert.Contains(
                "Use --help to print the available host arguments.",
                stdOut.ToString(),
                StringComparison.Ordinal);
        }
        finally
        {
            Console.SetOut(originalOut);
            ConsoleCaptureGate.Semaphore.Release();
        }
    }

    private sealed class StubCommandHandler(
        HostCommandKind commandKind,
        int exitCode)
        : ICommandHandler
    {
        public CommandExecutionContext? LastContext { get; private set; }

        public HostCommandKind CommandKind => commandKind;

        public Task<int> ExecuteAsync(CommandExecutionContext context)
        {
            LastContext = context;
            return Task.FromResult(exitCode);
        }
    }
}
