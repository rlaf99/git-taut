using System.ComponentModel;
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

            void TraceOptionUpdate<TValue>(string opt, TValue value)
            {
                logger.ZLogTrace($"Set option '{opt}' to {value}");
            }

            if (nameValue.StartsWith(optVerbosity))
            {
                if (int.TryParse(nameValue[(optVerbosity.Length + 1)..], out var value))
                {
                    Verbosity = value;
                    TraceOptionUpdate(optVerbosity, value);
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
                TraceOptionUpdate(optProgress, ShowProgress);
                Console.WriteLine("ok");
            }
            else if (nameValue.StartsWith(optCloning))
            {
                IsCloning = GetBooleanValue(optCloning);
                TraceOptionUpdate(optCloning, IsCloning);
                Console.WriteLine("ok");
            }
            else if (nameValue.StartsWith(optCheckConnectivity))
            {
                CheckConnectivity = GetBooleanValue(optCheckConnectivity);
                TraceOptionUpdate(optCheckConnectivity, CheckConnectivity);
                Console.WriteLine("ok");
            }
            else if (nameValue.StartsWith(optForce))
            {
                ForceUpdate = GetBooleanValue("force");
                TraceOptionUpdate(optForce, ForceUpdate);
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
    List<string> _pushBatch = [];

    enum HandleGitCommandResult
    {
        Keep = 0,
        Done = 1,
    }

    delegate HandleGitCommandResult HandleGitCommand(string input);

    HandleGitCommand? _handleGitCommand;

    HandleGitCommandResult DispatchGitCommand(string input)
    {
        if (input.Length == 0)
        {
            Console.WriteLine();

            return HandleGitCommandResult.Done;
        }
        else if (input == cmdCapabilities)
        {
            _handleGitCommand = HanldeGitCmdCapabilities;
        }
        else if (input == cmdList)
        {
            _handleGitCommand = HandleGitCmdList;
        }
        else if (input == cmdListForPush)
        {
            _handleGitCommand = HandleGitCmdListForPush;
        }
        else if (input.StartsWith(cmdPush))
        {
            _handleGitCommand = HandleGitCmdPush;
        }
        else if (input.StartsWith(cmdFetch))
        {
            _handleGitCommand = HandleGitCmdFetch;
        }
        else if (input.StartsWith(cmdOption))
        {
            _handleGitCommand = HandleGitCmdOption;
        }
        else
        {
            RaiseInvalidOperation($"Unknown command '{input}'");
        }

        var result = _handleGitCommand(input);

        return result;
    }

    HandleGitCommandResult HanldeGitCmdCapabilities(string input)
    {
        Console.WriteLine(capPush);
        Console.WriteLine(capFetch);
        Console.WriteLine(capCheckConnectivity);
        Console.WriteLine(capOption);
        Console.WriteLine();

        return HandleGitCommandResult.Done;
    }

    HandleGitCommandResult HandleGitCmdList(string input)
    {
        if (Directory.EnumerateFileSystemEntries(_tautRepoDir).Any())
        {
            RaiseNotImplemented($"taut repo exists");
        }
        else
        {
            GitCloneTaut();

            foreach (var refName in tautManager.TautRepo.GetRefList())
            {
                Lg2Oid oid = new();
                tautManager.TautRepo.GetOidForRef(refName, ref oid);
                Console.WriteLine($"{oid.ToString()} {refName}");
            }

            Console.WriteLine();
        }

        return HandleGitCommandResult.Done;
    }

    HandleGitCommandResult HandleGitCmdFetch(string input)
    {
        if (input.Length == 0)
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

            return HandleGitCommandResult.Done;
        }

        var args = CmdFetchArgs.Parse(input[cmdFetch.Length..].TrimStart());

        Lg2Oid oid = new();
        oid.FromString(args.Hash);
        tautManager.TransferCommitToHost(ref oid);

        return HandleGitCommandResult.Keep;
    }

    HandleGitCommandResult HandleGitCmdListForPush(string input)
    {
        GitUpdateTaut();

        tautManager.Open(_tautRepoDir);

        foreach (var refName in tautManager.TautRepo.GetRefList())
        {
            Lg2Oid oid = new();
            tautManager.TautRepo.GetOidForRef(refName, ref oid);
            Console.WriteLine($"{oid.ToString()} {refName}");
        }

        Console.WriteLine();

        return HandleGitCommandResult.Done;
    }

    HandleGitCommandResult HandleGitCmdPush(string input)
    {
        void HandleBatch()
        {
            foreach (var pushCmd in _pushBatch)
            {
                logger.ZLogTrace($"Handling '{pushCmd}'");

                var refSpecText = pushCmd[cmdPush.Length..].TrimStart();

                if (Lg2RefSpec.TryParseForPush(refSpecText, out var refSpec) == false)
                {
                    RaiseInvalidOperation($"Invalid refspec {refSpecText}");
                }

                var srcRefName = refSpec.GetSrc();

                if (tautManager.HostRepo.TryLookupRef(srcRefName, out var srcRef) == false)
                {
                    RaiseInvalidOperation($"Invalide source reference '{refSpec.GetSrc()}'");
                }

                var mappedSrcRefName = tautManager.MapHostRefToTautened(srcRef);

                var refSpecTextToUse = refSpec.ToString(replaceSrc: mappedSrcRefName);

                gitCli.Execute("--git-dir", _tautRepoDir, "push", _remote, refSpecTextToUse);
            }

            Console.WriteLine();
        }

        if (input.Length == 0)
        {
            HandleBatch();

            return HandleGitCommandResult.Done;
        }
        else if (input.StartsWith(cmdPush))
        {
            _pushBatch.Add(input);

            return HandleGitCommandResult.Keep;
        }
        {
            RaiseInvalidOperation($"{nameof(HandleGitCmdPush)}: Invalid input '{input}'");

            return HandleGitCommandResult.Done;
        }
    }

    HandleGitCommandResult HandleGitCmdOption(string input)
    {
        var nameValue = input[cmdOption.Length..].TrimStart();
        _options.HandleNameValue(nameValue);

        return HandleGitCommandResult.Done;
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

        for (; ; )
        {
            var input = Console.ReadLine();
            if (input is null)
            {
                break;
            }

            logger.TraceReceivedGitCommand(input);

            _handleGitCommand ??= DispatchGitCommand;

            var result = _handleGitCommand(input);
            if (result == HandleGitCommandResult.Done)
            {
                _handleGitCommand = null;
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

    void GitCloneTaut()
    {
        gitCli.Execute("clone", "--bare", _address, _tautRepoDir);

        logger.ZLogTrace($"Clone '{_remote}' to '{_tautRepoDir}'");

        tautManager.Open(_tautRepoDir);

        tautManager.TautRepoAddHostObjects();
        tautManager.TautRepoSetDefaultConfig();
        tautManager.TautRepoSetDefaultDescription();
    }

    void GitUpdateTaut()
    {
        gitCli.Execute("--git-dir", _tautRepoDir, "fetch", _remote, "+refs/heads/*:refs/heads/*");

        logger.ZLogTrace($"Update '{_tautRepoDir}'");
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
