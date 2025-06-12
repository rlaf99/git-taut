using ConsoleAppFramework;
using Lg2.Sharpy;
using Microsoft.Extensions.Logging;
using ZLogger;

namespace Git.Taut;

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

        const string optDryRun = "dry-run";
        internal bool DryRun = false;

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
                    Console.WriteLine("ok");

                    TraceOptionUpdate(optVerbosity, value);
                }
                else
                {
                    Console.WriteLine("error falied to parse the value");
                }
            }
            else if (nameValue.StartsWith(optProgress))
            {
                ShowProgress = GetBooleanValue(optProgress);
                Console.WriteLine("ok");

                TraceOptionUpdate(optProgress, ShowProgress);
            }
            else if (nameValue.StartsWith(optCloning))
            {
                IsCloning = GetBooleanValue(optCloning);
                Console.WriteLine("ok");

                TraceOptionUpdate(optCloning, IsCloning);
            }
            else if (nameValue.StartsWith(optCheckConnectivity))
            {
                CheckConnectivity = GetBooleanValue(optCheckConnectivity);
                Console.WriteLine("ok");

                TraceOptionUpdate(optCheckConnectivity, CheckConnectivity);
            }
            else if (nameValue.StartsWith(optForce))
            {
                ForceUpdate = GetBooleanValue(optForce);
                Console.WriteLine("ok");

                TraceOptionUpdate(optForce, ForceUpdate);
            }
            else if (nameValue.StartsWith(optDryRun))
            {
                DryRun = GetBooleanValue(optDryRun);

                Console.WriteLine("ok");

                TraceOptionUpdate(optDryRun, DryRun);
            }
            else
            {
                Console.WriteLine("unsupported");

                logger.ZLogTrace($"Unsupported option '{nameValue}'");
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
            throw new InvalidOperationException($"Unknown command '{input}'");
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

    void ListRegainedRefs()
    {
        var refList = tautManager.OrdinaryTautRefs;
        foreach (var refName in refList)
        {
            var regainedRefName = tautManager.RegainedRefSpec.TransformToTarget(refName);
            var oid = new Lg2Oid();
            tautManager.TautRepo.GetRefOid(regainedRefName, ref oid);
            var oidText = oid.ToHexDigits();

            logger.ZLogTrace($"{nameof(ListRegainedRefs)}: {oidText} {refName}");

            Console.WriteLine($"{oidText} {refName}");
        }
    }

    HandleGitCommandResult HandleGitCmdList(string input)
    {
        if (Directory.EnumerateFileSystemEntries(_tautRepoDir).Any())
        {
            GitFetchTaut();

            tautManager.RegainHostRefs();

            ListRegainedRefs();
        }
        else
        {
            GitCloneTaut();

            tautManager.RegainHostRefs();

            ListRegainedRefs();
        }

        Console.WriteLine();

        return HandleGitCommandResult.Done;
    }

    HandleGitCommandResult HandleGitCmdFetch(string input)
    {
        if (input.Length == 0)
        {
            if (_options.CheckConnectivity)
            {
                Console.WriteLine("connectivity-ok");
            }
            Console.WriteLine();

            return HandleGitCommandResult.Done;
        }

        var args = CmdFetchArgs.Parse(input[cmdFetch.Length..].TrimStart());

        Lg2Oid oid = new();
        oid.FromHexDigits(args.Hash);
        tautManager.RegainCommit(ref oid);

        return HandleGitCommandResult.Keep;
    }

    HandleGitCommandResult HandleGitCmdListForPush(string input)
    {
        GitFetchTaut();

        tautManager.TautenHostRefs();

        ListRegainedRefs();

        Console.WriteLine();

        return HandleGitCommandResult.Done;
    }

    HandleGitCommandResult HandleGitCmdPush(string input)
    {
        const string functionName = nameof(HandleGitCmdPush);

        void HandleBatch()
        {
            tautManager.RegainHostRefs();

            foreach (var pushCmd in _pushBatch)
            {
                logger.ZLogTrace($"{functionName} '{pushCmd}'");

                var refSpecText = pushCmd[cmdPush.Length..].TrimStart();

                if (Lg2RefSpec.TryParseForPush(refSpecText, out var refSpec) == false)
                {
                    throw new InvalidOperationException($"Invalid refspec {refSpecText}");
                }

                var srcRefName = refSpec.GetSrc();

                var tauntenedSrcRefName = tautManager.TautenedRefSpec.TransformToTarget(srcRefName);

                var refSpecTextToUse = refSpec.ToString(replaceSrc: tauntenedSrcRefName);

                if (_options.DryRun)
                {
                    gitCli.Execute(
                        "--git-dir",
                        _tautRepoDir,
                        "push",
                        "--dry-run",
                        _remote,
                        refSpecTextToUse
                    );
                }
                else
                {
                    gitCli.Execute("--git-dir", _tautRepoDir, "push", _remote, refSpecTextToUse);
                }
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
            throw new InvalidOperationException(
                $"{nameof(HandleGitCmdPush)}: Invalid input '{input}'"
            );
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
    /// Act as a Git remote helper.
    /// </summary>
    /// <param name="remote">Remote name.</param>
    /// <param name="address">Remote address.</param>
    [Command("--remote-helper")]
    public async Task HandleGitCommandsAsync(
        [Argument] string remote,
        [Argument] string address,
        CancellationToken cancellationToken
    )
    {
        _remote = remote;
        _address = address;

        logger.ZLogTrace($"Run {ProgramInfo.CommandName} with '{remote}' and '{address}'");

        EnsureTautDir();

        for (; ; )
        {
            var input = await Console.In.ReadLineAsync(cancellationToken);

            if (input is null)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    logger.ZLogTrace($"{ProgramInfo.CommandName} cancelled");
                }
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
            throw new InvalidOperationException("Git dir is null");
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

        tautManager.Open(_tautRepoDir, newSetup: true);
    }

    void GitFetchTaut()
    {
        gitCli.Execute("--git-dir", _tautRepoDir, "fetch", _remote, "+refs/heads/*:refs/heads/*");

        logger.ZLogTrace($"Update '{_tautRepoDir}'");

        tautManager.Open(_tautRepoDir);
    }
}
