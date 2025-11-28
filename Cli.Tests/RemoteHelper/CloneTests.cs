using Cli.Tests.TestSupport;
using Git.Taut;
using Lg2.Sharpy;
using static Cli.Tests.TestSupport.TestScenePlanConstants;

namespace Cli.Tests.RemoteHelper;

public sealed class CloneTests(ITestOutputHelper testOutput) : IDisposable
{
    TestScenePlan _plan = new(testOutput);

    public void Dispose()
    {
        _plan.PreserveContentWhenFailed(testOutput);
        _plan.Dispose();
    }

    void UpdateContentAndPush(string repoRoot)
    {
        string a_md = "a.md";
        string b_tt = "b.tt";

        string a_md_content = "Not encrypted";
        string b_tt_content = "Encrypted";

        _plan.AddFile(repoRoot, a_md, a_md_content);
        _plan.AddFile(repoRoot, b_tt, b_tt_content);

        _plan.RunGit("-C", repoRoot, "add", "--all");
        _plan.RunGit("-C", repoRoot, "commit", "-m", $"{nameof(UpdateContentAndPush)}");
        _plan.RunGit("-C", repoRoot, "push");
    }

    void AssertSameBranchId(Lg2Repository repo1, Lg2Repository repo2, string branch)
    {
        var repo1Branch = repo1.LookupLocalBranch(branch);
        var repo2Branch = repo2.LookupLocalBranch(branch);

        Assert.True(repo1Branch.Compare(repo2Branch));
    }

    void AssertSiteHeadRef(Lg2Repository repo)
    {
        var headRef = repo.GetHead();
        var headRefName = headRef.GetName();
        var headRefType = headRef.GetRefType();

        Assert.Equal("HEAD", headRefName);

        Assert.True(headRefType.IsSymbolic());

        var symTarget = headRef.GetSymbolicTarget();
        Assert.Equal("refs/heads/master", symTarget);
    }

    [Fact]
    public void CloneTautRepo0IntoRepo2()
    {
        _plan.SetupRepo0();
        _plan.SetupRepo1();

        _plan.RunGit("-C", _plan.Location, "clone", "--origin", Repo0, $"taut::{Repo0}", Repo2);

        UpdateContentAndPush(_plan.Repo2Root);

        using var repo2 = Lg2Repository.New(_plan.Repo2Root);
        using var repo2Config = repo2.GetConfigSnapshot();
        var repo0SiteName = TautSiteConfig.FindSiteNameForRemote(repo2Config, Repo0);

        _plan.RunGit("-C", _plan.Repo1Root, "pull");

        var repo2SiteRepo0Root = GitRepoHelpers.GetTautSitePath(_plan.Repo2GitDir, repo0SiteName);
        using var repo2SiteRepo0 = Lg2Repository.New(repo2SiteRepo0Root);
        using var repo0Base = Lg2Repository.New(_plan.Repo0Root);

        AssertSameBranchId(repo0Base, repo2SiteRepo0, "master");
        // AssertSiteHeadRef(repo2SiteRepo0);
    }

    [Fact]
    public void CloneTautRepo0IntoRepo2_ViaHttp()
    {
        _plan.SetupRepo0();
        _plan.SetupRepo1();

        using var repo0Http = _plan.ServeRepo0();

        var repo0HttpUri = repo0Http.GetServingUri();

        testOutput.WriteLine($"Serving {Repo0} at {repo0HttpUri.AbsoluteUri}");

        _plan.RunGit(
            "-C",
            _plan.Location,
            "clone",
            "--origin",
            Repo0,
            $"taut::{repo0HttpUri.AbsoluteUri}",
            Repo2
        );

        UpdateContentAndPush(_plan.Repo2Root);

        using var repo2 = Lg2Repository.New(_plan.Repo2Root);
        using var repo2Config = repo2.GetConfigSnapshot();
        var repo0SiteName = TautSiteConfig.FindSiteNameForRemote(repo2Config, Repo0);

        _plan.RunGit("-C", _plan.Repo1Root, "pull");

        var repo2SiteRepo0Root = GitRepoHelpers.GetTautSitePath(_plan.Repo2GitDir, repo0SiteName);
        using var repo2SiteRepo0 = Lg2Repository.New(repo2SiteRepo0Root);
        using var repo0Base = Lg2Repository.New(_plan.Repo0Root);

        AssertSameBranchId(repo0Base, repo2SiteRepo0, "master");
    }

