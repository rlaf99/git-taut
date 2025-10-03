using System.Diagnostics.CodeAnalysis;
using System.Text;
using Lg2.Sharpy;
using Microsoft.Extensions.Logging;
using ZLogger;

namespace Git.Taut;

sealed class TautSetup(
    ILogger<TautSetup> logger,
    TautManager tautManager,
    TautMapping tautMapping,
    Aes256Cbc1 tautCipher,
    GitCli gitCli
) : IDisposable
{
    const string defaultDescription = $"Created by {ProgramInfo.CommandName}";

    [AllowNull]
    string _remoteName;

    internal string RemoteName => _remoteName!;

    [AllowNull]
    Lg2Repository _hostRepo;

    internal Lg2Repository HostRepo => _hostRepo!;

    [AllowNull]
    Lg2Repository _tautRepo;

    internal Lg2Repository TautRepo => _tautRepo!;

    UserKeyHolder _keyHolder = new();

    internal UserKeyHolder KeyHolder => _keyHolder;

    internal string KeyValueStoreLocation => _tautRepo.GetObjectInfoDirPath();

    [AllowNull]
    TautConfig? _tautCfg;

    TautConfig TautCfg => _tautCfg!;

    bool _gearedUp;

    internal bool Initialized => _gearedUp;

    internal void EnsureInitialized()
    {
        ThrowHelper.InvalidOperationIfNotInitialized(_gearedUp, nameof(TautSetup));
    }

    internal void EnsureNotGearedUp()
    {
        if (_gearedUp)
        {
            throw new InvalidOperationException($"Already geared up");
        }
        _gearedUp = true;
    }

    internal Task GearUpBrandNew(
        Lg2Repository hostRepo,
        string remoteName,
        string remoteAddress,
        string? tautRepoNameToLink = null
    )
    {
        EnsureNotGearedUp();

        _hostRepo = hostRepo;
        _remoteName = remoteName;

        _tautCfg = new(Path.GetRandomFileName(), tautRepoNameToLink);

        EnsureHostOidType();

        EnsureBrandNewSetup(remoteAddress);

        tautCipher.Init(KeyHolder);
        tautMapping.Init(KeyValueStoreLocation);
        tautManager.Init(HostRepo, TautRepo);

        var result = new Task(WrapUpBrandNew);

        return result;
    }

    internal void GearUpExisting(Lg2Repository hostRepo, string remoteName, string tautRepoName)
    {
        EnsureNotGearedUp();

        _hostRepo = hostRepo;
        _remoteName = remoteName;

        _tautCfg = new(tautRepoName);

        EnsureHostOidType();

        EnsureExistingSetup();

        tautCipher.Init(KeyHolder);
        tautMapping.Init(KeyValueStoreLocation);
        tautManager.Init(HostRepo, TautRepo);
    }

    void EnsureHostOidType()
    {
        if (_hostRepo.GetOidType() != Lg2OidType.LG2_OID_SHA1)
        {
            var name = Enum.GetName(Lg2OidType.LG2_OID_SHA1);
            throw new InvalidOperationException($"Only oid type {name} is supported");
        }
    }

    void EnsureBrandNewSetup(string remoteAddress)
    {
        TautCfg.RemoteNames.Add(RemoteName);

        var tautRepoPath = HostRepo.GetTautRepoPath(TautCfg.TautRepoName);

        List<string> argList =
        [
            "clone",
            "--no-local",
            "--bare",
            "--origin",
            RemoteName,
            "--reference",
            HostRepo.GetPath(),
        ];

        if (TautCfg.LinkTo is not null)
        {
            using (var config = HostRepo.GetConfig())
            {
                TautCfg.LinkTo.Load(config);
            }

            var tautRepoPathToLink = HostRepo.GetTautRepoPath(TautCfg.LinkTo.TautRepoName);

            argList.Add("--reference");
            argList.Add(tautRepoPathToLink);

            logger.ZLogTrace($"{TautCfg.TautRepoName} is linked to {TautCfg.LinkTo.TautRepoName}");
        }

        argList.Add(remoteAddress);
        argList.Add(tautRepoPath);

        gitCli.Execute(argList.ToArray());

        logger.ZLogTrace($"Cloned '{RemoteName}' from '{remoteAddress}' into '{tautRepoPath}'");

        _tautRepo = Lg2Repository.New(tautRepoPath);

        TautSetDescription();
        TautSetFetchConfig();
        // TautAddHostObjects();
        UpdateRemoteUrls();
        UpdateTautConfig();
    }

    void TautSetDescription()
    {
        var descriptionFile = GitRepoExtra.GetDescriptionFile(_tautRepo);

        File.Delete(descriptionFile);

        using (var writer = File.AppendText(descriptionFile))
        {
            writer.NewLine = "\n";
            writer.WriteLine(defaultDescription);
        }

        logger.ZLogTrace($"Wrote '{defaultDescription}' to '{descriptionFile}'");
    }

    // Not used, as `--reference repo` is used when cloning.
    void TautAddHostObjects()
    {
        var tautRepoObjectsDir = GitRepoExtra.GetObjectDirPath(_tautRepo);
        var tautRepoObjectsInfoAlternatesFile = GitRepoExtra.GetObjectsInfoAlternatesFilePath(
            _tautRepo
        );
        var hostRepoObjectsDir = GitRepoExtra.GetObjectDirPath(_hostRepo);

        var relativePath = Path.GetRelativePath(tautRepoObjectsDir, hostRepoObjectsDir);
        relativePath = GitRepoExtra.UseForwardSlash(relativePath);

        using (var writer = File.AppendText(tautRepoObjectsInfoAlternatesFile))
        {
            writer.NewLine = "\n";
            writer.WriteLine(relativePath);
        }

        logger.ZLogTrace($"Wrote '{relativePath}' to '{tautRepoObjectsInfoAlternatesFile}'");
    }

    void TautSetFetchConfig()
    {
        using var config = _tautRepo.GetConfig();
        config.SetString(GitConfigExtra.Fetch_Prune, "true");
    }

    void UpdateRemoteUrls()
    {
        using (var remote = TautRepo.LookupRemote(RemoteName))
        {
            var remoteUrl = remote.GetUrl();
            var remoteUri = new Uri(remoteUrl);

            // normalize the remote's file path
            TautRepo.SetRemoteUrl(RemoteName, remoteUri.AbsolutePath);

            var hostRemoteUrl = GitRepoExtra.AddTautRemoteHelperPrefix(remoteUri.AbsoluteUri);
            _hostRepo.SetRemoteUrl(RemoteName, hostRemoteUrl);
        }
    }

    void UpdateTautConfig()
    {
        using var hostConfig = HostRepo.GetConfig();

        TautCfg.SaveRemotes(hostConfig);

        if (TautCfg.LinkTo is not null)
        {
            CheckCredentialKeyTrait(TautCfg.LinkTo);

            return;
        }

        var tautRepoPath = HostRepo.GetTautRepoPath(TautCfg.TautRepoName);
        var gitCredUrl = GitRepoExtra.ConvertPathToTautCredentialUrl(tautRepoPath);

        TautCfg.CredentialUrl = gitCredUrl;

        TautCfg.SaveCredentialUrl(hostConfig);

        using (var gitCred = new GitCredential(gitCli, gitCredUrl))
        {
            gitCred.Fill();

            byte[] passwordSalt = [];

            if (string.IsNullOrEmpty(gitCred.UserName) == false)
            {
                TautCfg.CredentialUserName = gitCred.UserName;

                passwordSalt = Encoding.UTF8.GetBytes(gitCred.UserName);
            }

            _keyHolder.DeriveCrudeKey(gitCred.PasswordData, passwordSalt);

            var infoData = Encoding.ASCII.GetBytes(gitCredUrl);
            var keyTrait = _keyHolder.DeriveCredentialKeyTrait(infoData);

            TautCfg.CredentialKeyTrait = keyTrait;

            gitCred.Approve();
        }
    }

    void EnsureExistingSetup()
    {
        using (var config = HostRepo.GetConfig())
        {
            TautCfg.Load(config);
        }

        var tautRepoPath = HostRepo.GetTautRepoPath(TautCfg.TautRepoName);
        _tautRepo = Lg2Repository.New(tautRepoPath);

        gitCli.Execute(
            "--git-dir",
            tautRepoPath,
            "fetch",
            RemoteName,
            "+refs/heads/*:refs/heads/*"
        );

        logger.ZLogTrace($"Fetched '{RemoteName}' for '{TautCfg.TautRepoName}'");

        var tautRemote = _tautRepo.LookupRemote(RemoteName);
        var tautRemoteUrl = tautRemote.GetUrl();
        var tautRemoteUri = new Uri(tautRemoteUrl);

        var hostRemote = _hostRepo.LookupRemote(RemoteName);
        var hostRemoteUrl = hostRemote.GetUrl();
        hostRemoteUrl = GitRepoExtra.RemoveTautRemoteHelperPrefix(hostRemoteUrl);
        var hostRemoteUri = new Uri(hostRemoteUrl);

        if (hostRemoteUri.IsFile)
        {
            if (hostRemoteUri.AbsolutePath != tautRemoteUri.AbsolutePath)
            {
                throw new InvalidOperationException(
                    $"host remote '{_remoteName}':'{hostRemoteUri.AbsolutePath}'"
                        + $" and taut remote '{_remoteName}':'{tautRemoteUri.AbsolutePath}'"
                        + " do not have the same path"
                );
            }
        }

        CheckCredentialKeyTrait(TautCfg);
    }

    void CheckCredentialKeyTrait(TautConfig tautConfig)
    {
        tautConfig.EnsureValues();

        using (var gitCred = new GitCredential(gitCli, tautConfig.CredentialUrl))
        {
            gitCred.Fill();

            byte[] passwordSalt = [];

            if (string.IsNullOrEmpty(tautConfig.CredentialUserName) == false)
            {
                passwordSalt = Encoding.UTF8.GetBytes(tautConfig.CredentialUserName);
            }

            _keyHolder.DeriveCrudeKey(gitCred.PasswordData, passwordSalt);

            var credUrlData = Encoding.ASCII.GetBytes(tautConfig.CredentialUrl);
            var keyTrait = _keyHolder.DeriveCredentialKeyTrait(credUrlData);

            if (keyTrait.SequenceEqual(tautConfig.CredentialKeyTrait) == false)
            {
                gitCred.Reject();

                throw new InvalidOperationException(
                    $"The credential for ${tautConfig.CredentialUrl} does not match the existing one"
                );
            }

            gitCred.Approve();
        }
    }

    internal void CloseHostRepo()
    {
        _hostRepo?.Dispose();
        _hostRepo = null;
    }

    internal void CloseTautRepo()
    {
        _tautRepo?.Dispose();
        _tautRepo = null;
    }

    internal void WrapUpBrandNew()
    {
        using (var config = HostRepo.GetConfig())
        {
            if (TautCfg.LinkTo is not null)
            {
                TautCfg.SaveLinkTo(config);
            }
            else
            {
                TautCfg.SaveCredentialPair(config);
            }
        }

        logger.ZLogTrace($"Exit {nameof(WrapUpBrandNew)}");
    }

    bool _disposed;

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }
        _disposed = true;

        tautMapping.Dispose();
        CloseTautRepo();
        CloseHostRepo();
    }
}
