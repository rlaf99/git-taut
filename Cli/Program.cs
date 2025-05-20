using System.Text;
using ConsoleAppFramework;
using Git.Remote.Taut;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ZLogger;
using static Lg2.Native.LibGit2Exports;

var rc = git_libgit2_init();
if (rc < 0)
{
    Console.Error.WriteLine($"Failed to initialize libgit2: {rc}");
    Environment.Exit(1);
}

const string commandName = "git-remote-taut";

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
                                commandName,
                                info.LogLevel
                            )
                    );

                    formatter.SetExceptionFormatter(
                        (writer, ex) =>
                            Utf8StringInterpolation.Utf8String.Format(writer, $"{ex.Message}")
                    );
                });
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

app.Add<GitRemoteHelper>();
app.Add(
    "--print-info",
    () =>
    {
        var libgit2Version = Encoding.UTF8.GetString(LIBGIT2_VERSION);
        ConsoleApp.Log($"LibGit2 Version: {libgit2Version}");
    }
);
app.Run(args);

git_libgit2_shutdown();
