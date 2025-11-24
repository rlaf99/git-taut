using Cli.Tests.TestSupport;
using Git.Taut;
using Lg2.Sharpy;
using static Cli.Tests.TestSupport.TestScenePlanConstants;

namespace Cli.Tests.RemoteHelper;

[Collection("SetCurrentDirectory")]
public sealed class CloneTests(ITestOutputHelper testOutput) : IDisposable
{
    TestScenePlan _plan = new(testOutput);

    public void Dispose()
    {
        _plan.PreserveContentWhenFailed(testOutput);
        _plan.Dispose();
    }

    void UpdateContentAndPush()
    {
        string a_md = "a.md";
        string b_tt = "b.tt";

        string a_md_content = "Not encrypted";
        string b_tt_content = "Encrypted";

        File.AppendAllText(a_md, a_md_content);
        File.AppendAllText(b_tt, b_tt_content);

        _plan.RunGit("add", "--all");
        _plan.RunGit("commit", "-m", $"{nameof(UpdateContentAndPush)}");
        _plan.RunGit("push");
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
        _plan.SetupRepo0();
        _plan.SetupRepo1();

        Directory.SetCurrentDirectory(_plan.DirPath);
        _plan.RunGit("clone", "--origin", Repo0, $"taut::{Repo0}", Repo2);

        Directory.SetCurrentDirectory(Repo2);

        UpdateContentAndPush();

        using var repo2 = Lg2Repository.New(".");
        using var repo2Config = repo2.GetConfigSnapshot();
        var repo0SiteName = TautSiteConfig.FindSiteNameForRemote(repo2Config, Repo0);

        Directory.SetCurrentDirectory(Path.Join("..", Repo1));
        _plan.RunGit("pull");

        Directory.SetCurrentDirectory("..");
        var repo0sitePath = GitRepoHelpers.GetTautSitePath(Repo2Git, repo0SiteName);
        using var repo0Site = Lg2Repository.New(repo0sitePath);
        using var repo0Base = Lg2Repository.New(Repo0);

        Assert.True(CompareBranch(repo0Base, repo0Site, "master"));
    }

    [Fact]
    public void CloneTautRepo0IntoRepo2_ViaHttp()
    {
        _plan.SetupRepo0();
        _plan.SetupRepo1();

        using var repo0Http = _plan.ServeRepo0();

        var repo0HttpUri = repo0Http.GetServingUri();

        testOutput.WriteLine($"Serving {Repo0} at {repo0HttpUri.AbsoluteUri}");

        Directory.SetCurrentDirectory(_plan.DirPath);
        _plan.RunGit("clone", "--origin", Repo0, $"taut::{repo0HttpUri.AbsoluteUri}", Repo2);

        Directory.SetCurrentDirectory(Repo2);

        UpdateContentAndPush();

        using var repo2 = Lg2Repository.New(".");
        using var repo2Config = repo2.GetConfigSnapshot();
        var repo0SiteName = TautSiteConfig.FindSiteNameForRemote(repo2Config, Repo0);

        Directory.SetCurrentDirectory(Path.Join("..", Repo1));
        _plan.RunGit("pull");

        Directory.SetCurrentDirectory("..");
        var repo0sitePath = GitRepoHelpers.GetTautSitePath(Repo2Git, repo0SiteName);
        using var repo0Site = Lg2Repository.New(repo0sitePath);
        using var repo0Base = Lg2Repository.New(Repo0);

        Assert.True(CompareBranch(repo0Base, repo0Site, "master"));
    }

