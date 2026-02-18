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
    const string _defaultDescription = $"Created by {AppInfo.GitTautCommandName}";

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
    TautSiteConfiguration? _siteConfig;

    TautSiteConfiguration SiteConfig => _siteConfig!;

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
        var tautSitePath = HostRepo.GetTautSitePath(SiteConfig.SiteName);

        List<string> referencedRepoPaths = [HostRepo.GetPath()];

        List<string> argList =
        [
            "clone",
            "--no-local",
            "--bare",
            // git version 2.34.1: fatal: --bare and --origin options are incompatible.
            // "--origin",
            // RemoteName,
        ];

        if (SiteConfig.LinkTo is not null)
        {
            using (var config = HostRepo.GetConfigSnapshot())
            {
                SiteConfig.LinkTo.Load(config);
            }

            var tautSitePathToLink = HostRepo.GetTautSitePath(SiteConfig.LinkTo.SiteName);

            referencedRepoPaths.Add(tautSitePathToLink);

            logger.ZLogTrace($"{SiteConfig.SiteName} is linked to {SiteConfig.LinkTo.SiteName}");
        }

        foreach (var repoPath in referencedRepoPaths)
        {
            argList.Add("--reference");
            argList.Add(repoPath);
        }

        argList.Add(remoteAddress);
        argList.Add(tautSitePath);

        gitCli.Execute(argList.ToArray());

        logger.ZLogTrace($"Cloned '{RemoteName}' from '{remoteAddress}' into '{tautSitePath}'");

        _tautRepo = Lg2Repository.New(tautSitePath);

        if (RemoteName != "origin")
        {
            _tautRepo.RenameRemote("origin", RemoteName);
        }

        SetSiteDescription();
        UpdateRemoteUrls();
        UpdateTautConfig();
        UpdateAlternates();
    }

    void UpdateAlternates()
    {
        var alternatesFile = TautRepo.GetObjectsInfoAlternatesFilePath();

        if (File.Exists(alternatesFile))
        {
            var lines = File.ReadAllLines(alternatesFile);

            File.Delete(alternatesFile);

            using (var writer = File.AppendText(alternatesFile))
            {
                writer.NewLine = "\n";

                var objectDir = TautRepo.GetObjectDirPath();

                foreach (var line in lines)
                {
                    var relPath = Path.GetRelativePath(objectDir, line);
                    relPath = GitRepoHelpers.UseForwardSlash(relPath);

                    writer.WriteLine(relPath);
                }
            }

            logger.ZLogTrace($"Updated '{alternatesFile}'");
        }
    }

    void SetSiteDescription()
    {
        var descriptionFile = TautRepo.GetDescriptionFile();

        File.Delete(descriptionFile);

        using (var writer = File.AppendText(descriptionFile))
        {
            writer.NewLine = "\n";
            writer.WriteLine(_defaultDescription);
        }

        logger.ZLogTrace($"Updated '{descriptionFile}'");
    }

    void UpdateRemoteUrls()
    {
        using var remote = TautRepo.LookupRemote(RemoteName);

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

        SiteConfig.RemoteUrl = hostRemoteUrl;

        if (HostRepo.TryLookupRemote(RemoteName, out _))
        {
            HostRepo.SetRemoteUrl(RemoteName, hostRemoteUrl);
        }
        else
        {
            HostRepo.NewRemote(RemoteName, hostRemoteUrl);
        }
    }

    void UpdateTautConfig()
    {
        using var hostConfig = HostRepo.GetConfig();

        SiteConfig.SaveRemoteUrl(hostConfig);

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

    internal void FetchRemote()
    {
        const string plusRefsHeadsToRefsHeads = "+refs/heads/*:refs/heads/*";
        const string plusRefsTagsToRefsTags = "+refs/tags/*:refs/tags/*";

        var tautRepoPath = TautRepo.GetPath();

        gitCli.Execute(
            "--git-dir",
            tautRepoPath,
            "fetch",
            "--prune",
            RemoteName,
            plusRefsHeadsToRefsHeads,
            plusRefsTagsToRefsTags
        );

        logger.ZLogTrace($"Fetched '{RemoteName}' for '{SiteConfig.SiteName}'");
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

    void CheckCredentialKeyTrait(TautSiteConfiguration tautConfig)
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
