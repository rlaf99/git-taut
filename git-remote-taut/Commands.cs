using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using ConsoleAppFramework;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ZLogger;

namespace Git.Remote.Taut;

static class KnownEnvironVars
{
    internal const string GitDir = "GIT_DIR";

    internal const string GitRemoteTautTrace = "GIT_REMOTE_TAUT_TRACE";
}

internal static class ConfigurationExtensions
{
    internal static bool GetGitRemoteTautTrace(this IConfiguration config)
    {
        var val = config[KnownEnvironVars.GitRemoteTautTrace];
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

class GitRemoteHelper(ILogger<GitRemoteHelper> logger)
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

    const string tautDirName = "taut";

    // bool _checkConnectivity = false;
    // bool _forceUpdate = false;

    string _remote = default!;
    string _address = default!;
    string _gitDir = default!;
    string _tautDir = default!;

    /// <summary>
    /// Invoked by Git and handle the commands from Git.
    /// </summary>
    /// <param name="remote">Remote name.</param>
    /// <param name="address">Remote address.</param>
    [Command("")]
    public void HandleGitCommands([Argument] string remote, [Argument] string address)
    {
        _remote = remote;
        _address = address;

        logger.ZLogInformation($"Working on remote '{remote}' at '{address}'");

        EnsureTautDir();

        for (; ; )
        {
            var line = Console.ReadLine();
            if (line is null)
            {
                break;
            }

            if (line == cmdCapabilities)
            {
                logger.ZLogTrace($"Command '{cmdCapabilities}' received");

                Console.WriteLine(capPush);
                Console.WriteLine(capFetch);
                Console.WriteLine(capCheckConnectivity);
                Console.WriteLine(capOption);
                Console.WriteLine();

                continue;
            }

            if (line == cmdList)
            {
                logger.ZLogTrace($"Command '{cmdList}' received");

                HandleGitCmdList();
                continue;
            }

            if (line == cmdListForPush)
            {
                logger.ZLogTrace($"Command '{cmdListForPush}' received");

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

                HandleGitCmdOption(nameValue);

                continue;
            }
        }
    }

    void EnsureTautDir()
    {
        var gitDir = Environment.GetEnvironmentVariable(KnownEnvironVars.GitDir);
        if (gitDir is null)
        {
            RaiseInvalidOperation("Git dir is null");
        }

        _gitDir = gitDir;

        var tautDir = Path.Join(_gitDir, tautDirName);
        Directory.CreateDirectory(tautDir);

        logger.ZLogTrace($"Taut dir localtion '{tautDir}'");

        _tautDir = tautDir;
    }

    void RunGit(params string[] args)
    {
        var startInfo = new ProcessStartInfo("git")
        {
            Arguments = string.Join(" ", args),
            CreateNoWindow = true,
            UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };

        logger.ZLogTrace($"Run git with arguments '{startInfo.Arguments}'");

        Process process = new() { StartInfo = startInfo };

        static void DataReceiver(object sender, DataReceivedEventArgs args)
        {
            if (args.Data is not null)
                Console.Error.WriteLine(args.Data);
        }

        process.OutputDataReceived += DataReceiver;
        process.ErrorDataReceived += DataReceiver;

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        process.StandardInput.Close();

        process.WaitForExit();
        process.Close();
    }

    List<string> GetGitOutputLines(params string[] args)
    {
        var startInfo = new ProcessStartInfo("git")
        {
            Arguments = string.Join(" ", args),
            CreateNoWindow = true,
            UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };

        logger.ZLogTrace($"Run git with arguments '{startInfo.Arguments}'");

        List<string> result = [];

        Process process = new() { StartInfo = startInfo };

        void OutputDataReceiver(object sender, DataReceivedEventArgs args)
        {
            if (args.Data is not null)
            {
                result.Add(args.Data);
            }
        }

        static void ErrorDataReceiver(object sender, DataReceivedEventArgs args)
        {
            if (args.Data is not null)
            {
                Console.Error.WriteLine(args.Data);
            }
        }

        process.OutputDataReceived += OutputDataReceiver;
        process.ErrorDataReceived += ErrorDataReceiver;

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        process.StandardInput.Close();

        process.WaitForExit();
        process.Close();

        return result;
    }

    void CloneRemoteRepoIntoTaut()
    {
        RunGit("clone", "--bare", _address, _tautDir);
    }

    void HandleGitCmdList()
    {
        if (Directory.EnumerateFileSystemEntries(_tautDir).Any())
        {
            RaiseNotImplemented($"taut repo exists");
        }
        else
        {
            CloneRemoteRepoIntoTaut();
            var lines = GetGitOutputLines("--git-dir", _tautDir, "show-ref");
            foreach (var line in lines)
            {
                // logger.ZLogDebug($"output '{line}'");
                Console.WriteLine(line);
            }
            Console.WriteLine();
        }

        // RaiseNotImplemented($"Command '{cmdList}' not implemented");
    }

    void HandleGitCmdOption(string nameValue)
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

    [DoesNotReturn]
    void RaiseInvalidOperation(string message)
    {
        logger.ZLogError($"{message}");
        throw new InvalidOperationException(message);
    }

    [DoesNotReturn]
    void RaiseNotImplemented(string message)
    {
        logger.ZLogCritical($"{message}");
        throw new NotImplementedException(message);
    }
}
