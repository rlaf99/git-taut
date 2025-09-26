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
    Lg2Repository _tautRepo;

    internal Lg2Repository TautRepo => _tautRepo!;

    UserKeyHolder _keyHolder = new();

    internal UserKeyHolder KeyHolder => _keyHolder;

    internal string KeyValueStoreLocation => _tautRepo.GetObjectInfoDirPath();

    bool _initialized;

    internal bool Initialized => _initialized;

    internal void EnsureInitialized()
    {
        ThrowHelper.InvalidOperationIfNotInitialized(_initialized, nameof(TautSetup));
    }

    void EnsureOidType()
    {
        if (_hostRepo.GetOidType() != Lg2OidType.LG2_OID_SHA1)
        {
            var name = Enum.GetName(Lg2OidType.LG2_OID_SHA1);
            throw new InvalidOperationException($"Only oid type {name} is supported");
        }
    }

    void InitCommon(
        string remoteName,
        Lg2Repository hostRepo,
        Lg2Repository tautRepo,
        bool brandNewSetup
    )
    {
        _remoteName = remoteName;
        _hostRepo = hostRepo;
        _tautRepo = tautRepo;

        EnsureOidType();

        if (brandNewSetup)
        {
            EnsureBrandNewSetup();
        }
        else
        {
            EnsureExistingSetup();
        }

        tautCipher.Init(KeyHolder);
        tautMapping.Init(KeyValueStoreLocation);
        tautManager.Init(remoteName, hostRepo, tautRepo);
    }

    internal void Init(
        string remoteName,
        Lg2Repository hostRepo,
        string tautRepoName,
        bool brandNewSetup
    )
    {
        ThrowHelper.InvalidOperationIfAlreadyInitalized(_initialized);

        _initialized = true;

        var tautRepoPath = GitRepoExtra.GetTautRepoPath(hostRepo.GetPath(), tautRepoName);
        var tautRepo = Lg2Repository.New(tautRepoPath);

        InitCommon(remoteName, hostRepo, tautRepo, brandNewSetup);
    }

    internal void Init(
        string remoteName,
        Lg2Repository hostRepo,
        Lg2Repository tautRepo,
        bool brandNewSetup
    )
    {
        ThrowHelper.InvalidOperationIfAlreadyInitalized(_initialized);

        _initialized = true;

        InitCommon(remoteName, hostRepo, tautRepo, brandNewSetup);
    }

    void EnsureBrandNewSetup()
    {
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

        using (var config = _hostRepo.GetConfig())
        {
            using (var gitCred = new GitCredential(gitCli, gitCredUrl))
            {
                gitCred.Fill();

                byte[] passwordSalt = [];

                if (string.IsNullOrEmpty(gitCred.UserName) == false)
                {
                    config.SetTautCredentialUserName(_remoteName, gitCred.UserName);

                    passwordSalt = Encoding.UTF8.GetBytes(gitCred.UserName);
                }

                _keyHolder.DeriveCrudeKey(gitCred.PasswordData, passwordSalt);

                var infoData = Encoding.ASCII.GetBytes(gitCredUrl);
                var keyTrait = _keyHolder.DeriveCredentialKeyTrait(infoData);

                config.SetTautCredentialKeyTrait(_remoteName, keyTrait);

                gitCred.Approve();
            }

            config.SetTautCredentialUrl(_remoteName, gitCredUrl);
        }
    }

    void HostSetTautRepoName(string tautRepoName)
    {
        using (var config = _hostRepo.GetConfig())
        {
            config.SetTautRepoName(_remoteName, tautRepoName);
        }
    }

    void TautSetConfig()
    {
        using var config = _tautRepo.GetConfig();
        config.SetString(GitConfigExtra.Fetch_Prune, "true");
    }

    void EnsureExistingSetup()
    {
        var tautRemote = _tautRepo.LookupRemote(_remoteName);
        var tautRemoteUrl = tautRemote.GetUrl();
        var tautRemoteUri = new Uri(tautRemoteUrl);

        var hostRemote = _hostRepo.LookupRemote(_remoteName);
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
        using (var config = _hostRepo.GetConfig())
        {
            var credUrl = config.GetTautCredentialUrl(_remoteName);
            if (credUrl is null)
            {
                throw new InvalidOperationException(
                    $"{GitConfigExtra.TautCredentialUrl} is not found in the repo config for remote '{_remoteName}'"
                );
            }

            var credKeyTrait = config.GetTautCredentialKeyTrait(_remoteName);
            if (credKeyTrait is null)
            {
                throw new InvalidOperationException(
                    $"{GitConfigExtra.TautCredentialKeyTrait} is not found in the repo config for remote '{_remoteName}'"
                );
            }

            var credUserName = config.GetTautCredentialUserName(_remoteName);

            using (var gitCred = new GitCredential(gitCli, credUrl))
            {
                gitCred.Fill();

                byte[] passwordSalt = [];

                if (string.IsNullOrEmpty(credUserName) == false)
                {
                    passwordSalt = Encoding.UTF8.GetBytes(credUserName);
                }

                _keyHolder.DeriveCrudeKey(gitCred.PasswordData, passwordSalt);

                var credUrlData = Encoding.ASCII.GetBytes(credUrl);
                var keyTrait = _keyHolder.DeriveCredentialKeyTrait(credUrlData);

                if (keyTrait.SequenceEqual(credKeyTrait) == false)
                {
                    gitCred.Reject();

                    throw new InvalidOperationException(
                        $"The credential for ${credUrl} does not match the existing one"
                    );
                }

                gitCred.Approve();
            }
        }
    }

    void CloseTautRepo()
    {
        TautRepo.Dispose();
        _tautRepo = null;
    }

    internal void ApproveNewTautRepo(string tautRepoName)
    {
        var tautRepoNameToUse = GitRepoExtra.RemoveTautRepoNameTempPrefix(tautRepoName);

        var tautHomePath = HostRepo.GetTautHomePath();
        var tautRepoPath = Path.Join(tautHomePath, tautRepoName);
        var tautRepoPathToUse = Path.Join(tautHomePath, tautRepoNameToUse);

        tautMapping.Dispose();
        CloseTautRepo();

        Directory.Move(tautRepoPath, tautRepoPathToUse);

        HostSetTautRepoName(tautRepoNameToUse);

        logger.ZLogTrace($"Moved taut repo from '{tautRepoName}' to '{tautRepoNameToUse}");
    }
}
