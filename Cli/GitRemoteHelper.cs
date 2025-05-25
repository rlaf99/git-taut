using System.Diagnostics.CodeAnalysis;
using ConsoleAppFramework;
using Lg2.Sharpy;
using Microsoft.Extensions.Logging;
using ZLogger;

namespace Git.Remote.Taut;

partial class GitRemoteHelper(
    ILogger<GitRemoteHelper> logger,
    GitCli gitCli,
    TautManager tautManager
)
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
        internal int Verbosity = 1;

        const string optProgress = "progress";
        internal bool ShowProgress = false;

        const string optCloning = "cloning";
        internal bool IsCloning = false;

        const string optCheckConnectivity = "check-connectivity";
        internal bool CheckConnectivity = false;

        const string optForce = "force";
        internal bool ForceUpdate = false;

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
                    Verbosity = value;
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
                ShowProgress = GetBooleanValue(optProgress);
                LogUpdate(optProgress, ShowProgress);
                Console.WriteLine("ok");
            }
            else if (nameValue.StartsWith(optCloning))
            {
                IsCloning = GetBooleanValue(optCloning);
                LogUpdate(optCloning, IsCloning);
                Console.WriteLine("ok");
            }
            else if (nameValue.StartsWith(optCheckConnectivity))
            {
                CheckConnectivity = GetBooleanValue(optCheckConnectivity);
                LogUpdate(optCheckConnectivity, CheckConnectivity);
                Console.WriteLine("ok");
            }
            else if (nameValue.StartsWith(optForce))
            {
                ForceUpdate = GetBooleanValue("force");
                LogUpdate(optForce, ForceUpdate);
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

static partial class GitRemoteHelperTraces
{
    [ZLoggerMessage(LogLevel.Trace, "Received command '{line}'")]
    internal static partial void TraceReceivedGitCommand(
        this ILogger<GitRemoteHelper> logger,
        string line
    );
}

partial class GitRemoteHelper
{
    record struct CmdFetchArgs(string Hash, string Name)
    {
        internal static CmdFetchArgs Parse(string input)
        {
            var parts = input.Split();
            if (parts.Length != 2)
            {
                throw new ArgumentException($"{cmdFetch}: failed to parse args '{input}'");
            }

            return new CmdFetchArgs(parts[0], parts[1]);
        }
    }
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

        string lastCmd = string.Empty;

        for (; ; )
        {
            var line = Console.ReadLine();
            if (line is null)
            {
                break;
            }

            if (line == cmdCapabilities)
            {
                logger.TraceReceivedGitCommand(line);

                Console.WriteLine(capPush);
                Console.WriteLine(capFetch);
                Console.WriteLine(capCheckConnectivity);
                Console.WriteLine(capOption);
                Console.WriteLine();

                lastCmd = cmdCapabilities;
            }
            else if (line == cmdList)
            {
                logger.TraceReceivedGitCommand(line);

                HandleGitCmdList();

                lastCmd = cmdList;
            }
            else if (line == cmdListForPush)
            {
                logger.TraceReceivedGitCommand(line);

                Console.Error.WriteLine("Not implemented");
                Environment.Exit(1);

                lastCmd = cmdListForPush;
            }
            else if (line == cmdPush)
            {
                logger.TraceReceivedGitCommand(line);

                Console.Error.WriteLine("Not implemented");
                Environment.Exit(1);

                lastCmd = cmdPush;
            }
            else if (line.StartsWith(cmdFetch))
            {
                logger.TraceReceivedGitCommand(line);

                var args = CmdFetchArgs.Parse(line[cmdFetch.Length..].TrimStart());

                HandleGitCmdFetch(args);

                lastCmd = cmdFetch;
            }
            else if (line.StartsWith(cmdOption))
            {
                logger.TraceReceivedGitCommand(line);

                var nameValue = line[cmdOption.Length..].TrimStart();
                _options.HandleNameValue(nameValue);

                lastCmd = cmdFetch;
            }
            else
            {
                if (line.Length == 0)
                {
                    if (lastCmd == cmdFetch)
                    {
                        if (_options.CheckConnectivity)
                        {
                            Console.WriteLine("connectivity-ok");
                            Console.WriteLine();
                        }
                        else
                        {
                            Console.WriteLine();
                        }

                        lastCmd = string.Empty;
                        continue;
                    }
                    else
                    {
                        break;
                    }
                }

                var message = $"Unknown command '{line}'";
                RaiseInvalidOperation(message);
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

    void CloneRemoteIntoTaut()
    {
        gitCli.Execute("clone", "--bare", _address, _tautRepoDir);

        tautManager.Open(_tautRepoDir);

        tautManager.SetDescription();
        tautManager.AddHostObjects();
        tautManager.SetHostRepoRefs();
    }

    void HandleGitCmdList()
    {
        if (Directory.EnumerateFileSystemEntries(_tautRepoDir).Any())
        {
            RaiseNotImplemented($"taut repo exists");
        }
        else
        {
            CloneRemoteIntoTaut();

            foreach (var refName in tautManager.RefList)
            {
                Lg2Oid oid = new();
                tautManager.Repo.GetOidFromName(refName, ref oid);
                Console.WriteLine($"{oid.ToString()} {refName}");
            }

            Console.WriteLine();
        }
    }

    void HandleGitCmdFetch(CmdFetchArgs args)
    {
        Lg2Oid oid = new();
        oid.FromString(args.Hash);
        tautManager.TransferCommitToHost(ref oid);
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
