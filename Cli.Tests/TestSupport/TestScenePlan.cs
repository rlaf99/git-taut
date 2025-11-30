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

    internal static readonly string Repo0Git = Path.Join(Repo0, GitRepoHelpers.DotGit);
    internal static readonly string Repo1Git = Path.Join(Repo1, GitRepoHelpers.DotGit);
    internal static readonly string Repo2Git = Path.Join(Repo2, GitRepoHelpers.DotGit);
    internal static readonly string Repo3Git = Path.Join(Repo3, GitRepoHelpers.DotGit);
    internal static readonly string Repo9Git = Path.Join(Repo9, GitRepoHelpers.DotGit);

    internal const string Master = "master";
    internal const string Branch1 = "branch1";
    internal const string Branch2 = "branch2";
    internal const string Tag1 = "tag1";
    internal const string Tag2 = "tag2";
    internal const string AnnotatedTag1 = "annotated-tag1";
    internal const string AnnotatedTag2 = "annotated-tag2";
}

class TestScenePlan(ITestOutputHelper testOutput) : TestScene
{
    readonly IHost _host = TestHostBuilder.BuildHost(testOutput);
    internal IHost Host => _host;

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

    internal void AddFile(string dirPath, string filename, string content)
    {
        var filePath = Path.Join(dirPath, filename);

        File.WriteAllText(filePath, content);
    }

    string? _repo0Root;
    string? _repo1Root;
    string? _repo2Root;
    string? _repo9Root;

    internal string Repo0Root => _repo0Root ??= Path.Join(Location, Repo0);
    internal string Repo1Root => _repo1Root ??= Path.Join(Location, Repo1);
    internal string Repo2Root => _repo2Root ??= Path.Join(Location, Repo2);
    internal string Repo9Root => _repo9Root ??= Path.Join(Location, Repo9);

    string? _repo0GitDir;
    string? _repo1GitDir;
    string? _repo2GitDir;
    string? _repo9GitDir;

    internal string Repo0GitDir => _repo0GitDir ??= Path.Join(Location, Repo0);
    internal string Repo1GitDir =>
        _repo1GitDir ??= Path.Join(Location, Repo1, GitRepoHelpers.DotGit);
    internal string Repo2GitDir =>
        _repo2GitDir ??= Path.Join(Location, Repo2, GitRepoHelpers.DotGit);
    internal string Repo9GitDir =>
        _repo9GitDir ??= Path.Join(Location, Repo9, GitRepoHelpers.DotGit);

    internal void RunGitOnRepo0(params string[] args) => RunGit(["-C", Repo0Root, .. args]);

    internal void RunGitOnRepo1(params string[] args) => RunGit(["-C", Repo1Root, .. args]);

    internal void RunGitOnRepo2(params string[] args) => RunGit(["-C", Repo2Root, .. args]);

    internal void RunGitOnRepo9(params string[] args) => RunGit(["-C", Repo9Root, .. args]);

    internal void AddFileOnRepo1(string filename, string content) =>
        AddFile(Repo1Root, filename, content);

    internal void AddFileOnRepo2(string filename, string content) =>
        AddFile(Repo2Root, filename, content);

    internal void AddFileOnRepo9(string filename, string content) =>
        AddFile(Repo9Root, filename, content);
}

static class TestScenePlanExtensions
{
    public static void SetLaunchDirectory(this TestScenePlan plan, string? dirPath)
    {
        var actionHelpers = plan.Host.Services.GetRequiredService<CommandActionHelpers>();

        actionHelpers.LaunchDirectory = dirPath;
    }

    public static void SetupRepo0(this TestScenePlan plan)
    {
        plan.RunGit("init", "--bare", plan.Repo0Root);
    }

    public static GitHttpBackend ServeRepo0(this TestScenePlan plan)
    {
        plan.RunGit("-C", plan.Repo0Root, "config", "http.receivepack", "true");

        var loggerFactory = plan.Host.Services.GetRequiredService<ILoggerFactory>();

        GitHttpBackend gitHttp = new(plan.Repo0Root, loggerFactory);

        gitHttp.Start();

        return gitHttp;
    }

    public static void ConfigRepo0WithTags(this TestScenePlan plan)
    {
        plan.RunGit("-C", plan.Repo0Root, "tag", "tag0", GitRepoHelpers.HEAD);
    }

    public static void SetupRepo1(this TestScenePlan plan)
    {
        plan.RunGit("-C", plan.Location, "clone", Repo0, Repo1);

        plan.AddFile(plan.Repo1Root, "README", Repo1);

        plan.AddFile(
            plan.Repo1Root,
            GitRepoHelpers.DotGitAttributes,
            """
            *.tt taut
            tt taut
            tt/** taut
            """
        );

        plan.RunGit("-C", plan.Repo1Root, "add", "--all");
        plan.RunGit("-C", plan.Repo1Root, "commit", "-m", Repo1);
        plan.RunGit("-C", plan.Repo1Root, "push");
    }

    public static void SetupRepo2(this TestScenePlan plan)
    {
        plan.RunGit("-C", plan.Location, "clone", "--origin", Repo0, $"taut::{Repo0}", Repo2);
    }

    public static void ConfigRepo2AddingRepo1(this TestScenePlan plan)
    {
        plan.RunGit("-C", plan.Repo2Root, "taut", "add", Repo1, Path.Join("..", Repo1));
    }

    public static void ConfigRepo2AddingRepo1WithLinkToRepo0(this TestScenePlan plan)
    {
        plan.RunGit(
            "-C",
            plan.Repo2Root,
            "taut",
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
        plan.RunGit("-C", plan.Location, "clone", Repo0, Repo9);
    }
}
