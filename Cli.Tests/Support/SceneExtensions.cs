using Git.Taut;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Cli.Tests.Support;

static class SceneExtensions
{
    public static void SetupRepo0(this TestScene scene, IHost host)
    {
        var gitCli = host.Services.GetRequiredService<GitCli>();

        var savedDir = Directory.GetCurrentDirectory();
        try
        {
            var scenePath = scene.DirPath;
            Directory.SetCurrentDirectory(scenePath);

            gitCli.Run("init", "--bare", "repo0");

            var repo0Path = Path.Join(scenePath, "repo0");
        }
        finally
        {
            Directory.SetCurrentDirectory(savedDir);
        }
    }

    public static void SetupRepo1(this TestScene scene, IHost host)
    {
        var gitCli = host.Services.GetRequiredService<GitCli>();

        var savedDir = Directory.GetCurrentDirectory();
        try
        {
            var scenePath = scene.DirPath;
            Directory.SetCurrentDirectory(scenePath);

            gitCli.Run("clone", "repo0", "repo1");

            var repo1Path = Path.Join(scenePath, "repo1");

            Directory.SetCurrentDirectory(repo1Path);

            File.WriteAllText("README", "repo1");
            File.WriteAllText(
                ".gitattributes",
                """
                *.tt taut
                tt taut
                tt/** taut
                """
            );

            gitCli.Run("add", "--all");
            gitCli.Run("commit", "-m", "repo1");
            gitCli.Run("push");
        }
        finally
        {
            Directory.SetCurrentDirectory(savedDir);
        }
    }
}
