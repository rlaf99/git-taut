using ConsoleAppFramework;
using Git.Remote.Taut;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ZLogger;

ConsoleApp.Version = "alpha-0.0.1";

var app = ConsoleApp
    .Create()
    .ConfigureEmptyConfiguration(config =>
    {
        config.AddEnvironmentVariables();
    })
    .ConfigureLogging(
        (config, logging) =>
        {
            logging.ClearProviders();

            logging.AddZLoggerConsole(options =>
            {
                // log all to standard error
                options.LogToStandardErrorThreshold = LogLevel.Trace;
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

app.Add<Commands>();
app.Run(args);
