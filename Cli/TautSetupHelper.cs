using Lg2.Sharpy;
using Microsoft.Extensions.Logging;
using ZLogger;

namespace Git.Taut;

class TautSetupHelper(
    ILogger<TautSetupHelper> logger,
    string remoteName,
    Lg2Repository tautRepo,
    Lg2Repository hostRepo
)
{
    const string defaultDescription = $"Created by {ProgramInfo.CommandName}";

    internal void SetupTautAndHost()
    {
        TautSetDescription();
        TautAddHostObjects();
        TautSetConfig();
        TautSetRemote();
    }

    void TautSetDescription()
    {
        var descriptionFile = GitRepoHelper.GetDescriptionFile(tautRepo);

        File.Delete(descriptionFile);

        using (var writer = File.AppendText(descriptionFile))
        {
            writer.NewLine = "\n";
            writer.WriteLine(defaultDescription);
        }

        logger.ZLogTrace($"Write '{defaultDescription}' to '{descriptionFile}'");
    }

    void TautSetConfig()
    {
        using var config = tautRepo.GetConfig();
        config.SetString(GitConfigHelper.Fetch_Prune, "true");
    }

    void TautSetRemote()
    {
        using (var remote = tautRepo.LookupRemote(remoteName))
        {
            var remoteUrl = remote.GetUrl();
            var remoteUri = new Uri(remoteUrl);

            if (remoteUri.IsFile)
            {
                // normalize the remote's file path
                tautRepo.SetRemoteUrl(remoteName, remoteUri.AbsolutePath);

                HostSetRemote(remoteUri);
            }
        }
    }

    void HostSetRemote(Uri tautRemoteUri)
    {
        if (tautRemoteUri.IsFile)
        {
            // var uriBuilder = new UriBuilder(tautRemoteUri) { Host = "localhost" };
            var hostRemoteUrl = GitRepoHelper.AddTautRemoteHelperPrefix(tautRemoteUri.AbsoluteUri);

            hostRepo.SetRemoteUrl(remoteName, hostRemoteUrl);
        }
    }

    void TautAddHostObjects()
    {
        var tautRepoObjectsDir = GitRepoHelper.GetObjectDir(tautRepo);
        var tautRepoObjectsInfoAlternatesFile = GitRepoHelper.GetObjectsInfoAlternatesFile(
            tautRepo
        );
        var hostRepoObjectsDir = GitRepoHelper.GetObjectDir(hostRepo);

        var relativePath = Path.GetRelativePath(tautRepoObjectsDir, hostRepoObjectsDir);
        relativePath = GitRepoHelper.UseForwardSlash(relativePath);

        using (var writer = File.AppendText(tautRepoObjectsInfoAlternatesFile))
        {
            writer.NewLine = "\n";
            writer.WriteLine(relativePath);
        }

        logger.ZLogTrace($"Write '{relativePath}' to '{tautRepoObjectsInfoAlternatesFile}'");
    }
}
