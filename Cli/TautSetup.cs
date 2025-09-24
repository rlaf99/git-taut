using System.Diagnostics.CodeAnalysis;
using System.Text;
using Lg2.Sharpy;
using Microsoft.Extensions.Logging;
using ZLogger;

namespace Git.Taut;

class TautSetup(
    ILogger<TautSetup> logger,
    GitCli gitCli,
    TautManager tautManager,
    TautMapping tautMapping,
    Aes256Cbc1 tautCipher
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

    internal string KeyValueStoreLocation => _tautRepo.GetObjectInfoDir();

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

    internal void Init(string remoteName, string hostRepoPath, bool brandNewSetup)
    {
        ThrowHelper.InvalidOperationIfAlreadyInitalized(_initialized);

        _initialized = true;

        var tautRepoPath = GitRepoHelper.GetTautDir(hostRepoPath);
        var tautRepo = Lg2Repository.New(tautRepoPath);
        var hostRepo = Lg2Repository.New(hostRepoPath);

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
        var descriptionFile = GitRepoHelper.GetDescriptionFile(_tautRepo);

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
        var tautRepoObjectsDir = GitRepoHelper.GetObjectDir(_tautRepo);
        var tautRepoObjectsInfoAlternatesFile = GitRepoHelper.GetObjectsInfoAlternatesFile(
            _tautRepo
        );
        var hostRepoObjectsDir = GitRepoHelper.GetObjectDir(_hostRepo);

        var relativePath = Path.GetRelativePath(tautRepoObjectsDir, hostRepoObjectsDir);
        relativePath = GitRepoHelper.UseForwardSlash(relativePath);

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
            Console.Error.WriteLine($"DBG {remoteUrl}");
            var remoteUri = new Uri(remoteUrl);

            // normalize the remote's file path
            _tautRepo.SetRemoteUrl(_remoteName, remoteUri.AbsolutePath);

            var gitCredUri = GitRepoHelper.ConvertToCredentialUri(remoteUri);
            var gitCredUrl = gitCredUri.AbsoluteUri;

            HostSetRemote(remoteUri, gitCredUrl);
        }
    }

    void HostSetRemote(Uri tautRemoteUri, string gitCredUrl)
    {
        var hostRemoteUrl = GitRepoHelper.AddTautRemoteHelperPrefix(tautRemoteUri.AbsoluteUri);
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

                var info = Encoding.ASCII.GetBytes(gitCredUrl);
                var credTag = _keyHolder.DeriveCredentialKeyTrait(info);

                config.SetTautCredentialKeyTrait(_remoteName, credTag);

                gitCred.Approve();
            }

            config.SetTautCredentialUrl(_remoteName, gitCredUrl);
        }
    }

    void TautSetConfig()
    {
        using var config = _tautRepo.GetConfig();
        config.SetString(GitConfigHelper.Fetch_Prune, "true");
    }

    void EnsureExistingSetup()
    {
        var tautRemote = _tautRepo.LookupRemote(_remoteName);
        var tautRemoteUrl = tautRemote.GetUrl();
        var tautRemoteUri = new Uri(tautRemoteUrl);

        var hostRemote = _hostRepo.LookupRemote(_remoteName);
        var hostRemoteUrl = hostRemote.GetUrl();
        hostRemoteUrl = GitRepoHelper.RemoveTautRemoteHelperPrefix(hostRemoteUrl);
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
                    $"{GitConfigHelper.TautCredentialUrl} is not found in the repo config for remote '{_remoteName}'"
                );
            }

            var credKeyTrait = config.GetTautCredentialKeyTrait(_remoteName);
            if (credKeyTrait is null)
            {
                throw new InvalidOperationException(
                    $"{GitConfigHelper.TautCredentialKeyTrait} is not found in the repo config for remote '{_remoteName}'"
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
}
