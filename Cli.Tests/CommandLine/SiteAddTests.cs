using System.CommandLine;
using Cli.Tests.TestSupport;
using Git.Taut;
using Lg2.Sharpy;
using static Cli.Tests.TestSupport.TestScenePlanConstants;

namespace Cli.Tests.CommandLine;

[Collection("SetCurrentDirectory")]
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

        var repo2Path = Path.Join(_plan.Location, Repo2);
        Directory.SetCurrentDirectory(repo2Path);

        ProgramCommandLine progCli = new(_plan.Host);

        string[] cliArgs = ["site", "add", Repo1, Path.Join("..", Repo1)];
        var parseResult = progCli.ParseForGitTaut(cliArgs);
        var exitCode = parseResult.Invoke(_invCfg);
        Assert.Equal(0, exitCode);

        using var hostRepo = Lg2Repository.New(".");
        using var hostConfig = hostRepo.GetConfigSnapshot();

        Assert.True(TautSiteConfig.TryLoadByRemoteName(hostConfig, Repo1, out var repo1SiteConfig));
        repo1SiteConfig.ResolveRemotes(hostConfig);
        Assert.Contains(Repo1, repo1SiteConfig.Remotes);
    }

    [Fact]
    public void AddRepo1_RemoteNameExited()
    {
        _plan.SetupRepo0();
        _plan.SetupRepo1();
        _plan.SetupRepo2();

        var repo2Path = Path.Join(_plan.Location, Repo2);
        Directory.SetCurrentDirectory(repo2Path);

        ProgramCommandLine progCli = new(_plan.Host);

        var remoteNameToUse = Repo0;
        string[] cliArgs = ["site", "add", remoteNameToUse, Path.Join("..", Repo1)];
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

        var repo2Path = Path.Join(_plan.Location, Repo2);
        Directory.SetCurrentDirectory(repo2Path);

        ProgramCommandLine progCli = new(_plan.Host);

        string[] cliArgs = ["site", "add", Repo1, Path.Join("..", Repo1), "--link-existing"];
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

        var repo2Path = Path.Join(_plan.Location, Repo2);
        Directory.SetCurrentDirectory(repo2Path);

        ProgramCommandLine progCli = new(_plan.Host);

        {
            string[] targetOpt = ["--target", Repo0];
            string[] cliArgs =
            [
                "site",
                .. targetOpt,
                "add",
                Repo1,
                Path.Join("..", Repo1),
                "--link-existing",
            ];
            var parseResult = progCli.ParseForGitTaut(cliArgs);

            var exitCode = parseResult.Invoke(_invCfg);
            Assert.Equal(0, exitCode);
        }

        {
            string[] targetOpt = ["--target", Repo1];
            string[] cliArgs =
            [
                "site",
                .. targetOpt,
                "add",
                Repo1 + "_again",
                Path.Join("..", Repo1),
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

        var repo2Path = Path.Join(_plan.Location, Repo2);
        Directory.SetCurrentDirectory(repo2Path);

        ProgramCommandLine progCli = new(_plan.Host);

        string[] targetOpt = ["--target", Repo0];

        string[] cliArgs =
        [
            "site",
            .. targetOpt,
            "add",
            Repo1,
            Path.Join("..", Repo1),
            "--link-existing",
        ];

        var parseResult = progCli.ParseForGitTaut(cliArgs);

        var exitCode = parseResult.Invoke(_invCfg);
        Assert.Equal(0, exitCode);

        testOutput.DumpError(_invCfg);

        using var hostRepo = Lg2Repository.New(".");
        using var hostConfig = hostRepo.GetConfigSnapshot();

        var repo0SiteName = TautSiteConfig.FindSiteNameForRemote(hostConfig, Repo0);
        var repo9SiteName = TautSiteConfig.FindSiteNameForRemote(hostConfig, Repo1);
        var repo9SiteConfig = TautSiteConfig.LoadNew(hostConfig, repo9SiteName);

        Assert.NotNull(repo9SiteConfig.LinkTo);
        Assert.Equal(repo0SiteName, repo9SiteConfig.LinkTo.SiteName);
    }
}
