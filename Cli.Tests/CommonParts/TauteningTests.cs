using Cli.Tests.TestSupport;
using Git.Taut;
using Lg2.Sharpy;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Cli.Tests.CommonParts;

[Collection("SetCurrentDirectory")]
public class TauteningTests(ITestOutputHelper testOutput) : IDisposable
{
    IHost _host => _plan.Host;

    TestScenePlan _plan = new(testOutput);

    public void Dispose()
    {
        var tautSetup = _host.Services.GetRequiredService<TautSetup>();
        tautSetup.Dispose();

        _plan.PreserveContentWhenFailed(testOutput);
        _plan.Dispose();
    }

    [Fact]
    public void CheckTautAttr()
    {
        _plan.SetupRepo0();
        _plan.SetupRepo1();

        Directory.SetCurrentDirectory(_plan.DirPath);

        var gitCli = _host.Services.GetRequiredService<GitCli>();

        gitCli.Run("clone", "taut::repo0", "repo2");

        Directory.SetCurrentDirectory("repo2");

        var repo2 = Lg2Repository.New(".");

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
    public void TautenCommitIntoSame()
    {
        _plan.SetupRepo0();
        _plan.SetupRepo1();

        Directory.SetCurrentDirectory(_plan.DirPath);

        var gitCli = _host.Services.GetRequiredService<GitCli>();

        gitCli.Run("clone", "taut::repo0", "repo2");

        var hostRepo = Lg2Repository.New("repo2");

        Directory.SetCurrentDirectory("repo2");

        string a_md = "a.md";
        string a_md_content = "Not encrypted";

        File.AppendAllText(a_md, a_md_content);

        gitCli.Run("add", "--all");
        gitCli.Run("commit", "-m", a_md);

        var tautSetup = _host.Services.GetRequiredService<TautSetup>();

        using var config = hostRepo.GetConfigSnapshot();
        var tautRepoName = TautSiteConfig.FindSiteNameForRemote(config, "origin");
        tautSetup.GearUpExisting(hostRepo, "origin", tautRepoName);

        var tautManager = _host.Services.GetRequiredService<TautManager>();

        var hostHeadCommit = tautManager.HostRepo.GetHeadCommit();

        tautManager.TautenCommit(hostHeadCommit);

        var kvStore = _host.Services.GetRequiredService<TautMapping>();

        Lg2Oid resultOid = new();
        Assert.True(kvStore.TryGetTautened(hostHeadCommit, ref resultOid));
        Assert.True(hostHeadCommit.GetOidPlainRef().Equals(resultOid));
    }

    [Fact]
    public void TautenCommitIntoDifferent()
    {
        _plan.SetupRepo0();
        _plan.SetupRepo1();

        Directory.SetCurrentDirectory(_plan.DirPath);

        var gitCli = _host.Services.GetRequiredService<GitCli>();

        gitCli.Run("clone", "taut::repo0", "repo2");

        var hostRepo = Lg2Repository.New("repo2");

        Directory.SetCurrentDirectory("repo2");

        string a_tt = "a.tt";
        string a_tt_content = "Encrypted";

        File.AppendAllText(a_tt, a_tt_content);

        gitCli.Run("add", "--all");
        gitCli.Run("commit", "-m", a_tt);

        var tautSetup = _host.Services.GetRequiredService<TautSetup>();

        using var config = hostRepo.GetConfigSnapshot();
        var tautRepoName = TautSiteConfig.FindSiteNameForRemote(config, "origin");
        tautSetup.GearUpExisting(hostRepo, "origin", tautRepoName);

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
