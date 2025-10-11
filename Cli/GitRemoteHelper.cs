using System.Diagnostics.CodeAnalysis;
using Lg2.Sharpy;
using Microsoft.Extensions.Logging;
using ZLogger;

namespace Git.Taut;

partial class GitRemoteHelper(
    ILogger<GitRemoteHelper> logger,
    TautSetup tautSetup,
    TautManager tautManager,
    GitCli gitCli
);

partial class GitRemoteHelper
{
    const string capPush = "push";
    const string capFetch = "fetch";
    const string capCheckConnectivity = "check-connectivity";
    const string capOption = "option";

    const string cmdCapabilities = "capabilities";
    const string cmdList = "list";
    const string cmdListForPush = "list for-push";
    const string cmdPush = "push";
    const string cmdFetch = "fetch";
    const string cmdOption = "option";

    readonly GitRemoteHelperOptions _options = new(logger);
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
    List<string> _fetchBatch = [];
    List<string> _pushBatch = [];

    internal event EventHandler? NotifyWorkWithGitDone;

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

    void RegainThenListRefs()
    {
        tautManager.RegainHostRefs();

        var refList = tautManager.OrdinaryTautRefs;
        foreach (var refName in refList)
        {
            var regainedRefName = tautManager.RegainedRefSpec.TransformToTarget(refName);
            Lg2Oid oid = new();
            tautManager.TautRepo.GetRefOid(regainedRefName, ref oid);
            var oidText = oid.ToHexDigits();

            logger.ZLogTrace($"{nameof(RegainThenListRefs)}: {oidText} {refName}");

            Console.WriteLine($"{oidText} {refName}");
        }
    }

    HandleGitCommandResult HandleGitCmdList(string input)
    {
        using var hostConfig = HostRepo.GetConfigSnapshot();

        var foundSiteName = TautSiteConfig.TryFindSiteName(
            hostConfig,
            _remoteName,
            out var tautSiteName
        );

        if (foundSiteName)
        {
            tautSetup.GearUpExisting(HostRepo, _remoteName, tautSiteName);

            RegainThenListRefs();
        }
        else
        {
            var cleanup = tautSetup.GearUpBrandNew(HostRepo, _remoteName, _remoteAddress);

            RegainThenListRefs();

            NotifyWorkWithGitDone += (_, _) =>
            {
                cleanup.RunSynchronously();
            };
        }

        Console.WriteLine();

        return HandleGitCommandResult.Done;
    }

    HandleGitCommandResult HandleGitCmdFetch(string input)
    {
        var functionName = nameof(HandleGitCmdFetch);

        void HandleBatch()
        {
            foreach (var fetchCmd in _fetchBatch)
            {
                var args = CmdFetchArgs.Parse(fetchCmd[cmdFetch.Length..].TrimStart());

                var regainedRefName = tautManager.RegainedRefSpec.TransformToTarget(args.Name);
                Lg2Oid oid = new();
                tautManager.TautRepo.GetRefOid(regainedRefName, ref oid);
                var regainedOidText = oid.ToHexDigits();

                if (args.Hash != regainedOidText)
                {
                    throw new InvalidOperationException(
                        $"{functionName}: requested {args.Hash} does not match regained {regainedOidText}"
                    );
                }
            }

            if (_options.CheckConnectivity)
            {
                Console.WriteLine("connectivity-ok");
            }
            Console.WriteLine();
        }

        if (input.Length == 0)
        {
            HandleBatch();

            return HandleGitCommandResult.Done;
        }
        else if (input.StartsWith(cmdFetch))
        {
            _fetchBatch.Add(input);

            return HandleGitCommandResult.Keep;
        }
        else
        {
            throw new InvalidOperationException(
                $"{nameof(HandleGitCmdFetch)}: Invalid input '{input}'"
            );
        }
    }

    HandleGitCommandResult HandleGitCmdListForPush(string input)
    {
        using var hostConfig = HostRepo.GetConfigSnapshot();

        var tautSiteName = TautSiteConfig.FindSiteName(hostConfig, RemoteName);

        tautSetup.GearUpExisting(HostRepo, RemoteName, tautSiteName);

        tautManager.TautenHostRefs();

        RegainThenListRefs();

        Console.WriteLine();

        return HandleGitCommandResult.Done;
    }

