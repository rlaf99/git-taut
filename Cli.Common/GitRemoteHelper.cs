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

class GitRemoteHelperOptions
{
    internal int Verbosity = 1;

    internal bool ShowProgress = false;

    internal bool IsCloning = false;

    internal bool CheckConnectivity = false;

    internal bool ForceUpdate = false;

    internal bool DryRun = false;
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

    const string optVerbosity = "verbosity";
    const string optProgress = "progress";
    const string optCloning = "cloning";
    const string optCheckConnectivity = "check-connectivity";
    const string optForce = "force";
    const string optDryRun = "dry-run";

    readonly GitRemoteHelperOptions _options = new();

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

    void SendLineToGit(string? content = null)
    {
        content ??= string.Empty;

        logger.ZLogTrace($"Send to Git '{content}'");

        Console.WriteLine(content);
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
            SendLineToGit();

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
        SendLineToGit(capPush);
        SendLineToGit(capFetch);
        SendLineToGit(capCheckConnectivity);
        SendLineToGit(capOption);
        SendLineToGit();

        return HandleGitCommandResult.Done;
    }

    void RegainThenListRefs()
    {
        tautManager.RegainHeads();
        tautManager.RegainTags();

        if (tautManager.TautRepo.TryLookupRef(GitRepoHelpers.HEAD, out var headRef))
        {
            var headRefName = headRef.GetName();
            var headRefType = headRef.GetRefType();

            if (headRefType.IsSymbolic())
            {
                var symTarget = headRef.GetSymbolicTarget();

                SendLineToGit($"@{symTarget} {headRefName}");
            }
            else
            {
                var target = headRef.GetTarget();
                var oidText = target.GetOidHexDigits();

                SendLineToGit($"{oidText} {headRefName}");
            }
        }

        var refNames = tautManager.TautRepo.GetRefNames();

        var refHeads = GitRefSpecs.FilterLocalRefHeads(refNames);

        foreach (var refHead in refHeads)
        {
            var regainedRefHead = GitRefSpecs.RefsToRefsRegained.TransformToTarget(refHead);

            Lg2Oid oid = new();
            tautManager.TautRepo.GetRefOid(regainedRefHead, ref oid);
            var oidText = oid.GetOidHexDigits();

            SendLineToGit($"{oidText} {refHead}");
        }

        var refTags = GitRefSpecs.FilterLocalRefTags(refNames);

        foreach (var refTag in refTags)
        {
            var regainedRefTag = GitRefSpecs.RefsToRefsRegained.TransformToTarget(refTag);

            Lg2Oid oid = new();
            tautManager.TautRepo.GetRefOid(regainedRefTag, ref oid);
            var oidText = oid.GetOidHexDigits();

            SendLineToGit($"{oidText} {refTag}");
        }
    }

    HandleGitCommandResult HandleGitCmdList(string input)
    {
        if (HostRepo.TryFindTautSiteNameForRemote(RemoteName, out var tautSiteName))
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

        SendLineToGit();

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
                SendLineToGit("connectivity-ok");
            }

            SendLineToGit();
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
        var tautSiteName = HostRepo.FindTautSiteNameForRemote(RemoteName);

        tautSetup.GearUpExisting(HostRepo, RemoteName, tautSiteName);

        tautManager.TautenHeads();
        tautManager.TautenTags();
        tautManager.TautenHostHead();

        bool noFetch = config.GetGitListForPushNoFetch();
        if (noFetch)
        {
            logger.ZLogTrace(
                $"Skip fetching {_remoteName} as {AppEnvironment.GitListForPushNoFetch} is set"
            );
        }
        else
        {
            tautSetup.FetchRemote();
        }

        RegainThenListRefs();

        SendLineToGit();

