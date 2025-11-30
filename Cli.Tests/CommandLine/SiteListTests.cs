using System.CommandLine;
using Cli.Tests.TestSupport;
using Git.Taut;
using Lg2.Sharpy;
using static Cli.Tests.TestSupport.TestScenePlanConstants;

namespace Cli.Tests.CommandLine;

public sealed class SiteListTests(ITestOutputHelper testOutput) : IDisposable
{
    TestScenePlan _plan = new(testOutput);

    InvocationConfiguration _invCfg = new()
    {
        Output = new StringWriter(),
        Error = new StringWriter(),
    };

    public void Dispose()
    {
        _plan.PreserveContentWhenFailed(testOutput);
        _plan.Dispose();
    }

    [Fact]
    public void ListAll()
    {
        _plan.SetupRepo0();
        _plan.SetupRepo1();
        _plan.SetupRepo2();
        _plan.ConfigRepo2AddingRepo1();

        _plan.SetLaunchDirectory(_plan.Repo2Root);

        ProgramCommandLine progCli = new(_plan.Host);

        string[] cliArgs = ["list"];
        var parseResult = progCli.ParseForGitTaut(cliArgs);

        var exitCode = parseResult.Invoke(_invCfg);
        Assert.Equal(0, exitCode);

        using var hostRepo = Lg2Repository.New(_plan.Repo2Root);
        using var hostConfig = hostRepo.GetConfigSnapshot();

        var repo0SiteName = TautSiteConfig.FindSiteNameForRemote(hostConfig, Repo0);
        var repo1SiteName = TautSiteConfig.FindSiteNameForRemote(hostConfig, Repo1);

        var wantedOutput =
            $"{repo0SiteName} {Repo0}"
            + Environment.NewLine
            + $"{repo1SiteName} {Repo1}"
            + Environment.NewLine;

        var actualOutput = _invCfg.Output.ToString();
        Assert.Equal(wantedOutput, actualOutput);
    }

    [Fact]
    public void ListOne()
    {
        _plan.SetupRepo0();
        _plan.SetupRepo1();
        _plan.SetupRepo2();
        _plan.ConfigRepo2AddingRepo1();

        _plan.SetLaunchDirectory(_plan.Repo2Root);

        ProgramCommandLine progCli = new(_plan.Host);

        string[] targetOpt = ["--target", Repo0];
        string[] cliArgs = [.. targetOpt, "list"];
        var parseResult = progCli.ParseForGitTaut(cliArgs);

        var exitCode = parseResult.Invoke(_invCfg);
        Assert.Equal(0, exitCode);

        using var hostRepo = Lg2Repository.New(_plan.Repo2Root);
        using var hostConfig = hostRepo.GetConfigSnapshot();

        var repo0SiteName = TautSiteConfig.FindSiteNameForRemote(hostConfig, Repo0);
        var repo1SiteName = TautSiteConfig.FindSiteNameForRemote(hostConfig, Repo1);

        var wantedOutput = $"{repo0SiteName} {Repo0}" + Environment.NewLine;

        var actualOutput = _invCfg.Output.ToString();
        Assert.Equal(wantedOutput, actualOutput);
    }

    [Fact]
    public void ListOne_TargetNotExists()
    {
        _plan.SetupRepo0();
        _plan.SetupRepo1();
        _plan.SetupRepo2();

        _plan.SetLaunchDirectory(_plan.Repo2Root);

        ProgramCommandLine progCli = new(_plan.Host);

        var invalidTarget = "invalid-target";

        string[] targetOpt = ["--target", invalidTarget];

        string[] cliArgs = [.. targetOpt, "list"];
        var parseResult = progCli.ParseForGitTaut(cliArgs);

        var exitCode = parseResult.Invoke(_invCfg);
        Assert.NotEqual(0, exitCode);

        var wantedError =
            $"The value '{invalidTarget}' specified by {ProgramCommandLine.SiteTargetOption.Name} is invalid"
            + Environment.NewLine;

        var actualError = _invCfg.Error.ToString();
        Assert.Equal(wantedError, actualError);
    }
}
