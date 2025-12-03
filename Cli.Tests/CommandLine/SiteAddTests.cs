using System.CommandLine;
using Cli.Tests.TestSupport;
using Git.Taut;
using Lg2.Sharpy;
using static Cli.Tests.TestSupport.TestScenePlanConstants;

namespace Cli.Tests.CommandLine;

public sealed class SiteAddTests(ITestOutputHelper testOutput) : IDisposable
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
    public void AddRepo1()
    {
        _plan.SetupRepo0();
        _plan.SetupRepo1();
        _plan.SetupRepo2();

        _plan.SetLaunchDirectory(_plan.Repo2Root);

        ProgramCommandLine progCli = new(_plan.Host);

        string[] cliArgs = ["add", Repo1, _plan.Repo1Root];
        var parseResult = progCli.ParseForGitTaut(cliArgs);
        var exitCode = parseResult.Invoke(_invCfg);
        Assert.Equal(0, exitCode);

        using var hostRepo = Lg2Repository.New(_plan.Repo2Root);
        using var hostConfig = hostRepo.GetConfigSnapshot();
        var remoteRepo1 = hostRepo.LookupRemote(Repo1);

        Assert.True(
            TautSiteConfiguration.TryLoadForRemote(hostConfig, remoteRepo1, out var repo1SiteConfig)
        );
    }

    [Fact]
    public void AddRepo1_RemoteNameExited()
    {
        _plan.SetupRepo0();
        _plan.SetupRepo1();
        _plan.SetupRepo2();

        _plan.SetLaunchDirectory(_plan.Repo2Root);

        ProgramCommandLine progCli = new(_plan.Host);

        var remoteNameToUse = Repo0;
        string[] cliArgs = ["add", remoteNameToUse, _plan.Repo1Root];
        var parseResult = progCli.ParseForGitTaut(cliArgs);
        var exitCode = parseResult.Invoke(_invCfg);
        Assert.Equal(1, exitCode);

        var wantedError =
            $"Remote '{remoteNameToUse}' already exists in the host repository"
            + Environment.NewLine;

        Assert.Equal(wantedError, _invCfg.Error.ToString());
    }

    [Fact]
    public void AddRepo1_LinkRepo0_TargetNotExists()
    {
        _plan.SetupRepo0();
        _plan.SetupRepo1();
        _plan.SetupRepo2();

        _plan.SetLaunchDirectory(_plan.Repo2Root);

        ProgramCommandLine progCli = new(_plan.Host);

        string[] cliArgs = ["add", Repo1, _plan.Repo1Root, "--link-existing"];
        var parseResult = progCli.ParseForGitTaut(cliArgs);
        var exitCode = parseResult.Invoke(_invCfg);
        Assert.Equal(1, exitCode);

        var wantedError =
            $"Option {ProgramCommandLine.SiteTargetOption.Name} is not speficied when {ProgramCommandLine.LinkExistingOption.Name} is used"
            + Environment.NewLine;
        var actualError = _invCfg.Error.ToString();

        Assert.Equal(wantedError, actualError);
    }

    [Fact]
    public void AddRepo1_LinkRepo0_TargetLinkedToOther()
    {
        _plan.SetupRepo0();
        _plan.SetupRepo1();
        _plan.SetupRepo2();

        _plan.SetLaunchDirectory(_plan.Repo2Root);

        ProgramCommandLine progCli = new(_plan.Host);

        {
            string[] targetOpt = ["--target", Repo0];
            string[] cliArgs = [.. targetOpt, "add", Repo1, _plan.Repo1Root, "--link-existing"];
            var parseResult = progCli.ParseForGitTaut(cliArgs);

            var exitCode = parseResult.Invoke(_invCfg);
            Assert.Equal(0, exitCode);
        }

        {
            string[] targetOpt = ["--target", Repo1];
            string[] cliArgs =
            [
                .. targetOpt,
                "add",
                Repo1 + "_again",
                _plan.Repo1Root,
                "--link-existing",
            ];
            var parseResult = progCli.ParseForGitTaut(cliArgs);

            var exitCode = parseResult.Invoke(_invCfg);
            Assert.Equal(1, exitCode);

            var errorPattern = $"Cannot link to a site '.*' that already links to other site";

            Assert.Matches(errorPattern, _invCfg.Error.ToString());
        }
    }

    [Fact]
    public void AddRepo1_LinkRepo0()
    {
        _plan.SetupRepo0();
        _plan.SetupRepo1();
        _plan.SetupRepo2();

        _plan.SetLaunchDirectory(_plan.Repo2Root);

        ProgramCommandLine progCli = new(_plan.Host);

        string[] targetOpt = ["--target", Repo0];

        string[] cliArgs = [.. targetOpt, "add", Repo1, _plan.Repo1Root, "--link-existing"];

        var parseResult = progCli.ParseForGitTaut(cliArgs);

        var exitCode = parseResult.Invoke(_invCfg);
        Assert.Equal(0, exitCode);

        testOutput.DumpError(_invCfg);

        using var hostRepo = Lg2Repository.New(_plan.Repo2Root);
        using var hostConfig = hostRepo.GetConfigSnapshot();

        var repo0SiteName = hostRepo.FindTautSiteNameForRemote(Repo0);
        var repo1SiteName = hostRepo.FindTautSiteNameForRemote(Repo1);

        var repo1SiteConfig = TautSiteConfiguration.LoadNew(hostConfig, repo1SiteName);

        Assert.NotNull(repo1SiteConfig.LinkTo);
        Assert.Equal(repo0SiteName, repo1SiteConfig.LinkTo.SiteName);
    }
}
