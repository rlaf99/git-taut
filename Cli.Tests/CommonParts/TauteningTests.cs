using Cli.Tests.TestSupport;
using Git.Taut;
using Lg2.Sharpy;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using static Cli.Tests.TestSupport.TestScenePlanConstants;

namespace Cli.Tests.CommonParts;

public class TauteningTests(ITestOutputHelper testOutput) : IDisposable
{
    IHost _host => _plan.Host;

    TestScenePlan _plan = new(testOutput);

    public void Dispose()
    {
        _plan.PreserveContentWhenFailed(testOutput);
        _plan.Dispose();
    }

    [Fact]
    public void CheckTautAttr()
    {
        _plan.SetupRepo0();
        _plan.SetupRepo1();

        _plan.RunGitOnRoot("clone", $"taut::{Repo0}", Repo2);

        using var repo2 = Lg2Repository.New(_plan.Repo2Root);

        var repo2Head = repo2.GetHead();

        Lg2AttrOptions attrOpts = new()
        {
            Flags =
                Lg2AttrCheckFlags.LG2_ATTR_CHECK_NO_SYSTEM
                | Lg2AttrCheckFlags.LG2_ATTR_CHECK_INCLUDE_COMMIT,
        };

        attrOpts.SetCommitId(repo2Head.GetTarget());

        {
            var attrVal = repo2.GetTautAttrValue("a.tt", attrOpts);
            Assert.True(attrVal.IsSetOrSpecified);
        }

        {
            var attrVal = repo2.GetTautAttrValue("1tt", attrOpts);
            Assert.False(attrVal.IsSetOrSpecified);
        }

        {
            var attrVal = repo2.GetTautAttrValue("tt", attrOpts);
            Assert.True(attrVal.IsSetOrSpecified);
        }

        {
            var attrVal = repo2.GetTautAttrValue("tt/a", attrOpts);
            Assert.True(attrVal.IsSetOrSpecified);
        }
    }

    [Fact]
    public void TautenCommitIntoSameObject()
    {
        _plan.SetupRepo0();
        _plan.SetupRepo1();

        _plan.RunGitOnRoot("clone", $"taut::{Repo0}", Repo2);

        using var hostRepo = Lg2Repository.New(_plan.Repo2Root);

        string a_md = "a.md";
        string a_md_content = "Not encrypted";

        _plan.AddFileOnRepo2(a_md, a_md_content);

        _plan.RunGitOnRepo2("add", "--all");
        _plan.RunGitOnRepo2("commit", "-m", a_md);

        using var tautSetup = _host.Services.GetRequiredService<TautSetup>();

        var tautRepoName = hostRepo.FindTautSiteNameForRemote(Origin);
        tautSetup.GearUpExisting(hostRepo, Origin, tautRepoName);

        var tautManager = _host.Services.GetRequiredService<TautManager>();

        var hostHeadCommit = tautManager.HostRepo.GetHeadCommit();

        tautManager.TautenCommit(hostHeadCommit);

        var kvStore = _host.Services.GetRequiredService<TautMapping>();

        Lg2Oid resultOid = new();
        Assert.True(kvStore.TryGetTautened(hostHeadCommit, ref resultOid));
        Assert.True(hostHeadCommit.GetOidPlainRef().Equals(resultOid));
    }

