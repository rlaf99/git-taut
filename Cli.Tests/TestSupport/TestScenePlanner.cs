using Git.Taut;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using static Cli.Tests.TestSupport.TestScenePlannerConstants;

namespace Cli.Tests.TestSupport;

sealed class TestScenePlanner(IHost host) : IDisposable
{
    readonly TestScene _scene = new();

    internal TestScene Scene => _scene;

    internal IHost Host => host;

    public void PreserveContentWhenFailed(ITestOutputHelper? output = null) =>
        _scene.PreserveContentWhenFailed(output);

    public void PreserveContentWhenAsked(ITestOutputHelper? output = null) =>
        _scene.PreserveContentWhenAsked(output);

    bool _disposed;

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }
        _disposed = true;

        host.Dispose();
        _scene.Dispose();
    }
}

static class TestScenePlannerConstants
{
    internal const string Repo0 = "repo0";
    internal const string Repo1 = "repo1";
    internal const string Repo2 = "repo2";
    internal const string Repo3 = "repo3";
    internal const string Repo9 = "repo9";

    internal static readonly string Repo0Git = Path.Join(Repo0, ".git");
    internal static readonly string Repo1Git = Path.Join(Repo1, ".git");
    internal static readonly string Repo2Git = Path.Join(Repo2, ".git");
    internal static readonly string Repo3Git = Path.Join(Repo3, ".git");
    internal static readonly string Repo9Git = Path.Join(Repo9, ".git");
}

static class TestScenePlannerExtensions
{
    public static void SetupRepo0(this TestScenePlanner planner)
    {
        var scene = planner.Scene;
        var host = planner.Host;

        var gitCli = host.Services.GetRequiredService<GitCli>();

        using var pushDir = new PushDirectory(scene.DirPath);

        gitCli.Run("init", "--bare", Repo0);
    }

    public static void ConfigRepo0WithTags(this TestScenePlanner planner)
    {
        var scene = planner.Scene;
        var host = planner.Host;

        var gitCli = host.Services.GetRequiredService<GitCli>();

        using var pushDir = new PushDirectory(Path.Join(scene.DirPath, Repo0));

        gitCli.Run("tag", "tag0", "HEAD");
    }

    public static void SetupRepo1(this TestScenePlanner planner)
    {
        var scene = planner.Scene;
        var host = planner.Host;

        var gitCli = host.Services.GetRequiredService<GitCli>();

        using var pushDir = new PushDirectory(scene.DirPath);

        gitCli.Run("clone", "repo0", "repo1");

        var repo1Path = Path.Join(scene.DirPath, "repo1");

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

    public static void SetupRepo2(this TestScenePlanner planner)
    {
        var scene = planner.Scene;
        var host = planner.Host;

        var gitCli = host.Services.GetRequiredService<GitCli>();

        using var pushDir = new PushDirectory(scene.DirPath);

        gitCli.Run("clone", "--origin", Repo0, "taut::repo0", Repo2);
    }

    public static void ConfigRepo2AddingRepo1(this TestScenePlanner planner)
    {
        var scene = planner.Scene;
        var host = planner.Host;

        var gitCli = host.Services.GetRequiredService<GitCli>();

        using var pushDir = new PushDirectory(scene.DirPath);

        Directory.SetCurrentDirectory(Repo2);

        gitCli.Run("taut", "site", "add", Repo1, Path.Join("..", Repo1));
    }

    public static void ConfigRepo2AddingRepo1WithLinkToRepo0(this TestScenePlanner planner)
    {
        var scene = planner.Scene;
        var host = planner.Host;

        var gitCli = host.Services.GetRequiredService<GitCli>();

        using var pushDir = new PushDirectory(scene.DirPath);

        Directory.SetCurrentDirectory(Repo2);

        gitCli.Run(
            "taut",
            "site",
            "--target",
            Repo0,
            "add",
            Repo1,
            Path.Join("..", Repo1),
            "--link-existing"
        );
    }

    public static void SetupRepo9(this TestScenePlanner planner)
    {
        var scene = planner.Scene;
        var host = planner.Host;

        var gitCli = host.Services.GetRequiredService<GitCli>();

        using var pushDir = new PushDirectory(scene.DirPath);

        gitCli.Run("clone", Repo0, Repo9);
    }
}
