using System.Text;
using Lg2.Sharpy;
using Microsoft.Extensions.Logging;
using ZLogger;

namespace Git.Taut;

class TautSetupHelper(
    ILogger<TautSetupHelper> logger,
    string remoteName,
    Lg2Repository tautRepo,
    Lg2Repository hostRepo,
    GitCli gitCli,
    UserKeyHolder keyHolder
)
{
    const string defaultDescription = $"Created by {ProgramInfo.CommandName}";

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

            var gitCredUri = GitRepoHelper.ConvertToCredentialUri(remoteUri);
            var gitCredUrl = gitCredUri.AbsoluteUri;

            HostSetRemote(remoteUri, gitCredUrl, keyHolder);
        }
    }

    void HostSetRemote(Uri tautRemoteUri, string gitCredUrl, UserKeyHolder keyHolder)
    {
        var hostRemoteUrl = GitRepoHelper.AddTautRemoteHelperPrefix(tautRemoteUri.AbsoluteUri);
        hostRepo.SetRemoteUrl(remoteName, hostRemoteUrl);

        using (var config = hostRepo.GetConfig())
        {
            using (var gitCred = new GitCredential(gitCli, gitCredUrl))
            {
                gitCred.Fill();

                byte[] passwordSalt = [];

                if (string.IsNullOrEmpty(gitCred.UserName) == false)
                {
                    config.SetTautCredentialUserName(remoteName, gitCred.UserName);

                    passwordSalt = Encoding.UTF8.GetBytes(gitCred.UserName);
                }

                keyHolder.DeriveCrudeKey(gitCred.PasswordData, passwordSalt);

                var info = Encoding.ASCII.GetBytes(gitCredUrl);
                var credTag = keyHolder.DeriveCredentialKeyTrait(info);

                config.SetTautCredentialKeyTrait(remoteName, credTag);

                gitCred.Approve();
            }

            config.SetTautCredentialUrl(remoteName, gitCredUrl);
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