    [Fact]
    public void CloneTautRepo0IntoRepo2_ViaSsh()
    {
        _plan.SetupRepo0();
        _plan.SetupRepo1();

        using var repo0 = Lg2Repository.New(_plan.Repo0Root);

        UriBuilder uriBuilder = new()
        {
            Scheme = Uri.UriSchemeSsh,
            Host = "localhost",
            Path = repo0.GetPath(),
        };

        var repo0SshUri = uriBuilder.Uri;
        var tautRepo0Sshuri = $"taut::{repo0SshUri.AbsoluteUri}";

        _plan.RunGit("-C", _plan.Location, "clone", "--origin", Repo0, tautRepo0Sshuri, Repo2);

        UpdateContentAndPush(_plan.Repo2Root);

        using var repo2 = Lg2Repository.New(_plan.Repo2Root);
        using var repo2Config = repo2.GetConfigSnapshot();
        var repo0SiteName = TautSiteConfig.FindSiteNameForRemote(repo2Config, Repo0);

        _plan.RunGit("-C", _plan.Repo1Root, "pull");

        var repo2SiteRepo0Root = GitRepoHelpers.GetTautSitePath(_plan.Repo2GitDir, repo0SiteName);
        using var repo0Site = Lg2Repository.New(repo2SiteRepo0Root);
        using var repo0Base = Lg2Repository.New(_plan.Repo0Root);

        AssertSameBranchId(repo0Base, repo0Site, "master");
    }

    [Fact]
    public void CloneTautRepo0IntoRepo2_WithTags()
    {
        _plan.SetupRepo0();
        _plan.SetupRepo1();
        _plan.ConfigRepo0WithTags();

        _plan.RunGit("-C", _plan.Location, "clone", "--origin", Repo0, $"taut::{Repo0}", Repo2);

        UpdateContentAndPush(_plan.Repo2Root);

        _plan.RunGit("-C", _plan.Repo2Root, "tag", "tag1", "HEAD");
        _plan.RunGit("-C", _plan.Repo2Root, "push");

        using var repo2 = Lg2Repository.New(_plan.Repo2Root);
        using var repo2Config = repo2.GetConfigSnapshot();
        var repo0SiteName = TautSiteConfig.FindSiteNameForRemote(repo2Config, Repo0);

        _plan.RunGit("-C", _plan.Repo1Root, "pull");

        var repo0sitePath = GitRepoHelpers.GetTautSitePath(_plan.Repo2GitDir, repo0SiteName);
        using var repo0Site = Lg2Repository.New(repo0sitePath);
        using var repo0Base = Lg2Repository.New(_plan.Repo0Root);

        AssertSameBranchId(repo0Base, repo0Site, "master");
    }

    [Fact]
    public void AddTautRepo0IntoRepo2()
    {
        _plan.SetupRepo0();
        _plan.SetupRepo1();

        _plan.RunGit("-C", _plan.Location, "init", Repo2);

        _plan.RunGit("-C", _plan.Repo2Root, "taut", "site", "add", Repo0, Path.Join("..", Repo0));

        using var repo2 = Lg2Repository.New(_plan.Repo2Root);
        using var repo2Config = repo2.GetConfigSnapshot();
        var repo0SiteName = TautSiteConfig.FindSiteNameForRemote(repo2Config, Repo0);

        _plan.RunGit("-C", _plan.Repo2Root, "pull", Repo0);

        var repo0sitePath = GitRepoHelpers.GetTautSitePath(_plan.Repo2GitDir, repo0SiteName);
        using var repo0Site = Lg2Repository.New(repo0sitePath);
        using var repo0Base = Lg2Repository.New(_plan.Repo0Root);

        AssertSameBranchId(repo0Base, repo0Site, "master");
    }

    [Fact]
    public void AddTautRepo1IntoRepo2_WithLinkToRepo0()
    {
        _plan.SetupRepo0();
        _plan.SetupRepo1();
        _plan.SetupRepo2();

        _plan.RunGit(
            "-C",
            _plan.Repo2Root,
            "taut",
            "site",
            "--target",
            Repo0,
            "add",
            "--link-existing",
            Repo1,
            Path.Join("..", Repo1)
        );

        using var repo2 = Lg2Repository.New(_plan.Repo2Root);
        using var repo2Config = repo2.GetConfigSnapshot();
        var repo2SiteRepo1Name = TautSiteConfig.FindSiteNameForRemote(repo2Config, Repo1);

        var repo2SiteRepo1Root = GitRepoHelpers.GetTautSitePath(
            _plan.Repo2GitDir,
            repo2SiteRepo1Name
        );
        using var repo2SiteRepo1 = Lg2Repository.New(repo2SiteRepo1Root);
        using var repo1Base = Lg2Repository.New(_plan.Repo1Root);

        AssertSameBranchId(repo1Base, repo2SiteRepo1, "master");
    }
}
