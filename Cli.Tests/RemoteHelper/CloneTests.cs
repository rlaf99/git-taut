using Cli.Tests.TestSupport;
using Git.Taut;
using Lg2.Sharpy;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using static Cli.Tests.TestSupport.TestScenePlannerConstants;

namespace Cli.Tests.RemoteHelper;

[Collection("WithGitTautPaths")]
public sealed class CloneTests(ITestOutputHelper testOutput) : IDisposable
{
    IHost Host => _planner.Host;

    TestScene Scene => _planner.Scene;

    TestScenePlanner _planner = new(GitTautHostBuilder.BuildHost());

    public void Dispose()
    {
        _planner.PreserveContentWhenFailed(testOutput);
        _planner.Dispose();
    }

    void UpdateContentAndPush(GitCli gitCli)
    {
        string a_md = "a.md";
        string b_tt = "b.tt";

        string a_md_content = "Not encrypted";
        string b_tt_content = "Encrypted";

        File.AppendAllText(a_md, a_md_content);
        File.AppendAllText(b_tt, b_tt_content);

        gitCli.Run("add", "--all");
        gitCli.Run("commit", "-m", $"{nameof(UpdateContentAndPush)}");
        gitCli.Run("push");
    }

    bool CompareBranch(Lg2Repository repo1, Lg2Repository repo2, string branch)
    {
        var repo1Branch = repo1.LookupBranch(branch, Lg2BranchType.LG2_BRANCH_LOCAL);
        var repo2Branch = repo2.LookupBranch(branch, Lg2BranchType.LG2_BRANCH_LOCAL);

        return repo1Branch.Compare(repo2Branch);
    }

    [Fact]
    public void CloneTautRepo0IntoRepo2()
    {
        _planner.SetupRepo0();
        _planner.SetupRepo1();

        var gitCli = Host.Services.GetRequiredService<GitCli>();

        Directory.SetCurrentDirectory(Scene.DirPath);
        gitCli.Run("clone", "--origin", Repo0, $"taut::{Repo0}", Repo2);

        Directory.SetCurrentDirectory(Repo2);

        UpdateContentAndPush(gitCli);

        using var repo2 = Lg2Repository.New(".");
        using var repo2Config = repo2.GetConfigSnapshot();
        var repo0SiteName = TautSiteConfig.FindSiteNameForRemote(repo2Config, Repo0);

        Directory.SetCurrentDirectory(Path.Join("..", Repo1));
        gitCli.Run("pull");

        Directory.SetCurrentDirectory("..");
        var repo0sitePath = GitRepoHelpers.GetTautSitePath(Repo2Git, repo0SiteName);
        using var repo0Site = Lg2Repository.New(repo0sitePath);
        using var repo0Base = Lg2Repository.New(Repo0);

        Assert.True(CompareBranch(repo0Base, repo0Site, "master"));
    }

    [Fact]
    public void CloneTautRepo0IntoRepo2_ViaHttp()
    {
        _planner.SetupRepo0();
        _planner.SetupRepo1();

        using var repo0Http = _planner.ServeRepo0();

        var repo0HttpUri = repo0Http.GetServingUri();

        testOutput.WriteLine($"Serving {Repo0} at {repo0HttpUri.AbsoluteUri}");

        var gitCli = Host.Services.GetRequiredService<GitCli>();

        Directory.SetCurrentDirectory(Scene.DirPath);
        gitCli.Run("clone", "--origin", Repo0, $"taut::{repo0HttpUri.AbsoluteUri}", Repo2);

        Directory.SetCurrentDirectory(Repo2);

        UpdateContentAndPush(gitCli);

        using var repo2 = Lg2Repository.New(".");
        using var repo2Config = repo2.GetConfigSnapshot();
        var repo0SiteName = TautSiteConfig.FindSiteNameForRemote(repo2Config, Repo0);

        Directory.SetCurrentDirectory(Path.Join("..", Repo1));
        gitCli.Run("pull");

        Directory.SetCurrentDirectory("..");
        var repo0sitePath = GitRepoHelpers.GetTautSitePath(Repo2Git, repo0SiteName);
        using var repo0Site = Lg2Repository.New(repo0sitePath);
        using var repo0Base = Lg2Repository.New(Repo0);

        Assert.True(CompareBranch(repo0Base, repo0Site, "master"));
    }