    [Fact]
    public void CloneTautRepo0IntoRepo2_ViaSsh()
    {
        _plan.SetupRepo0();
        _plan.SetupRepo1();

        Directory.SetCurrentDirectory(_plan.DirPath);

        using var repo0 = Lg2Repository.New(Repo0);

        UriBuilder uriBuilder = new()
        {
            Scheme = Uri.UriSchemeSsh,
            Host = "localhost",
            Path = repo0.GetPath(),
        };

        var repo0SshUri = uriBuilder.Uri;
        var tautRepo0Sshuri = $"taut::{repo0SshUri.AbsoluteUri}";

        testOutput.WriteLine($"Clone from {tautRepo0Sshuri}");

        _plan.RunGit("clone", "--origin", Repo0, tautRepo0Sshuri, Repo2);

        Directory.SetCurrentDirectory(Repo2);

        UpdateContentAndPush();

        using var repo2 = Lg2Repository.New(".");
        using var repo2Config = repo2.GetConfigSnapshot();
        var repo0SiteName = TautSiteConfig.FindSiteNameForRemote(repo2Config, Repo0);

        Directory.SetCurrentDirectory(Path.Join("..", Repo1));
        _plan.RunGit("pull");

        Directory.SetCurrentDirectory("..");
        var repo0sitePath = GitRepoHelpers.GetTautSitePath(Repo2Git, repo0SiteName);
        using var repo0Site = Lg2Repository.New(repo0sitePath);
        using var repo0Base = Lg2Repository.New(Repo0);

        Assert.True(CompareBranch(repo0Base, repo0Site, "master"));
    }

    [Fact]
    public void CloneTautRepo0IntoRepo2_WithTags()
    {
        _plan.SetupRepo0();
        _plan.SetupRepo1();
        _plan.ConfigRepo0WithTags();

        Directory.SetCurrentDirectory(_plan.DirPath);
        _plan.RunGit("clone", "--origin", Repo0, $"taut::{Repo0}", Repo2);

        Directory.SetCurrentDirectory(Repo2);

        UpdateContentAndPush();

        _plan.RunGit("tag", "tag1", "HEAD");
        _plan.RunGit("push");

        using var repo2 = Lg2Repository.New(".");
        using var repo2Config = repo2.GetConfigSnapshot();
        var repo0SiteName = TautSiteConfig.FindSiteNameForRemote(repo2Config, Repo0);

        Directory.SetCurrentDirectory(Path.Join("..", Repo1));
        _plan.RunGit("pull");

        Directory.SetCurrentDirectory("..");
        var repo0sitePath = GitRepoHelpers.GetTautSitePath(Repo2Git, repo0SiteName);
        using var repo0Site = Lg2Repository.New(repo0sitePath);
        using var repo0Base = Lg2Repository.New(Repo0);

        Assert.True(CompareBranch(repo0Base, repo0Site, "master"));
    }

    [Fact]
    public void AddTautRepo0IntoRepo2()
    {
        _plan.SetupRepo0();
        _plan.SetupRepo1();

        Directory.SetCurrentDirectory(_plan.DirPath);
        _plan.RunGit("init", Repo2);

        Directory.SetCurrentDirectory(Repo2);
        _plan.RunGit("taut", "site", "add", Repo0, Path.Join("..", Repo0));

        using var repo2 = Lg2Repository.New(".");
        using var repo2Config = repo2.GetConfigSnapshot();
        var repo0SiteName = TautSiteConfig.FindSiteNameForRemote(repo2Config, Repo0);

        _plan.RunGit("pull", Repo0);

        Directory.SetCurrentDirectory("..");

        var repo0sitePath = GitRepoHelpers.GetTautSitePath(Repo2Git, repo0SiteName);
        using var repo0Site = Lg2Repository.New(repo0sitePath);
        using var repo0Base = Lg2Repository.New(Repo0);

        Assert.True(CompareBranch(repo0Base, repo0Site, "master"));
    }

    [Fact]
    public void AddTautRepo1IntoRepo2_WithLinkToRepo0()
    {
        _plan.SetupRepo0();
        _plan.SetupRepo1();
        _plan.SetupRepo2();

        Directory.SetCurrentDirectory(Path.Join(_plan.DirPath, Repo2));
        _plan.RunGit(
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
