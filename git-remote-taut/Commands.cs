using ConsoleAppFramework;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Git.Remote.Taut;

internal static class ConfigurationExtensions
{
    const string KeyGitRemoteTautTrace = "GIT_REMOTE_TAUT_TRACE";

    internal static bool GetGitRemoteTautTrace(this IConfiguration config)
    {
        var val = config[KeyGitRemoteTautTrace];
        if (val is null)
        {
            return false;
        }

        if (val == "0" || val.Equals("false", StringComparison.InvariantCultureIgnoreCase))
        {
            return false;
        }

        return true;
    }
}

public partial class Commands(ILogger<Commands> logger)
{
    /// <summary>
    /// Invoked by Git and handle the commands from Git.
    /// </summary>
    /// <param name="remote">Remote name.</param>
    /// <param name="address">Remote address.</param>
    [Command("")]
    public void RemoteHelper([Argument] string remote, [Argument] string address) =>
        HandleGitCommands(remote, address);
}

partial class Commands
{
    // for pushing
    const string capPush = "push";

    // for fetching
    const string capFetch = "fetch";
    const string capCheckConnectivity = "check-connectivity";

    // miscellaneous
    const string capOption = "option";

    const string cmdCapabilities = "capabilities";

    const string cmdList = "list";
    const string cmdListForPush = "list for-push";
    const string cmdPush = "push";
    const string cmdFetch = "fetch";
    const string cmdOption = "option";

    const string optVerbosity = "verbosity";
    int _verbosity = 1;

    const string optProgress = "progress";
    bool _showProgress = false;

    const string optCloning = "cloning";
    bool _isCloning = false;

    // bool _checkConnectivity = false;
    // bool _forceUpdate = false;

    void HandleGitCommands(string remote, string address)
    {
        logger.LogInformation($"Working on remote '{remote}' at '{address}'");

        for (; ; )
        {
            var line = Console.ReadLine();
            if (line is null)
            {
                break;
            }

            if (line == cmdCapabilities)
            {
                logger.LogTrace("Command '{0}' received", cmdCapabilities);

                Console.WriteLine(capPush);
                Console.WriteLine(capFetch);
                Console.WriteLine(capCheckConnectivity);
                Console.WriteLine(capOption);
                Console.WriteLine();

                continue;
            }

            if (line == cmdList)
            {
                logger.LogTrace("Command '{0}' received", cmdList);

                Console.Error.WriteLine("Not implemented");
                Environment.Exit(1);

                continue;
            }

            if (line == cmdListForPush)
            {
                logger.LogTrace("Command '{0}' received", cmdListForPush);

                Console.Error.WriteLine("Not implemented");
                Environment.Exit(1);

                continue;
            }

            if (line == cmdPush)
            {
                logger.LogTrace("Command '{0}' received", cmdPush);

                Console.Error.WriteLine("Not implemented");
                Environment.Exit(1);

                continue;
            }

            if (line.StartsWith(cmdFetch))
            {
                logger.LogTrace("Command '{0}' received", cmdFetch);

                Console.Error.WriteLine("Not implemented");
                Environment.Exit(1);

                continue;
            }

            if (line.StartsWith(cmdOption))
            {
                var nameValue = line[cmdOption.Length..].TrimStart();

                logger.LogTrace("Option '{0}' received", nameValue);

                HanldeGitOption(nameValue);

                continue;
            }
        }
    }

    void HanldeGitOption(string nameValue)
    {
        bool GetBooleanValue(string opt)
        {
            return nameValue[(opt.Length + 1)..] == "true";
        }

        if (nameValue.StartsWith(optVerbosity))
        {
            if (int.TryParse(nameValue[(optVerbosity.Length + 1)..], out var value))
            {
                _verbosity = value;
            }

            Console.WriteLine("ok");

            return;
        }

        if (nameValue.StartsWith(optProgress))
        {
            _showProgress = GetBooleanValue(optProgress);

            Console.WriteLine("ok");

            return;
        }

        if (nameValue.StartsWith(optCloning))
        {
            _isCloning = GetBooleanValue(optCloning);

            Console.WriteLine("ok");

            return;
        }

        // _checkConnectivity = GetBooleanValue("check-connectivity");
        // _forceUpdate = GetBooleanValue("force");

        Console.WriteLine("unsupported");
    }
}
