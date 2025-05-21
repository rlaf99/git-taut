using System.Diagnostics.CodeAnalysis;
using ConsoleAppFramework;
using Lg2.Sharpy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ZLogger;

namespace Git.Remote.Taut;

static class KnownEnvironVars
{
    internal const string GitDir = "GIT_DIR";

    internal const string GitRemoteTautTrace = "GIT_REMOTE_TAUT_TRACE";

    internal const string GitAlternateObjectDirectories = "GIT_ALTERNATE_OBJECT_DIRECTORIES";
}

static class GitRepoLayout
{
    internal const string ObjectsDir = "Objects";
    internal static readonly string ObjectsInfoDir = Path.Join(ObjectsDir, "info");
    internal static readonly string ObjectsInfoAlternatesFile = Path.Join(
        ObjectsInfoDir,
        "alternates"
    );

    internal const string Description = "description";
    internal const string DefaultDescriptionForTautRepo = "git-remote-taut";
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

partial class GitRemoteHelper(GitCli gitCli, ILogger<GitRemoteHelper> logger)
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

    const string objectsDirname = "objects";
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
    string _hostRepoObjectsDir = default!;
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

        _hostRepoObjectsDir = Path.Join(gitDir, objectsDirname);

        var tautDir = Path.Join(_hostRepoDir, tautDirName);
        Directory.CreateDirectory(tautDir);

        logger.ZLogTrace($"Taut dir localtion '{tautDir}'");

        _tautRepoDir = tautDir;
    }

    void RepoSetAlternateObjects()
    {
        var tautRepoObjectsDir = Path.Join(_tautRepoDir, GitRepoLayout.ObjectsDir);

        var tautRepoObjectsInfoAlternatesFile = Path.Join(
            _tautRepoDir,
            GitRepoLayout.ObjectsInfoAlternatesFile
        );

        var relPathToHostObjects = Path.GetRelativePath(tautRepoObjectsDir, _hostRepoObjectsDir);

        using (var writer = File.AppendText(tautRepoObjectsInfoAlternatesFile))
        {
            writer.NewLine = "\n";
            writer.WriteLine(relPathToHostObjects);
        }

        logger.ZLogTrace(
            $"Append '{relPathToHostObjects}' to '{tautRepoObjectsInfoAlternatesFile}'"
        );
    }

    void RepoSetDescription()
    {
        var tautRepoDescriptionFile = Path.Join(_tautRepoDir, GitRepoLayout.Description);

        File.Delete(tautRepoDescriptionFile);

        using (var writer = File.AppendText(tautRepoDescriptionFile))
        {
            writer.NewLine = "\n";
            writer.WriteLine(GitRepoLayout.DefaultDescriptionForTautRepo);
        }

        logger.ZLogTrace(
            $"Write '{GitRepoLayout.DefaultDescriptionForTautRepo}' to '{tautRepoDescriptionFile}'"
        );
    }

    void CloneRemoteIntoTaut()
    {
        gitCli.Execute("clone", "--bare", _address, _tautRepoDir);

        RepoSetAlternateObjects();
        RepoSetDescription();
    }

    void InitializeTautRepo()
    {
        void RunGitInit()
        {
            gitCli.Execute("--git-dir", _tautRepoDir, "init", "--bare");

            logger.ZLogTrace($"Initialized taut repo at '{_tautRepoDir}'");
        }

        RunGitInit();

        void SetRemote()
        {
            gitCli.Execute("--git-dir", _tautRepoDir, "remote", "add", _remote, _address);

            logger.ZLogTrace($"Added remote '{_remote}' = '{_address}'");
        }

        SetRemote();

        RepoSetAlternateObjects();

        void RunGitFetch()
        {
            gitCli.Execute("--git-dir", _tautRepoDir, "fetch", "--all");
        }

        RunGitFetch();
    }

    void TautRepoConfigHostRepoRefs(Lg2Repository tautRepo)
    {
        using var lg2Config = tautRepo.GetConfig();
        lg2Config.SetString("hostRepo.refs", "dummy");
    }

    void TautRepoMapObjectsToHostRepo(Lg2Repository tautRepo)
    {
        using var lg2RevWalk = tautRepo.NewRevWalk();
    }

    void TautRepoTransferCommonObjectsToHost(Lg2Repository tautRepo)
    {
        using var hostRepoOdb = Lg2Odb.Open(_hostRepoObjectsDir);

        using var revWalk = tautRepo.NewRevWalk();

        var refList = tautRepo.GetRefList();
        foreach (var refName in refList)
        {
            revWalk.PushRef(refName);
        }

        Lg2Oid oid = new();

        while (revWalk.Next(ref oid))
        {
            logger.ZLogDebug($"oid: {oid.ToString()}");

            var commit = tautRepo.LookupCommit(ref oid);

            logger.ZLogDebug($"commit summary: {commit.GetSummary()}");
        }
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

            var tautRepo = Lg2Repository.Open(_tautRepoDir);

            var refList = tautRepo.GetRefList();

            foreach (var refName in refList)
            {
                logger.ZLogDebug($"ref: {refName}");
            }

            TautRepoConfigHostRepoRefs(tautRepo);

            TautRepoMapObjectsToHostRepo(tautRepo);

            TautRepoTransferCommonObjectsToHost(tautRepo);

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
