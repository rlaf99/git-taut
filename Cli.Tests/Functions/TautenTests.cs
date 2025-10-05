using Cli.Tests.Support;
using Git.Taut;
using Lg2.Sharpy;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Cli.Tests.Functions;

[Collection("GitTautPaths")]
public class TauteningTests : IDisposable
{
    ITestOutputHelper _output;
    IHost _host;

    TestScene _scene;

    public TauteningTests(ITestOutputHelper output, HostBuilderFixture hostBuilder)
    {
        _output = output;
        _host = hostBuilder.BuildHost(_output);

        _scene = new TestScene();
        _scene.SetupRepo0(_host);
        _scene.SetupRepo1(_host);
    }

    public void Dispose()
    {
        var tautSetup = _host.Services.GetRequiredService<TautSetup>();
        tautSetup.Dispose();

        _scene.PreserveContentWhenFailed(_output);
        _scene.Dispose();
    }

    [Fact]
    public void CheckTautAttr()
    {
        Directory.SetCurrentDirectory(_scene.DirPath);

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
        Directory.SetCurrentDirectory(_scene.DirPath);

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

        var tautRepoName = hostRepo.FindTautBaseName("origin");
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
        Directory.SetCurrentDirectory(_scene.DirPath);

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

        var tautRepoName = hostRepo.FindTautBaseName("origin");
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
