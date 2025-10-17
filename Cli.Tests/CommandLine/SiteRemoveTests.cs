using System.CommandLine;
using Cli.Tests.TestSupport;
using Git.Taut;
using Lg2.Sharpy;
using Microsoft.Extensions.Hosting;
using ProgramHelpers;
using static Cli.Tests.TestSupport.TestScenePlannerConstants;

namespace Cli.Tests.CommandLine;

[Collection("WithGitTautPaths")]
public sealed class SiteRemoveTests(ITestOutputHelper testOutput) : IDisposable
{
    IHost _host = GitTautHostBuilder.BuildHost();

    InvocationConfiguration _invCfg = new()
    {
        EnableDefaultExceptionHandler = false,
        Output = new StringWriter(),
        Error = new StringWriter(),
    };

    TestScene _scene = new();

    public void Dispose()
    {
        _host.Dispose();
        _scene.PreserveContentWhenFailed(testOutput);
        _scene.Dispose();
    }

    [Fact]
    public void TargetNotSpecified()
    {
        _scene.SetupRepo0(_host);
        _scene.SetupRepo1(_host);
        _scene.SetupRepo2(_host);

        var repo2Path = Path.Join(_scene.DirPath, Repo2);
        Directory.SetCurrentDirectory(repo2Path);

        ProgramCommandLine progCli = new(_host);

        string[] cliArgs = ["site", "remove"];
        var parseResult = progCli.Parse(cliArgs);

        var exitCode = parseResult.Invoke(_invCfg);
        Assert.NotEqual(0, exitCode);

        var wantedError =
            $"Option {ProgramCommandLine.SiteTargetOption.Name} is not specified"
            + Environment.NewLine;
        var actualError = _invCfg.Error.ToString();

        Assert.Equal(wantedError, actualError);
    }

    [Fact]
    public void RemoveRepo0()
    {
        _scene.SetupRepo0(_host);
        _scene.SetupRepo1(_host);
        _scene.SetupRepo2(_host);

        var repo2Path = Path.Join(_scene.DirPath, Repo2);
        Directory.SetCurrentDirectory(repo2Path);

        using var hostRepo = Lg2Repository.New(".");
        using var hostConfig = hostRepo.GetConfig();

        var repo0SiteName = TautSiteConfig.FindSiteNameForRemote(hostConfig, Repo0);

        ProgramCommandLine progCli = new(_host);

        string[] targetOpt = ["--target", Repo0];
        string[] cliArgs = ["site", .. targetOpt, "remove"];
        var parseResult = progCli.Parse(cliArgs);

        var exitCode = parseResult.Invoke(_invCfg);
        Assert.Equal(0, exitCode);

        Assert.False(TautSiteConfig.TryFindSiteNameForRemote(hostConfig, Repo0, out _));

        var repo0SitePath = hostRepo.GetTautSitePath(repo0SiteName);
        Assert.False(Directory.Exists(repo0SitePath));
    }

    [Fact]
    public void RemoveRepo0_TargetLinkedByOther()
    {
        _scene.SetupRepo0(_host);
        _scene.SetupRepo1(_host);
        _scene.SetupRepo2(_host);
        _scene.ConfigRepo2AddingRepo1WithLinkToRepo0(_host);

        var repo2Path = Path.Join(_scene.DirPath, Repo2);
        Directory.SetCurrentDirectory(repo2Path);

        using var hostRepo = Lg2Repository.New(".");
        using var hostConfig = hostRepo.GetConfig();

        var repo0SiteName = TautSiteConfig.FindSiteNameForRemote(hostConfig, Repo0);
        var repo1SiteName = TautSiteConfig.FindSiteNameForRemote(hostConfig, Repo1);

        ProgramCommandLine progCli = new(_host);

        string[] targetOpt = ["--target", Repo0];
        string[] cliArgs = ["site", .. targetOpt, "remove"];
        var parseResult = progCli.Parse(cliArgs);

        var exitCode = parseResult.Invoke(_invCfg);
        Assert.NotEqual(0, exitCode);

        var wantedError =
            $"Taut site '{repo0SiteName}' is linked by others (e.g., '{repo1SiteName}')"
            + Environment.NewLine;
        var actualError = _invCfg.Error.ToString();

        Assert.Equal(wantedError, actualError);
    }

    [Fact]
    public void RemoveRepo1ThatLinksToRepo0()
    {
        _scene.SetupRepo0(_host);
        _scene.SetupRepo1(_host);
        _scene.SetupRepo2(_host);
        _scene.ConfigRepo2AddingRepo1WithLinkToRepo0(_host);

        var repo2Path = Path.Join(_scene.DirPath, Repo2);
        Directory.SetCurrentDirectory(repo2Path);

        using var hostRepo = Lg2Repository.New(".");
        using var hostConfig = hostRepo.GetConfig();

        var repo1SiteName = TautSiteConfig.FindSiteNameForRemote(hostConfig, Repo1);

        ProgramCommandLine progCli = new(_host);

        string[] targetOpt = ["--target", Repo1];
        string[] cliArgs = ["site", .. targetOpt, "remove"];
        var parseResult = progCli.Parse(cliArgs);

        var exitCode = parseResult.Invoke(_invCfg);
        Assert.Equal(0, exitCode);

        Assert.False(TautSiteConfig.TryFindSiteNameForRemote(hostConfig, Repo1, out _));

        var repo1SitePath = hostRepo.GetTautSitePath(repo1SiteName);
        Assert.False(Directory.Exists(repo1SitePath));
    }
}
