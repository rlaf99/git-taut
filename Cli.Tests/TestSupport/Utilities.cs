using System.CommandLine;
using Git.Taut;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ZLogger;

namespace Cli.Tests.TestSupport;

static class ITestOutputHelperExtensions
{
    internal static void DumpError(
        this ITestOutputHelper testOutput,
        InvocationConfiguration invocationConfiguration
    )
    {
        var erorrText = invocationConfiguration.Error.ToString();

        if (string.IsNullOrEmpty(erorrText))
        {
            testOutput.WriteLine($"No error output from {nameof(InvocationConfiguration)}");
        }
        else
        {
            testOutput.WriteLine($"Error output from {nameof(InvocationConfiguration)}:");
            testOutput.WriteLine(erorrText);
        }
    }
}

ref struct PushDirectory : IDisposable
{
    string? _pushed;

    internal PushDirectory(string? targetDirectory = null)
    {
        _pushed = Directory.GetCurrentDirectory();

        if (targetDirectory is not null)
        {
            Directory.SetCurrentDirectory(targetDirectory);
        }
    }

    public void Dispose()
    {
        var pushed = Interlocked.Exchange(ref _pushed, null);
        if (pushed is null)
        {
            return;
        }

        Directory.SetCurrentDirectory(pushed);
    }
}

class TestHostBuilder
{
    public static IHost BuildHost()
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);

        builder.AddGitTautConfiguration();
        builder.AddGitTautServices();
        builder.AddGitTautCommandActions();
        builder.AddGitRemoteTautCommandActions();
        builder.AddGitTautLogging();

        return builder.Build();
    }

    public static IHost BuildHost(ITestOutputHelper testOutput)
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);

        builder.AddGitTautConfiguration();
        builder.AddGitTautServices();
        builder.AddGitTautCommandActions();
        builder.AddGitRemoteTautCommandActions();

        var logging = builder.Logging;

        logging.ClearProviders();
        logging.AddZLoggerInMemory(processor =>
        {
            processor.MessageReceived += msg => testOutput.WriteLine(msg);
        });

        if (builder.Configuration.GetGitTautTrace())
        {
            logging.SetMinimumLevel(LogLevel.Trace);
        }
        else
        {
            logging.SetMinimumLevel(LogLevel.Information);
        }

        return builder.Build();
    }
}