    [Fact]
    public void CloneTautRepo0IntoRepo2_WithTags()
    {
        _planner.SetupRepo0();
        _planner.SetupRepo1();
        _planner.ConfigRepo0WithTags();

        var gitCli = Host.Services.GetRequiredService<GitCli>();

        Directory.SetCurrentDirectory(Scene.DirPath);
        gitCli.Run("clone", "--origin", Repo0, $"taut::{Repo0}", Repo2);

        Directory.SetCurrentDirectory(Repo2);

        UpdateContentAndPush(gitCli);

        gitCli.Run("tag", "tag1", "HEAD");
        gitCli.Run("push");

        using var repo2 = Lg2Repository.New(".");
        using var repo2Config = repo2.GetConfigSnapshot();
        var repo0SiteName = TautSiteConfig.FindSiteNameForRemote(repo2Config, Repo0);

        Directory.SetCurrentDirectory(Path.Join("..", Repo1));
        gitCli.Run("pull");

        Directory.SetCurrentDirectory("..");
        var repo0sitePath = GitRepoHelpers.GetTautSitePath(Repo2Git, repo0SiteName);
        using var repo0Site = Lg2Repository.New(repo0sitePath);
        using var repo0Base = Lg2Repository.New(Repo0);

        Assert.True(CompareBranch(repo0Base, repo0Site, "master"));
    }

    [Fact]
    public void AddTautRepo0IntoRepo2()
    {
        _planner.SetupRepo0();
        _planner.SetupRepo1();

        var gitCli = Host.Services.GetRequiredService<GitCli>();

        Directory.SetCurrentDirectory(Scene.DirPath);
        gitCli.Run("init", Repo2);

        Directory.SetCurrentDirectory(Repo2);
        gitCli.Run("taut", "site", "add", Repo0, Path.Join("..", Repo0));

        using var repo2 = Lg2Repository.New(".");
        using var repo2Config = repo2.GetConfigSnapshot();
        var repo0SiteName = TautSiteConfig.FindSiteNameForRemote(repo2Config, Repo0);

        gitCli.Run("pull", Repo0);

        Directory.SetCurrentDirectory("..");

        var repo0sitePath = GitRepoHelpers.GetTautSitePath(Repo2Git, repo0SiteName);
        using var repo0Site = Lg2Repository.New(repo0sitePath);
        using var repo0Base = Lg2Repository.New(Repo0);

        Assert.True(CompareBranch(repo0Base, repo0Site, "master"));
    }

    [Fact]
    public void AddTautRepo1IntoRepo2_WithLinkToRepo0()
    {
        _planner.SetupRepo0();
        _planner.SetupRepo1();
        _planner.SetupRepo2();

        var gitCli = Host.Services.GetRequiredService<GitCli>();

        Directory.SetCurrentDirectory(Path.Join(Scene.DirPath, Repo2));
        gitCli.Run(
            "taut",
            "site",
            "--target",
            Repo0,
            "add",
            "--link-existing",
            Repo1,
            Path.Join("..", Repo1)
        );

        using var repo2 = Lg2Repository.New(".");
        using var repo2Config = repo2.GetConfigSnapshot();
        var repo1SiteName = TautSiteConfig.FindSiteNameForRemote(repo2Config, Repo1);

        Directory.SetCurrentDirectory("..");

        var repo1SitePath = GitRepoHelpers.GetTautSitePath(Repo2Git, repo1SiteName);
        using var repo1Site = Lg2Repository.New(repo1SitePath);
        using var repo1Base = Lg2Repository.New(Repo1);

        Assert.True(CompareBranch(repo1Base, repo1Site, "master"));
    }
}
