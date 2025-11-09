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
    TautSiteConfig? _siteConfig;

    TautSiteConfig SiteConfig => _siteConfig!;

    bool _gearedUp;

    internal bool GearedUp => _gearedUp;

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
        string? tautSiteNameToLink = null
    )
    {
        EnsureNotGearedUp();

        _hostRepo = hostRepo;
        _remoteName = remoteName;

        var siteName = Path.GetRandomFileName().Replace('.', '-');

        _siteConfig = new(siteName, tautSiteNameToLink);

        EnsureHostOidType();

        EnsureBrandNewSetup(remoteAddress);

        tautCipher.Init(KeyHolder);
        tautMapping.Init(KeyValueStoreLocation);
        tautManager.Init(HostRepo, TautRepo);

        var result = new Task(WrapUpBrandNew);

        return result;
    }

    internal void GearUpExisting(Lg2Repository hostRepo, string? remoteName, string tautSiteName)
    {
        EnsureNotGearedUp();

        _hostRepo = hostRepo;
        _remoteName = remoteName;

        _siteConfig = new(tautSiteName);

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
        SiteConfig.Remotes.Add(RemoteName);

        var tautSitePath = HostRepo.GetTautSitePath(SiteConfig.SiteName);

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

        if (SiteConfig.LinkTo is not null)
        {
            using (var config = HostRepo.GetConfigSnapshot())
            {
                SiteConfig.LinkTo.Load(config);
            }

            var tautSitePathToLink = HostRepo.GetTautSitePath(SiteConfig.LinkTo.SiteName);

            argList.Add("--reference");
            argList.Add(tautSitePathToLink);

            logger.ZLogTrace($"{SiteConfig.SiteName} is linked to {SiteConfig.LinkTo.SiteName}");
        }

        argList.Add(remoteAddress);
        argList.Add(tautSitePath);

        gitCli.Execute(argList.ToArray());

        logger.ZLogTrace($"Cloned '{RemoteName}' from '{remoteAddress}' into '{tautSitePath}'");

        _tautRepo = Lg2Repository.New(tautSitePath);

        SetSiteDescription();
        SetSiteFetchConfig();
        // TautAddHostObjects();
        UpdateRemoteUrls();
        UpdateTautConfig();
    }

    void SetSiteDescription()
    {
        var descriptionFile = GitRepoHelpers.GetDescriptionFile(_tautRepo);

        File.Delete(descriptionFile);

        using (var writer = File.AppendText(descriptionFile))
        {
            writer.NewLine = "\n";
            writer.WriteLine(defaultDescription);
        }

        logger.ZLogTrace($"Updated '{descriptionFile}'");
    }

    // Not used, as `--reference repo` is used when cloning.
    void TautAddHostObjects()
    {
        var tautRepoObjectsDir = GitRepoHelpers.GetObjectDirPath(_tautRepo);
        var tautRepoObjectsInfoAlternatesFile = GitRepoHelpers.GetObjectsInfoAlternatesFilePath(
            _tautRepo
        );
        var hostRepoObjectsDir = GitRepoHelpers.GetObjectDirPath(_hostRepo);

        var relativePath = Path.GetRelativePath(tautRepoObjectsDir, hostRepoObjectsDir);
        relativePath = GitRepoHelpers.UseForwardSlash(relativePath);

        using (var writer = File.AppendText(tautRepoObjectsInfoAlternatesFile))
        {
            writer.NewLine = "\n";
            writer.WriteLine(relativePath);
        }

        logger.ZLogTrace($"Wrote '{relativePath}' to '{tautRepoObjectsInfoAlternatesFile}'");
    }

    void SetSiteFetchConfig()
    {
        using var config = _tautRepo.GetConfig();
        config.SetString(GitConfigHelpers.Fetch_Prune, "true");
    }

    void UpdateRemoteUrls()
    {
        using (var remote = TautRepo.LookupRemote(RemoteName))
        {
            var remoteUrl = remote.GetUrl();
            var remoteUri = new Uri(remoteUrl);

            if (remoteUri.IsFile)
            {
                TautRepo.SetRemoteUrl(RemoteName, remoteUri.AbsolutePath);
            }
            else
            {
                TautRepo.SetRemoteUrl(RemoteName, remoteUri.AbsoluteUri);
            }

            var hostRemoteUrl = GitRepoHelpers.AddTautRemoteHelperPrefix(remoteUri.AbsoluteUri);
            _hostRepo.SetRemoteUrl(RemoteName, hostRemoteUrl);
        }
    }

    void UpdateTautConfig()
    {
        using var hostConfig = HostRepo.GetConfig();

        SiteConfig.SaveRemotes(hostConfig);

        if (SiteConfig.LinkTo is not null)
        {
            CheckCredentialKeyTrait(SiteConfig.LinkTo);

            return;
        }

        var tautSitePath = HostRepo.GetTautSitePath(SiteConfig.SiteName);
        var gitCredUrl = GitRepoHelpers.ConvertPathToTautCredentialUrl(tautSitePath);

        SiteConfig.CredentialUrl = gitCredUrl;
        SiteConfig.SaveCredentialUrl(hostConfig);

        using (var gitCred = new GitCredential(gitCli, gitCredUrl))
        {
            gitCred.Fill();

            byte[] passwordSalt = [];

            if (string.IsNullOrEmpty(gitCred.UserName) == false)
            {
                SiteConfig.CredentialUserName = gitCred.UserName;

                passwordSalt = Encoding.UTF8.GetBytes(gitCred.UserName);
            }

            _keyHolder.DeriveCrudeKey(gitCred.PasswordData, passwordSalt);

            var infoData = Encoding.ASCII.GetBytes(gitCredUrl);
            var keyTrait = _keyHolder.DeriveCredentialKeyTrait(infoData);

            SiteConfig.CredentialKeyTrait = keyTrait;

            gitCred.Approve();
        }
    }

    void EnsureExistingSetup()
    {
        using (var config = HostRepo.GetConfig())
        {
            SiteConfig.Load(config);
        }

        var tautSitePath = HostRepo.GetTautSitePath(SiteConfig.SiteName);

        _tautRepo = Lg2Repository.New(tautSitePath);

        if (_remoteName is not null)
        {
            gitCli.Execute(
                "--git-dir",
                tautSitePath,
                "fetch",
                RemoteName,
                GitRepoHelpers.DefaultFetchSpec
            );

            logger.ZLogTrace($"Fetched '{RemoteName}' for '{SiteConfig.SiteName}'");

            using var tautRemote = _tautRepo.LookupRemote(RemoteName);
            var tautRemoteUrl = tautRemote.GetUrl();
            var tautRemoteUri = new Uri(tautRemoteUrl);

            using var hostRemote = _hostRepo.LookupRemote(RemoteName);
            var hostRemoteUrl = hostRemote.GetUrl();
            hostRemoteUrl = GitRepoHelpers.RemoveTautRemoteHelperPrefix(hostRemoteUrl);
            var hostRemoteUri = new Uri(hostRemoteUrl);

            if (hostRemoteUri.IsFile)
            {
                if (hostRemoteUri.AbsolutePath != tautRemoteUri.AbsolutePath)
                {
                    throw new InvalidOperationException(
                        $"host remote '{RemoteName}':'{hostRemoteUri.AbsolutePath}'"
                            + $" and taut remote '{RemoteName}':'{tautRemoteUri.AbsolutePath}'"
                            + " do not have the same path"
                    );
                }
            }
        }

        CheckCredentialKeyTrait(SiteConfig);
    }

    void CheckCredentialKeyTrait(TautSiteConfig tautConfig)
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
            if (SiteConfig.LinkTo is not null)
            {
                SiteConfig.SaveLinkTo(config);
            }
            else
            {
                SiteConfig.SaveCredentialPair(config);
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
