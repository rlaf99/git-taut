using System.CommandLine;
using Cli.Tests.TestSupport;
using Git.Taut;
using Lg2.Sharpy;
using static Cli.Tests.TestSupport.TestScenePlanConstants;

namespace Cli.Tests.CommandLine;

public sealed class SiteRemoveTests(ITestOutputHelper testOutput) : IDisposable
{
    InvocationConfiguration _invCfg = new()
    {
        EnableDefaultExceptionHandler = false,
        Output = new StringWriter(),
        Error = new StringWriter(),
    };

    TestScenePlan _plan = new(testOutput);

    public void Dispose()
    {
        _plan.PreserveContentWhenFailed(testOutput);
        _plan.Dispose();
    }

    [Fact]
    public void TargetNotSpecified()
    {
        _plan.SetupRepo0();
        _plan.SetupRepo1();
        _plan.SetupRepo2();

        _plan.SetLaunchDirectory(_plan.Repo2Root);

        ProgramCommandLine progCli = new(_plan.Host);

        string[] cliArgs = ["site", "remove"];
        var parseResult = progCli.ParseForGitTaut(cliArgs);

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
        _plan.SetupRepo0();
        _plan.SetupRepo1();
        _plan.SetupRepo2();

        using var hostRepo = Lg2Repository.New(_plan.Repo2Root);
        using var hostConfig = hostRepo.GetConfig();

        var repo0SiteName = TautSiteConfig.FindSiteNameForRemote(hostConfig, Repo0);

        _plan.SetLaunchDirectory(_plan.Repo2Root);

        ProgramCommandLine progCli = new(_plan.Host);

        string[] targetOpt = ["--target", Repo0];
        string[] cliArgs = ["site", .. targetOpt, "remove"];
        var parseResult = progCli.ParseForGitTaut(cliArgs);

        var exitCode = parseResult.Invoke(_invCfg);
        Assert.Equal(0, exitCode);

        Assert.False(TautSiteConfig.TryFindSiteNameForRemote(hostConfig, Repo0, out _));

        var repo0SitePath = hostRepo.GetTautSitePath(repo0SiteName);
        Assert.False(Directory.Exists(repo0SitePath));
    }

    [Fact]
    public void RemoveRepo0_TargetLinkedByOther()
    {
        _plan.SetupRepo0();
        _plan.SetupRepo1();
        _plan.SetupRepo2();
        _plan.ConfigRepo2AddingRepo1WithLinkToRepo0();

        using var hostRepo = Lg2Repository.New(_plan.Repo2Root);
        using var hostConfig = hostRepo.GetConfig();

        var repo0SiteName = TautSiteConfig.FindSiteNameForRemote(hostConfig, Repo0);
        var repo1SiteName = TautSiteConfig.FindSiteNameForRemote(hostConfig, Repo1);

        _plan.SetLaunchDirectory(_plan.Repo2Root);

        ProgramCommandLine progCli = new(_plan.Host);

        string[] targetOpt = ["--target", Repo0];
        string[] cliArgs = ["site", .. targetOpt, "remove"];
        var parseResult = progCli.ParseForGitTaut(cliArgs);

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
        _plan.SetupRepo0();
        _plan.SetupRepo1();
        _plan.SetupRepo2();
        _plan.ConfigRepo2AddingRepo1WithLinkToRepo0();

        using var hostRepo = Lg2Repository.New(_plan.Repo2Root);
        using var hostConfig = hostRepo.GetConfig();

        var repo1SiteName = TautSiteConfig.FindSiteNameForRemote(hostConfig, Repo1);

        _plan.SetLaunchDirectory(_plan.Repo2Root);

        ProgramCommandLine progCli = new(_plan.Host);

        string[] targetOpt = ["--target", Repo1];
        string[] cliArgs = ["site", .. targetOpt, "remove"];
        var parseResult = progCli.ParseForGitTaut(cliArgs);

        var exitCode = parseResult.Invoke(_invCfg);
        Assert.Equal(0, exitCode);

        Assert.False(TautSiteConfig.TryFindSiteNameForRemote(hostConfig, Repo1, out _));

        var repo1SitePath = hostRepo.GetTautSitePath(repo1SiteName);
        Assert.False(Directory.Exists(repo1SitePath));
    }
}
