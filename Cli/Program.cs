using ConsoleAppFramework;
using Git.Taut;
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

ConsoleApp.Timeout = TimeSpan.FromMicroseconds(800);

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
        services.AddSingleton<KeyValueStore>();
        services.AddSingleton<Aes256Cbc1>();
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
                        $"{0} {1}[{2:short}]\t\t",
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

            if (config.GetGitTautTrace())
            {
                logging.SetMinimumLevel(LogLevel.Trace);
            }
            else
            {
                logging.SetMinimumLevel(LogLevel.Information);
            }
        }
    );

app.UseFilter<ExtraFilter>();

app.Add<GitRemoteHelper>();
app.Add<ExtraCommands>();

await app.RunAsync(args);
