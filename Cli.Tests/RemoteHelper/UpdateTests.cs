using Cli.Tests.TestSupport;
using Git.Taut;
using Lg2.Sharpy;
using static Cli.Tests.TestSupport.TestScenePlanConstants;

namespace Cli.Tests.RemoteHelper;

public sealed class UpdateTests(ITestOutputHelper testOutput) : IDisposable
{
    TestScenePlan _plan = new(testOutput);

    public void Dispose()
    {
        _plan.PreserveContentWhenFailed(testOutput);
        _plan.Dispose();
    }

    const string _a_md = "a.md";
    const string _a_md_content = "a.md";

    const string _b_tt = "b.tt";
    const string _b_tt_content = "b.tt";

    void AssertSameBranchId(Lg2Repository aRepo, Lg2Repository bRepo, string branch)
    {
        var aRepoBranch = aRepo.LookupLocalBranch(branch);
        var bRepoBranch = bRepo.LookupLocalBranch(branch);

        Assert.True(aRepoBranch.Compare(bRepoBranch));
    }

    void AssertSameTagId(Lg2Repository aRepo, Lg2Repository bRepo, string tagName)
    {
        var aRepoTagRef = aRepo.LookupRef(tagName, shorthand: true);
        var bRepoTagRef = bRepo.LookupRef(tagName, shorthand: true);

        Assert.True(aRepoTagRef.IsTag());
        Assert.True(bRepoTagRef.IsTag());

        Assert.True(aRepoTagRef.Compare(bRepoTagRef));
    }

    [Fact]
    public void AddPlain_AddTautened()
    {
        _plan.SetupRepo0();
        _plan.SetupRepo1();
        _plan.SetupRepo2();

        using var repo0 = Lg2Repository.New(_plan.Repo0Root);
        using var repo2 = Lg2Repository.New(_plan.Repo2Root);
        using var repo2SiteRepo0 = TautSiteConfig.OpenSiteForRemote(repo2, Repo0);

        _plan.AddFileOnRepo2(_a_md, _a_md_content);

        _plan.RunGitOnRepo2("add", "--all");
        _plan.RunGitOnRepo2("commit", "-m", _a_md);
        _plan.RunGitOnRepo2("push");

        AssertSameBranchId(repo0, repo2SiteRepo0, Master);

        _plan.AddFileOnRepo2(_b_tt, _b_tt_content);

        _plan.RunGitOnRepo2("add", "--all");
        _plan.RunGitOnRepo2("commit", "-m", _b_tt);
        _plan.RunGitOnRepo2("push");

        AssertSameBranchId(repo0, repo2SiteRepo0, Master);
    }

    [Fact]
    public void AddPlain_SwitchBranch_AddTautened()
    {
        _plan.SetupRepo0();
        _plan.SetupRepo1();
        _plan.SetupRepo2();

        using var repo0 = Lg2Repository.New(_plan.Repo0Root);
        using var repo2 = Lg2Repository.New(_plan.Repo2Root);
        using var repo2SiteRepo0 = TautSiteConfig.OpenSiteForRemote(repo2, Repo0);

        _plan.AddFileOnRepo2(_a_md, _a_md_content);

        _plan.RunGitOnRepo2("add", "--all");
        _plan.RunGitOnRepo2("commit", "-m", _a_md);
        _plan.RunGitOnRepo2("push");

        AssertSameBranchId(repo0, repo2SiteRepo0, Master);

        _plan.RunGitOnRepo2("checkout", "-b", Branch1);

        _plan.AddFileOnRepo2(_b_tt, _b_tt_content);

        _plan.RunGitOnRepo2("add", "--all");
        _plan.RunGitOnRepo2("commit", "-m", _b_tt);
        _plan.RunGitOnRepo2("push", "--set-upstream", Repo0, Branch1);

        AssertSameBranchId(repo0, repo2SiteRepo0, Branch1);
    }

    [Fact]
    public void AddPlain_AddTags_AddTautened()
    {
        _plan.SetupRepo0();
        _plan.SetupRepo1();
        _plan.SetupRepo2();

        using var repo0 = Lg2Repository.New(_plan.Repo0Root);
        using var repo2 = Lg2Repository.New(_plan.Repo2Root);
        using var repo2SiteRepo0 = TautSiteConfig.OpenSiteForRemote(repo2, Repo0);

        _plan.AddFileOnRepo2(_a_md, _a_md_content);

        _plan.RunGitOnRepo2("add", "--all");
        _plan.RunGitOnRepo2("commit", "-m", _a_md);
        _plan.RunGitOnRepo2("push");

        _plan.RunGitOnRepo2("tag", Tag1);
        _plan.RunGitOnRepo2("tag", "-a", "-m", AnnotatedTag1, AnnotatedTag1);
        _plan.RunGitOnRepo2("push", "--tags");

        AssertSameBranchId(repo0, repo2SiteRepo0, Master);
        AssertSameTagId(repo0, repo2SiteRepo0, Tag1);
        AssertSameTagId(repo0, repo2SiteRepo0, AnnotatedTag1);

        _plan.AddFileOnRepo2(_b_tt, _b_tt_content);

        _plan.RunGitOnRepo2("add", "--all");
        _plan.RunGitOnRepo2("commit", "-m", _b_tt);
        _plan.RunGitOnRepo2("push", "--set-upstream", Repo0);

        _plan.RunGitOnRepo2("tag", Tag2);
        _plan.RunGitOnRepo2("tag", "-a", "-m", AnnotatedTag2, AnnotatedTag2);
        _plan.RunGitOnRepo2("push", "--tags");

        AssertSameBranchId(repo0, repo2SiteRepo0, Master);
        AssertSameTagId(repo0, repo2SiteRepo0, Tag2);
        AssertSameTagId(repo0, repo2SiteRepo0, AnnotatedTag2);
    }

