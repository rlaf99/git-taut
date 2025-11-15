using System.Diagnostics.CodeAnalysis;
using Lg2.Sharpy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ZLogger;

namespace Git.Taut;

partial class GitRemoteHelper(
    ILogger<GitRemoteHelper> logger,
    IConfiguration config,
    TautSetup tautSetup,
    TautManager tautManager,
    GitCli gitCli
);

static class ILoggerExtensions
{
    internal static void SendLineToGit<TCategory>(
        this ILogger<TCategory> logger,
        string? content = null
    )
        where TCategory : GitRemoteHelper
    {
        content ??= string.Empty;

        logger.ZLogTrace($"Send to Git '{content}'");

        Console.WriteLine(content);
    }
}

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
            logger.SendLineToGit();

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
        logger.SendLineToGit(capPush);
        logger.SendLineToGit(capFetch);
        logger.SendLineToGit(capCheckConnectivity);
        logger.SendLineToGit(capOption);
        logger.SendLineToGit();

        return HandleGitCommandResult.Done;
    }

    void RegainThenListRefs()
    {
        tautManager.RegainRefHeads();
        tautManager.RegainRefTags();

        if (tautManager.TautRepo.TryLookupRef(GitRepoHelpers.HEAD, out var headRef))
        {
            var headRefName = headRef.GetName();
            var headRefType = headRef.GetRefType();

            if (headRefType == Lg2RefType.LG2_REFERENCE_SYMBOLIC)
            {
                var symTarget = headRef.GetSymbolicTarget();

                logger.SendLineToGit($"@{symTarget} {headRefName}");
            }
            else
            {
                var target = headRef.GetTarget();
                var oidText = target.GetOidHexDigits();

                logger.SendLineToGit($"{oidText} {headRefName}");
            }
        }

        var refList = tautManager.TautRepo.GetRefList();

        var refHeads = GitRefSpecs.FilterLocalRefHeads(refList);

        foreach (var refHead in refHeads)
        {
            var regainedRefHead = GitRefSpecs.RefsToRefsRegained.TransformToTarget(refHead);

            Lg2Oid oid = new();
            tautManager.TautRepo.GetRefOid(regainedRefHead, ref oid);
            var oidText = oid.GetOidHexDigits();

            logger.SendLineToGit($"{oidText} {refHead}");
        }

        var refTags = GitRefSpecs.FilterLocalRefTags(refList);

        foreach (var refTag in refTags)
        {
            var regainedRefTag = GitRefSpecs.RefsToRefsRegained.TransformToTarget(refTag);

            Lg2Oid oid = new();
            tautManager.TautRepo.GetRefOid(regainedRefTag, ref oid);
            var oidText = oid.GetOidHexDigits();

            logger.SendLineToGit($"{oidText} {refTag}");
        }
    }

    HandleGitCommandResult HandleGitCmdList(string input)
    {
        using var hostConfig = HostRepo.GetConfigSnapshot();

        if (TautSiteConfig.TryFindSiteNameForRemote(hostConfig, _remoteName, out var tautSiteName))
        {
            tautSetup.GearUpExisting(HostRepo, _remoteName, tautSiteName);

            tautSetup.FetchRemote();

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

        logger.SendLineToGit();

        return HandleGitCommandResult.Done;
    }

    HandleGitCommandResult HandleGitCmdFetch(string input)
    {
        void HandleBatch()
        {
            foreach (var fetchCmd in _fetchBatch)
            {
                logger.ZLogTrace($"Handle '{fetchCmd}'");

                var args = CmdFetchArgs.Parse(fetchCmd[cmdFetch.Length..].TrimStart());

                Lg2Oid oid = new();

                if (args.Name == GitRepoHelpers.HEAD)
                {
                    tautManager.TautRepo.GetRefOid(args.Name, ref oid);
                }
                else
                {
                    var regainedRefName = GitRefSpecs.RefsToRefsRegained.TransformToTarget(
                        args.Name
                    );
                    tautManager.TautRepo.GetRefOid(regainedRefName, ref oid);
                }

                // Next: fetch from kv store and translate it

                var oidText = oid.GetOidHexDigits();

                if (args.Hash != oidText)
                {
                    throw new InvalidOperationException(
                        $"Requested hash '{args.Hash}' for '{args.Name}' does not match actual hash '{oidText}'"
                    );
                }
            }

            if (_options.CheckConnectivity)
            {
                logger.SendLineToGit("connectivity-ok");
            }

            logger.SendLineToGit();
        }

        if (input.Length == 0)
        {
            try
            {
                HandleBatch();
            }
            finally
            {
                _fetchBatch.Clear();
            }

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

        var tautSiteName = TautSiteConfig.FindSiteNameForRemote(hostConfig, RemoteName);

        tautSetup.GearUpExisting(HostRepo, RemoteName, tautSiteName);

        tautManager.TautenRefHeads();
        tautManager.TautenRefTags();

        bool noFetch = config.GetGitListForPushNoFetch();
        if (noFetch)
        {
            logger.ZLogTrace(
                $"Skip fetching {_remoteName} as {KnownEnvironVars.GitListForPushNoFetch} is set"
            );
        }
        else
        {
            tautSetup.FetchRemote();
        }

        RegainThenListRefs();

        logger.SendLineToGit();

        return HandleGitCommandResult.Done;
    }

    HandleGitCommandResult HandleGitCmdPush(string input)
    {
        void HandleBatch()
        {
            using var config = HostRepo.GetConfigSnapshot();
            var tautSiteName = TautSiteConfig.FindSiteNameForRemote(config, RemoteName);

            foreach (var pushCmd in _pushBatch)
            {
                logger.ZLogTrace($"Handle '{pushCmd}'");

                var refSpecText = pushCmd[cmdPush.Length..].TrimStart();

                if (Lg2RefSpec.TryParseForPush(refSpecText, out var refSpec) == false)
                {
                    throw new InvalidOperationException($"Invalid refspec {refSpecText}");
                }

                var tautSitePath = HostRepo.GetTautSitePath(tautSiteName);

                var srcRefName = refSpec.GetSrc();
                var dstRefName = refSpec.GetDst();

                var tauntenedSrcRefName = GitRefSpecs.RefsToRefsTautened.TransformToTarget(
                    srcRefName
                );

                var refSpecTextToUse = refSpec.ToString(replaceSrc: tauntenedSrcRefName);

                string[] dryRunOpt = _options.DryRun ? ["--dry-run"] : [];
                string[] args =
                [
                    "--git-dir",
                    tautSitePath,
                    "push",
                    .. dryRunOpt,
                    _remoteName,
                    refSpecTextToUse,
                ];

                gitCli.Execute(args);

                if (_options.DryRun == false)
                {
                    var tauntenedSrcRef = tautManager.TautRepo.LookupRef(tauntenedSrcRefName);
                    var tauntenedSrcRefTarget = tauntenedSrcRef.GetTarget();

                    tautManager.TautRepo.NewRef(srcRefName, tauntenedSrcRefTarget, force: true);

                    logger.ZLogTrace(
                        $"Refer '{srcRefName}' to '{tauntenedSrcRefTarget.GetOidHexDigits()}'"
                    );
                }

                logger.SendLineToGit($"ok {dstRefName}");
            }

            logger.SendLineToGit();
        }

        if (input.Length == 0)
        {
            try
            {
                HandleBatch();
            }
            finally
            {
                _pushBatch.Clear();
            }

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

            logger.ZLogTrace($"Received from Git '{input}'");

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
