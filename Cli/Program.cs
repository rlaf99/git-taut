using ConsoleAppFramework;
using Git.Remote.Taut;
using Lg2.Sharpy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ZLogger;

using var lg2Global = new Lg2Global();

try
{
    lg2Global.Init();
}
catch (Exception ex)
{
    Console.Error.WriteLine(ex.Message);
    Environment.Exit(1);
}

ConsoleApp.Version = "alpha-0.0.1";

var app = ConsoleApp
    .Create()
    .ConfigureEmptyConfiguration(config =>
    {
        config.AddEnvironmentVariables();
    })
    .ConfigureServices(services =>
    {
        services.AddSingleton<GitCli>();
        services.AddSingleton<TautManager>();
        services.AddSingleton<AesCbc1>();
    })
    .ConfigureLogging(
        (config, logging) =>
        {
            logging.ClearProviders();

            logging.AddZLoggerConsole(options =>
            {
                // log all to standard error
                options.LogToStandardErrorThreshold = LogLevel.Trace;

                options.UsePlainTextFormatter(formatter =>
                {
                    formatter.SetPrefixFormatter(
                        $"{0} {1}[{2:short}]\t",
                        (in MessageTemplate template, in LogInfo info) =>
                            template.Format(
                                info.Timestamp.Local.ToString("hh:mm:ss.ffffff"),
                                ProgramInfo.CommandName,
                                info.LogLevel
                            )
                    );

                    formatter.SetExceptionFormatter(
                        (writer, ex) =>
                            Utf8StringInterpolation.Utf8String.Format(writer, $"{ex.Message}")
                    );
                });

                options.FullMode = BackgroundBufferFullMode.Block;
            });

            if (config.GetGitRemoteTautTrace())
            {
                logging.SetMinimumLevel(LogLevel.Trace);
            }
            else
            {
                logging.SetMinimumLevel(LogLevel.Information);
            }
        }
    );

app.UseFilter<CustomFilter>();

app.Add<GitRemoteHelper>();
app.Add(
    "--print-info",
    () =>
    {
        var lg2Version = Lg2Global.Version;
        ConsoleApp.Log($"LibGit2 Version: {lg2Version}");
    }
);

app.Run(args);

internal class CustomFilter(IServiceProvider serviceProvider, ConsoleAppFilter next)
    : ConsoleAppFilter(next)
{
    void SetLg2TraceOutput()
    {
        var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger<Lg2Trace>();

        Lg2Trace.SetTraceOutput(
            (message) =>
            {
                logger.ZLogTrace($"{message}");
            }
        );
    }

    void ResetLg2TraceOutput()
    {
        Lg2Trace.SetTraceOutput(null);
    }

    public override async Task InvokeAsync(
        ConsoleAppContext context,
        CancellationToken cancellationToken
    )
    {
        ConsoleApp.LogError = msg => Console.Error.WriteLine(msg);

        SetLg2TraceOutput();

        try
        {
            await Next.InvokeAsync(context, cancellationToken);
        }
        finally
        {
            ResetLg2TraceOutput();
        }
    }
}
