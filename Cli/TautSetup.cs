using System.Diagnostics.CodeAnalysis;
using System.Text;
using Lg2.Sharpy;
using Microsoft.Extensions.Logging;
using ZLogger;

namespace Git.Taut;

class TautSetup(
    ILogger<TautSetup> logger,
    TautManager tautManager,
    TautMapping tautMapping,
    Aes256Cbc1 tautCipher,
    GitCli gitCli
)
{
    const string defaultDescription = $"Created by {ProgramInfo.CommandName}";

    [AllowNull]
    string _remoteName;

    internal string RemoteName => _remoteName!;

    [AllowNull]
    Lg2Repository _hostRepo;

    internal Lg2Repository HostRepo => _hostRepo!;

    [AllowNull]
    string _tautRepoName;

    internal string TautRepoName => _tautRepoName!;

    [AllowNull]
    Lg2Repository _tautRepo;

    internal Lg2Repository TautRepo => _tautRepo!;

    UserKeyHolder _keyHolder = new();

    internal UserKeyHolder KeyHolder => _keyHolder;

    internal string KeyValueStoreLocation => _tautRepo.GetObjectInfoDirPath();

    TautConfig _tautConfig = new();

    bool _initialized;

    internal bool Initialized => _initialized;

    internal void EnsureInitialized()
    {
        ThrowHelper.InvalidOperationIfNotInitialized(_initialized, nameof(TautSetup));
    }

    internal void InitBrandNew(Lg2Repository hostRepo, string remoteName, string remoteAddress)
    {
        ThrowHelper.InvalidOperationIfAlreadyInitalized(_initialized);

        _initialized = true;

        _hostRepo = hostRepo;
        _remoteName = remoteName;

        EnsureHostOidType();

        EnsureBrandNewSetup(remoteAddress);

        tautCipher.Init(KeyHolder);
        tautMapping.Init(KeyValueStoreLocation);
        tautManager.Init(remoteName, HostRepo, TautRepo);
    }

    internal void InitExisting(Lg2Repository hostRepo, string remoteName, string tautRepoName)
    {
        ThrowHelper.InvalidOperationIfAlreadyInitalized(_initialized);

        _initialized = true;

        _hostRepo = hostRepo;
        _remoteName = remoteName;

        EnsureHostOidType();

        EnsureExistingSetup(tautRepoName);

        tautCipher.Init(KeyHolder);
        tautMapping.Init(KeyValueStoreLocation);
        tautManager.Init(remoteName, HostRepo, TautRepo);
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
        _tautConfig.RemoteNames.Add(RemoteName);
        _tautConfig.TautRepoName = Path.GetRandomFileName();

        var tautRepoTempName = GitRepoExtra.AddTautRepoNameTempPrefix(_tautConfig.TautRepoName);
        var tautRepoTempPath = GitRepoExtra.GetTautRepoPath(HostRepo.GetPath(), tautRepoTempName); // HostRepo.GetTautRepoPath(tautRepoTempName);

        gitCli.Execute("clone", "--bare", "--origin", RemoteName, remoteAddress, tautRepoTempPath);

        logger.ZLogTrace($"Cloned '{RemoteName}' from '{remoteAddress}' into '{tautRepoTempPath}'");

        _tautRepoName = tautRepoTempName;
        _tautRepo = Lg2Repository.New(tautRepoTempPath);

        TautSetDescription();
        TautAddHostObjects();
        TautSetRemote();
        TautSetConfig();
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

    void TautSetRemote()
    {
        using (var remote = _tautRepo.LookupRemote(_remoteName))
        {
            var remoteUrl = remote.GetUrl();
            var remoteUri = new Uri(remoteUrl);

            // normalize the remote's file path
            _tautRepo.SetRemoteUrl(_remoteName, remoteUri.AbsolutePath);

            var gitCredUri = GitRepoExtra.ConvertToCredentialUri(remoteUri);
            var gitCredUrl = gitCredUri.AbsoluteUri;

            HostSetRemote(remoteUri, gitCredUrl);
        }
    }

    void HostSetRemote(Uri tautRemoteUri, string gitCredUrl)
    {
        var hostRemoteUrl = GitRepoExtra.AddTautRemoteHelperPrefix(tautRemoteUri.AbsoluteUri);
        _hostRepo.SetRemoteUrl(_remoteName, hostRemoteUrl);

        _tautConfig.CredentialUrl = gitCredUrl;

        using (var gitCred = new GitCredential(gitCli, gitCredUrl))
        {
            gitCred.Fill();

            byte[] passwordSalt = [];

            if (string.IsNullOrEmpty(gitCred.UserName) == false)
            {
                _tautConfig.CredentialUserName = gitCred.UserName;

                passwordSalt = Encoding.UTF8.GetBytes(gitCred.UserName);
            }

            _keyHolder.DeriveCrudeKey(gitCred.PasswordData, passwordSalt);

            var infoData = Encoding.ASCII.GetBytes(gitCredUrl);
            var keyTrait = _keyHolder.DeriveCredentialKeyTrait(infoData);

            _tautConfig.CredentialKeyTrait = keyTrait;

            gitCred.Approve();
        }
    }

    void TautSetConfig()
    {
        using var config = _tautRepo.GetConfig();
        config.SetString(GitConfigExtra.Fetch_Prune, "true");
    }

    void EnsureExistingSetup(string tautRepoName)
    {
        using (var config = HostRepo.GetConfig())
        {
            _tautConfig.TautRepoName = tautRepoName;
            _tautConfig.Load(config);
        }

        var tautRepoPath = HostRepo.GetTautRepoPath(tautRepoName);
        _tautRepo = Lg2Repository.New(tautRepoPath);

        gitCli.Execute(
            "--git-dir",
            tautRepoPath,
            "fetch",
            RemoteName,
            "+refs/heads/*:refs/heads/*"
        );

        logger.ZLogTrace($"Fetched '{RemoteName}' for '{tautRepoName}'");

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

        CheckCredentialKeyTrait();
    }

    void CheckCredentialKeyTrait()
    {
        _tautConfig.EnsureValues();

        using (var gitCred = new GitCredential(gitCli, _tautConfig.CredentialUrl))
        {
            gitCred.Fill();

            byte[] passwordSalt = [];

            if (string.IsNullOrEmpty(_tautConfig.CredentialUserName) == false)
            {
                passwordSalt = Encoding.UTF8.GetBytes(_tautConfig.CredentialUserName);
            }

            _keyHolder.DeriveCrudeKey(gitCred.PasswordData, passwordSalt);

            var credUrlData = Encoding.ASCII.GetBytes(_tautConfig.CredentialUrl);
            var keyTrait = _keyHolder.DeriveCredentialKeyTrait(credUrlData);

            if (keyTrait.SequenceEqual(_tautConfig.CredentialKeyTrait) == false)
            {
                gitCred.Reject();

                throw new InvalidOperationException(
                    $"The credential for ${_tautConfig.CredentialUrl} does not match the existing one"
                );
            }

            gitCred.Approve();
        }
    }

    void CloseTautRepo()
    {
        _tautRepo?.Dispose();
        _tautRepo = null;
    }

    internal void ApproveNewTautRepo()
    {
        var tautRepoNameToUse = GitRepoExtra.RemoveTautRepoNameTempPrefix(_tautRepoName);

        var tautHomePath = HostRepo.GetTautHomePath();
        var tautRepoPath = Path.Join(tautHomePath, _tautRepoName);
        var tautRepoPathToUse = Path.Join(tautHomePath, tautRepoNameToUse);

        tautMapping.Dispose();
        CloseTautRepo();

        Directory.Move(tautRepoPath, tautRepoPathToUse);

        logger.ZLogTrace($"Moved taut repo from '{_tautRepoName}' to '{tautRepoNameToUse}");

        using (var config = HostRepo.GetConfig())
        {
            _tautConfig.Save(config);
        }
    }
}