        return HandleGitCommandResult.Done;
    }

    HandleGitCommandResult HandleGitCmdPush(string input)
    {
        void HandleBatch()
        {
            var tautSiteName = HostRepo.FindTautSiteNameForRemote(RemoteName);

            List<(string srcRefName, string tauntenedSrcRefName, string dstRefName)> parsedRefS =
                new(_pushBatch.Count);
            List<string> pushRefSpecs = new(_pushBatch.Count);

            foreach (var pushCmd in _pushBatch)
            {
                logger.ZLogTrace($"Handle '{pushCmd}'");

                var refSpecText = pushCmd[cmdPush.Length..].TrimStart();

                if (Lg2RefSpec.TryParseForPush(refSpecText, out var refSpec) == false)
                {
                    throw new InvalidOperationException($"Invalid refspec {refSpecText}");
                }

                var srcRefName = refSpec.GetSrc();
                var dstRefName = refSpec.GetDst();

                var tauntenedSrcRefName = GitRefSpecs.RefsToRefsTautened.TransformToTarget(
                    srcRefName
                );

                parsedRefS.Add((srcRefName, tauntenedSrcRefName, dstRefName));

                var pushRefSpec = refSpec.ToString(replaceSrc: tauntenedSrcRefName);

                pushRefSpecs.Add(pushRefSpec);
            }

            var tautSitePath = HostRepo.GetTautSitePath(tautSiteName);

            string[] dryRunOpt = _options.DryRun ? ["--dry-run"] : [];
            string[] args =
            [
                "--git-dir",
                tautSitePath,
                "push",
                .. dryRunOpt,
                _remoteName,
                .. pushRefSpecs,
            ];

            gitCli.Execute(args);

            foreach (var parsedRef in parsedRefS)
            {
                if (_options.DryRun == false)
                {
                    var tauntenedSrcRef = tautManager.TautRepo.LookupRef(
                        parsedRef.tauntenedSrcRefName
                    );

                    var tauntenedSrcRefTarget = tauntenedSrcRef.GetTarget();

                    tautManager.TautRepo.NewRef(
                        parsedRef.srcRefName,
                        tauntenedSrcRefTarget,
                        force: true
                    );

                    logger.ZLogTrace(
                        $"Set taut site ref '{parsedRef.srcRefName}' to '{tauntenedSrcRefTarget.GetOidHexDigits()}'"
                    );
                }

                SendLineToGit($"ok {parsedRef.dstRefName}");
            }

            tautManager.UpdateTautSiteHead();

            SendLineToGit();
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

        bool GetBooleanValue(string opt)
        {
            return nameValue[(opt.Length + 1)..] == "true";
        }

        void TraceOptionUpdate(string opt, string value)
        {
            logger.ZLogTrace($"Set option '{opt}' to {value}");
        }

        if (nameValue.StartsWith(optVerbosity))
        {
            if (int.TryParse(nameValue[(optVerbosity.Length + 1)..], out var value))
            {
                _options.Verbosity = value;

                SendLineToGit("ok");

                TraceOptionUpdate(optVerbosity, value.ToString());
            }
            else
            {
                Console.WriteLine("error falied to parse the value");
            }
        }
        else if (nameValue.StartsWith(optProgress))
        {
            _options.ShowProgress = GetBooleanValue(optProgress);

            SendLineToGit("ok");

            TraceOptionUpdate(optProgress, _options.ShowProgress.ToString());
        }
        else if (nameValue.StartsWith(optCloning))
        {
            _options.IsCloning = GetBooleanValue(optCloning);

            SendLineToGit("ok");

            TraceOptionUpdate(optCloning, _options.IsCloning.ToString());
        }
        else if (nameValue.StartsWith(optCheckConnectivity))
        {
            _options.CheckConnectivity = GetBooleanValue(optCheckConnectivity);

            SendLineToGit("ok");

            TraceOptionUpdate(optCheckConnectivity, _options.CheckConnectivity.ToString());
        }
        else if (nameValue.StartsWith(optForce))
        {
            _options.ForceUpdate = GetBooleanValue(optForce);

            SendLineToGit("ok");

            TraceOptionUpdate(optForce, _options.ForceUpdate.ToString());
        }
        else if (nameValue.StartsWith(optDryRun))
        {
            _options.DryRun = GetBooleanValue(optDryRun);

            SendLineToGit("ok");

            TraceOptionUpdate(optDryRun, _options.DryRun.ToString());
        }
        else
        {
            SendLineToGit("unsupported");
        }

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
            $"Run {AppInfo.GitRemoteTautCommandName} with '{remoteName}' and '{remoteAddress}'"
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
            Environment.GetEnvironmentVariable(AppEnvironment.GitDir)
            ?? throw new InvalidOperationException($"{AppEnvironment.GitDir} is null");

        _hostRepo = Lg2Repository.New(gitDir);

        logger.ZLogTrace($"Host repo locates at '{gitDir}'");

        _tautHomePath = GitRepoHelpers.GetTautHomePath(gitDir);

        Directory.CreateDirectory(_tautHomePath);
    }
}
