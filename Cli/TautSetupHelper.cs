using Lg2.Sharpy;
using Microsoft.Extensions.Logging;
using ZLogger;

namespace Git.Taut;

class TautSetupHelper(
    ILogger<TautSetupHelper> logger,
    string remoteName,
    Lg2Repository tautRepo,
    Lg2Repository hostRepo,
    GitCli gitCli
)
{
    const string defaultDescription = $"Created by {ProgramInfo.CommandName}";
    const string tautCredentialUrl = "tautCredentialUrl";
    const string tautCredentialKey = "tautCredentialKey";

    internal void SetupTautAndHost()
    {
        TautSetDescription();
        TautAddHostObjects();
        TautSetRemote();
        TautSetConfig();
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

            // normalize the remote's file path
            tautRepo.SetRemoteUrl(remoteName, remoteUri.AbsolutePath);

            using (var config = tautRepo.GetConfig())
            {
                var credUri = GitRepoHelper.ConvertToCredentialUri(remoteUri);
                var credUrl = credUri.AbsoluteUri;

                GitCredential gitCred = new(gitCli, credUrl);
                gitCred.Fill();
                gitCred.Approve();
                gitCred.Reject();

                var configName = $"remote.{remoteName}.{tautCredentialUrl}";
                config.SetString(configName, $"{credUrl}");
            }

            HostSetRemote(remoteUri);
        }
    }

    void HostSetRemote(Uri tautRemoteUri)
    {
        var hostRemoteUrl = GitRepoHelper.AddTautRemoteHelperPrefix(tautRemoteUri.AbsoluteUri);
        logger.ZLogDebug($"hostRemoteUri {hostRemoteUrl}");
        hostRepo.SetRemoteUrl(remoteName, hostRemoteUrl);
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
