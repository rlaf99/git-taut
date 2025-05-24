using System.Diagnostics.CodeAnalysis;
using ConsoleAppFramework;
using Lg2.Sharpy;
using Microsoft.Extensions.Logging;
using ZLogger;

namespace Git.Remote.Taut;

partial class GitRemoteHelper(ILogger<GitRemoteHelper> logger, GitCli gitCli, TautRepo tautRepo)
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

    const string tautDirName = "taut";
}

partial class GitRemoteHelper
{
    class Options(ILogger<GitRemoteHelper> logger)
    {
        const string optVerbosity = "verbosity";
        int _verbosity = 1;

        const string optProgress = "progress";
        bool _showProgress = false;

        const string optCloning = "cloning";
        bool _isCloning = false;

        const string optCheckConnectivity = "check-connectivity";
        bool _checkConnectivity = false;

        const string optForce = "force";
        bool _forceUpdate = false;

        internal void HandleNameValue(string nameValue)
        {
            bool GetBooleanValue(string opt)
            {
                return nameValue[(opt.Length + 1)..] == "true";
            }

            void LogUpdate<TValue>(string opt, TValue value)
            {
                logger.ZLogTrace($"Set option '{opt}' to {value}");
            }

            if (nameValue.StartsWith(optVerbosity))
            {
                if (int.TryParse(nameValue[(optVerbosity.Length + 1)..], out var value))
                {
                    _verbosity = value;
                    LogUpdate(optVerbosity, value);
                    Console.WriteLine("ok");
                }
                else
                {
                    Console.WriteLine("error falied to parse the value");
                }
            }
            else if (nameValue.StartsWith(optProgress))
            {
                _showProgress = GetBooleanValue(optProgress);
                LogUpdate(optProgress, _showProgress);
                Console.WriteLine("ok");
            }
            else if (nameValue.StartsWith(optCloning))
            {
                _isCloning = GetBooleanValue(optCloning);
                LogUpdate(optCloning, _isCloning);
                Console.WriteLine("ok");
            }
            else if (nameValue.StartsWith(optCheckConnectivity))
            {
                _checkConnectivity = GetBooleanValue(optCheckConnectivity);
                LogUpdate(optCheckConnectivity, _checkConnectivity);
                Console.WriteLine("ok");
            }
            else if (nameValue.StartsWith(optForce))
            {
                _forceUpdate = GetBooleanValue("force");
                LogUpdate(optForce, _forceUpdate);
                Console.WriteLine("ok");
            }
            else
            {
                Console.WriteLine("unsupported");
            }
        }
    }

    readonly Options _options = new(logger);
}

partial class GitRemoteHelper
{
    string _remote = default!;
    string _address = default!;
    string _hostRepoDir = default!;
    string _tautRepoDir = default!;

    /// <summary>
    /// Invoked by Git and act as a remote helper.
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
            }
            else if (line == cmdList)
            {
                logger.ZLogTrace($"Command '{cmdList}' received");

                HandleGitCmdList();
            }
            else if (line == cmdListForPush)
            {
                logger.ZLogTrace($"Command '{cmdListForPush}' received");

                Console.Error.WriteLine("Not implemented");
                Environment.Exit(1);
            }
            else if (line == cmdPush)
            {
                logger.LogTrace("Command '{0}' received", cmdPush);

                Console.Error.WriteLine("Not implemented");
                Environment.Exit(1);
            }
            else if (line.StartsWith(cmdFetch))
            {
                logger.ZLogTrace($"Received Command '{line}'");

                Console.Error.WriteLine("Not implemented");
                Environment.Exit(1);
            }
            else if (line.StartsWith(cmdOption))
            {
                logger.ZLogTrace($"Received Command '{line}'");

                var nameValue = line[cmdOption.Length..].TrimStart();
                _options.HandleNameValue(nameValue);
            }
            else
            {
                Console.Error.WriteLine("Unknown command '{0}'", line);
                Environment.Exit(1);
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

        _hostRepoDir = gitDir;

        var tautDir = Path.Join(_hostRepoDir, tautDirName);
        Directory.CreateDirectory(tautDir);

        logger.ZLogTrace($"Taut dir locates at '{tautDir}'");

        _tautRepoDir = tautDir;
    }

    TautRepo CloneRemoteIntoTaut()
    {
        gitCli.Execute("clone", "--bare", _address, _tautRepoDir);

        tautRepo.Open(_tautRepoDir);

        tautRepo.SetDescription();
        tautRepo.AddHostObjects();
        tautRepo.SetHostRepoRefs();

        return tautRepo;
    }

    void HandleGitCmdList()
    {
        if (Directory.EnumerateFileSystemEntries(_tautRepoDir).Any())
        {
            RaiseNotImplemented($"taut repo exists");
        }
        else
        {
            var tautRepo = CloneRemoteIntoTaut();

            tautRepo.TransferCommonObjectsToHostRepo();

            Thread.Sleep(100); // for zlogger to flush
            // logger.ZLogDebug($"IsBare {tautRepo.IsBare()}");

            // var lines = gitCli.GetOutputLines("--git-dir", _tautRepoDir, "show-ref");
            // foreach (var line in lines)
            // {
            //     Console.WriteLine(line);
            // }
            Console.WriteLine();
        }

        // RaiseNotImplemented($"Command '{cmdList}' not implemented");
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