    HandleGitCommandResult HandleGitCmdPush(string input)
    {
        void HandleBatch()
        {
            using var config = HostRepo.GetConfigSnapshot();
            var tautSiteName = TautSiteConfig.FindSiteName(config, RemoteName);

            foreach (var pushCmd in _pushBatch)
            {
                logger.ZLogTrace($"{nameof(HandleGitCmdPush)} '{pushCmd}'");

                var refSpecText = pushCmd[cmdPush.Length..].TrimStart();

                if (Lg2RefSpec.TryParseForPush(refSpecText, out var refSpec) == false)
                {
                    throw new InvalidOperationException($"Invalid refspec {refSpecText}");
                }

                var tautSitePath = HostRepo.GetTautSitePath(tautSiteName);

                var srcRefName = refSpec.GetSrc();

                var tauntenedSrcRefName = tautManager.TautenedRefSpec.TransformToTarget(srcRefName);

                var refSpecTextToUse = refSpec.ToString(replaceSrc: tauntenedSrcRefName);

                if (_options.DryRun)
                {
                    gitCli.Execute(
                        "--git-dir",
                        tautSitePath,
                        "push",
                        "--dry-run",
                        _remoteName,
                        refSpecTextToUse
                    );
                }
                else
                {
                    gitCli.Execute(
                        "--git-dir",
                        tautSitePath,
                        "push",
                        _remoteName,
                        refSpecTextToUse
                    );
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
    [AllowNull]
    string _remoteName;

    string RemoteName => _remoteName!;

    [AllowNull]
    string _remoteAddress;

    string RemoteAddress => RemoteAddress!;

    [AllowNull]
    Lg2Repository _hostRepo;

    Lg2Repository HostRepo => _hostRepo!;

    [AllowNull]
    string _tautHomePath;

    public async Task WorkWithGitAsync(
        string remoteName,
        string remoteAddress,
        CancellationToken cancellationToken
    )
    {
        _remoteName = remoteName;
        _remoteAddress = remoteAddress;

        logger.ZLogTrace(
            $"Run {ProgramInfo.CommandName} with '{remoteName}' and '{remoteAddress}'"
        );

        EnsureTautHome();

        for (; ; )
        {
            var input = await Console.In.ReadLineAsync(cancellationToken);

            if (input is null)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    logger.ZLogWarning($"{nameof(WorkWithGitAsync)}: cancellation requested");
                }

                if (_handleGitCommand is not null)
                {
                    logger.ZLogWarning(
                        $"{nameof(WorkWithGitAsync)}: no input when there is pending command"
                    );
                }

                break;
            }

            logger.ZLogTrace($"Received git command '{input}'");

            _handleGitCommand ??= DispatchGitCommand;

            var result = _handleGitCommand(input);

            if (result == HandleGitCommandResult.Done)
            {
                _handleGitCommand = null;
            }
        }

        if (NotifyWorkWithGitDone is not null)
        {
            logger.ZLogTrace($"Begin event {nameof(NotifyWorkWithGitDone)}");

            NotifyWorkWithGitDone?.Invoke(this, EventArgs.Empty);

            logger.ZLogTrace($"End event {nameof(NotifyWorkWithGitDone)}");
        }

        logger.ZLogTrace($"Exit {nameof(WorkWithGitAsync)}");
    }

    void EnsureTautHome()
    {
        var gitDir =
            Environment.GetEnvironmentVariable(KnownEnvironVars.GitDir)
            ?? throw new InvalidOperationException($"{KnownEnvironVars.GitDir} is null");

        _hostRepo = Lg2Repository.New(gitDir);

        logger.ZLogTrace($"Host repo locates at '{gitDir}'");

        _tautHomePath = GitRepoHelpers.GetTautHomePath(gitDir);

        Directory.CreateDirectory(_tautHomePath);
    }
}