    [Fact]
    public void AddTautened_AddPlain()
    {
        _plan.SetupRepo0();
        _plan.SetupRepo1();
        _plan.SetupRepo2();

        using var repo0 = Lg2Repository.New(_plan.Repo0Root);
        using var repo2 = Lg2Repository.New(_plan.Repo2Root);
        using var repo2SiteRepo0 = TautSiteConfig.OpenSiteForRemote(repo2, Repo0);

        _plan.AddFileOnRepo2(_b_tt, _b_tt_content);

        _plan.RunGitOnRepo2("add", "--all");
        _plan.RunGitOnRepo2("commit", "-m", _b_tt);
        _plan.RunGitOnRepo2("push");

        AssertSameBranchId(repo0, repo2SiteRepo0, Master);

        _plan.AddFileOnRepo2(_a_md, _a_md_content);

        _plan.RunGitOnRepo2("add", "--all");
        _plan.RunGitOnRepo2("commit", "-m", _a_md);
        _plan.RunGitOnRepo2("push");

        AssertSameBranchId(repo0, repo2SiteRepo0, Master);
    }

    [Fact]
    public void AddTautened_SwitchBranch_AddPlain()
    {
        _plan.SetupRepo0();
        _plan.SetupRepo1();
        _plan.SetupRepo2();

        using var repo0 = Lg2Repository.New(_plan.Repo0Root);
        using var repo2 = Lg2Repository.New(_plan.Repo2Root);
        using var repo2SiteRepo0 = TautSiteConfig.OpenSiteForRemote(repo2, Repo0);

        _plan.AddFileOnRepo2(_b_tt, _b_tt_content);

        _plan.RunGitOnRepo2("add", "--all");
        _plan.RunGitOnRepo2("commit", "-m", _b_tt);
        _plan.RunGitOnRepo2("push");

        AssertSameBranchId(repo0, repo2SiteRepo0, Master);

        _plan.RunGitOnRepo2("checkout", "-b", Branch1);

        _plan.AddFileOnRepo2(_a_md, _a_md_content);

        _plan.RunGitOnRepo2("add", "--all");
        _plan.RunGitOnRepo2("commit", "-m", _a_md);
        _plan.RunGitOnRepo2("push", "--set-upstream", Repo0, Branch1);

        AssertSameBranchId(repo0, repo2SiteRepo0, Master);
    }

    [Fact]
    public void AddTautened_AddTags_AddPlain()
    {
        _plan.SetupRepo0();
        _plan.SetupRepo1();
        _plan.SetupRepo2();

        using var repo0 = Lg2Repository.New(_plan.Repo0Root);
        using var repo2 = Lg2Repository.New(_plan.Repo2Root);
        using var repo2SiteRepo0 = TautSiteConfig.OpenSiteForRemote(repo2, Repo0);

        _plan.AddFileOnRepo2(_b_tt, _b_tt_content);

        _plan.RunGitOnRepo2("add", "--all");
        _plan.RunGitOnRepo2("commit", "-m", _b_tt);
        _plan.RunGitOnRepo2("push");

        _plan.RunGitOnRepo2("tag", Tag1);
        _plan.RunGitOnRepo2("tag", "-a", "-m", AnnotatedTag1, AnnotatedTag1);
        _plan.RunGitOnRepo2("push", "--tags");

        AssertSameBranchId(repo0, repo2SiteRepo0, Master);
        AssertSameTagId(repo0, repo2SiteRepo0, Tag1);
        AssertSameTagId(repo0, repo2SiteRepo0, AnnotatedTag1);

        _plan.AddFileOnRepo2(_a_md, _a_md_content);

        _plan.RunGitOnRepo2("add", "--all");
        _plan.RunGitOnRepo2("commit", "-m", _a_md);
        _plan.RunGitOnRepo2("push");

        _plan.RunGitOnRepo2("tag", Tag2);
        _plan.RunGitOnRepo2("tag", "-a", "-m", AnnotatedTag2, AnnotatedTag2);
        _plan.RunGitOnRepo2("push", "--tags");

        AssertSameBranchId(repo0, repo2SiteRepo0, Master);
        AssertSameTagId(repo0, repo2SiteRepo0, Tag2);
        AssertSameTagId(repo0, repo2SiteRepo0, AnnotatedTag2);
    }
}