    [Fact]
    public void TautenCommitIntoDifferentObjects()
    {
        _plan.SetupRepo0();
        _plan.SetupRepo1();

        _plan.RunGitOnRoot("clone", $"taut::{Repo0}", Repo2);

        using var hostRepo = Lg2Repository.New(_plan.Repo2Root);

        string a_tt = "a.tt";
        string a_tt_content = "Encrypted";

        _plan.AddFileOnRepo2(a_tt, a_tt_content);

        _plan.RunGitOnRepo2("add", "--all");
        _plan.RunGitOnRepo2("commit", "-m", a_tt);

        using var tautSetup = _host.Services.GetRequiredService<TautSetup>();

        var tautRepoName = hostRepo.FindTautSiteNameForRemote(Origin);
        tautSetup.GearUpExisting(hostRepo, Origin, tautRepoName);

        var tautManager = _host.Services.GetRequiredService<TautManager>();

        var hostHeadCommit = tautManager.HostRepo.GetHeadCommit();

        tautManager.TautenCommit(hostHeadCommit);

        var kvStore = _host.Services.GetRequiredService<TautMapping>();

        Lg2Oid resultOid = new();
        Assert.True(kvStore.TryGetTautened(hostHeadCommit, ref resultOid));
        Assert.False(hostHeadCommit.GetOidPlainRef().Equals(resultOid));

        var tautenedCommit = tautManager.TautRepo.LookupCommit(resultOid);
        tautManager.RegainCommit(tautenedCommit);
    }

    [Fact]
    public void TautenSubFilesIncludingDirectory()
    {
        _plan.SetupRepo0();
        _plan.SetupRepo1();

        _plan.RunGitOnRoot("clone", $"taut::{Repo0}", Repo2);

        var tt = "tt";

        _plan.AddDirectoryOnRepo2(tt);

        var tt_a_md = Path.Join(tt, "a.md");
        var tt_a_md_content = "Encrypted";

        _plan.AddFileOnRepo2(tt_a_md, tt_a_md_content);

        _plan.RunGitOnRepo2("add", "--all");
        _plan.RunGitOnRepo2("commit", "-m", tt);

        using var tautSetup = _host.Services.GetRequiredService<TautSetup>();

        using var hostRepo = Lg2Repository.New(_plan.Repo2Root);

        var tautRepoName = hostRepo.FindTautSiteNameForRemote(Origin);
        tautSetup.GearUpExisting(hostRepo, Origin, tautRepoName);

        var tautManager = _host.Services.GetRequiredService<TautManager>();

        var hostHeadCommit = tautManager.HostRepo.GetHeadCommit();

        tautManager.TautenCommit(hostHeadCommit);

        var kvStore = _host.Services.GetRequiredService<TautMapping>();

        Lg2Oid resultOid = new();
        Assert.True(kvStore.TryGetTautened(hostHeadCommit, ref resultOid));
        Assert.False(hostHeadCommit.GetOidPlainRef().Equals(resultOid));

        var tautenedCommit = tautManager.TautRepo.LookupCommit(resultOid);
        tautManager.RegainCommit(tautenedCommit);
    }

    [Fact]
    public void TautenSubFilesExcludsingDirectory()
    {
        _plan.SetupRepo0();
        _plan.SetupRepo1();

        _plan.RunGitOnRoot("clone", $"taut::{Repo0}", Repo2);

        var dd = "dd";

        _plan.AddDirectoryOnRepo2(dd);

        var dd_a_md = Path.Join(dd, "a.md");
        var dd_a_md_content = "Encrypted";

        _plan.AddFileOnRepo2(dd_a_md, dd_a_md_content);

        _plan.RunGitOnRepo2("add", "--all");
        _plan.RunGitOnRepo2("commit", "-m", dd);

        using var tautSetup = _host.Services.GetRequiredService<TautSetup>();

        using var hostRepo = Lg2Repository.New(_plan.Repo2Root);

        var tautRepoName = hostRepo.FindTautSiteNameForRemote(Origin);
        tautSetup.GearUpExisting(hostRepo, Origin, tautRepoName);

        var tautManager = _host.Services.GetRequiredService<TautManager>();

        var hostHeadCommit = tautManager.HostRepo.GetHeadCommit();

        tautManager.TautenCommit(hostHeadCommit);

        var kvStore = _host.Services.GetRequiredService<TautMapping>();

        Lg2Oid resultOid = new();
        Assert.True(kvStore.TryGetTautened(hostHeadCommit, ref resultOid));
        Assert.False(hostHeadCommit.GetOidPlainRef().Equals(resultOid));

        var tautenedCommit = tautManager.TautRepo.LookupCommit(resultOid);
        tautManager.RegainCommit(tautenedCommit);
    }
}
