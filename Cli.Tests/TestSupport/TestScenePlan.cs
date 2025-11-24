using Git.Taut;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using static Cli.Tests.TestSupport.TestScenePlanConstants;

namespace Cli.Tests.TestSupport;

static class TestScenePlanConstants
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

class TestScenePlan(ITestOutputHelper testOutput) : TestScene
{
    readonly IHost _host = TestHostBuilder.BuildHost(testOutput);
    internal IHost Host => _host;

    internal void RunGit(params string[] args)
    {
        var gitCli = _host.Services.GetRequiredService<GitCli>();

        gitCli.Execute(
            inputProvider: null,
            outputDataReceiver: line => testOutput.WriteLine(line),
            errorDataReceiver: line => testOutput.WriteLine(line),
            args
        );
    }

    bool _disposed;

    protected override void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }
        _disposed = true;

        if (disposing)
        {
            _host.Dispose();
        }

        base.Dispose(disposing);
    }
}

static class TestScenePlanExtensions
{
    public static void SetupRepo0(this TestScenePlan plan)
    {
        using var pushDir = new PushDirectory(plan.DirPath);

        plan.RunGit("init", "--bare", Repo0);
    }

    public static GitHttpBackend ServeRepo0(this TestScenePlan plan)
    {
        var repo0Path = Path.Join(plan.DirPath, Repo0);

        plan.RunGit("--git-dir", repo0Path, "config", "http.receivepack", "true");

        var loggerFactory = plan.Host.Services.GetRequiredService<ILoggerFactory>();

        GitHttpBackend gitHttp = new(repo0Path, loggerFactory);

        gitHttp.Start();

        return gitHttp;
    }

    public static void ConfigRepo0WithTags(this TestScenePlan plan)
    {
        using var pushDir = new PushDirectory(Path.Join(plan.DirPath, Repo0));

        plan.RunGit("tag", "tag0", "HEAD");
    }

    public static void SetupRepo1(this TestScenePlan plan)
    {
        using var pushDir = new PushDirectory(plan.DirPath);

        plan.RunGit("clone", "repo0", "repo1");

        var repo1Path = Path.Join(plan.DirPath, "repo1");

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

        plan.RunGit("add", "--all");
        plan.RunGit("commit", "-m", "repo1");
        plan.RunGit("push");
    }

    public static void SetupRepo2(this TestScenePlan plan)
    {
        using var pushDir = new PushDirectory(plan.DirPath);

        plan.RunGit("clone", "--origin", Repo0, "taut::repo0", Repo2);
    }

    public static void ConfigRepo2AddingRepo1(this TestScenePlan plan)
    {
        using var pushDir = new PushDirectory(plan.DirPath);

        Directory.SetCurrentDirectory(Repo2);

        plan.RunGit("taut", "site", "add", Repo1, Path.Join("..", Repo1));
    }

    public static void ConfigRepo2AddingRepo1WithLinkToRepo0(this TestScenePlan plan)
    {
        using var pushDir = new PushDirectory(plan.DirPath);

        Directory.SetCurrentDirectory(Repo2);

        plan.RunGit(
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

    public static void SetupRepo9(this TestScenePlan plan)
    {
        using var pushDir = new PushDirectory(plan.DirPath);

        plan.RunGit("clone", Repo0, Repo9);
    }
}
