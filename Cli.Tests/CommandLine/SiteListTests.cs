using System.CommandLine;
using Cli.Tests.TestSupport;
using Git.Taut;
using Lg2.Sharpy;
using Microsoft.Extensions.Hosting;
using static Cli.Tests.TestSupport.TestScenePlannerConstants;

namespace Cli.Tests.CommandLine;

[Collection("SetCurrentDirectory")]
public sealed class SiteListTests(ITestOutputHelper testOutput) : IDisposable
{
    IHost _host = GitTautHostBuilder.BuildHost();

    TestScene _scene = new();

    InvocationConfiguration _invCfg = new()
    {
        Output = new StringWriter(),
        Error = new StringWriter(),
    };

    public void Dispose()
    {
        _host.Dispose();
        _scene.PreserveContentWhenFailed(testOutput);
        _scene.Dispose();
    }

    [Fact]
    public void ListAll()
    {
        _scene.SetupRepo0(_host);
        _scene.SetupRepo1(_host);
        _scene.SetupRepo2(_host);
        _scene.ConfigRepo2AddingRepo1(_host);

        var repo2Path = Path.Join(_scene.DirPath, Repo2);
        Directory.SetCurrentDirectory(repo2Path);

        ProgramCommandLine progCli = new(_host);

        string[] cliArgs = ["site", "list"];
        var parseResult = progCli.ParseForGitTaut(cliArgs);

        var exitCode = parseResult.Invoke(_invCfg);
        Assert.Equal(0, exitCode);

        using var hostRepo = Lg2Repository.New(".");
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
        _scene.SetupRepo0(_host);
        _scene.SetupRepo1(_host);
        _scene.SetupRepo2(_host);
        _scene.ConfigRepo2AddingRepo1(_host);

        var repo2Path = Path.Join(_scene.DirPath, Repo2);
        Directory.SetCurrentDirectory(repo2Path);

        ProgramCommandLine progCli = new(_host);

        string[] targetOpt = ["--target", Repo0];
        string[] cliArgs = ["site", .. targetOpt, "list"];
        var parseResult = progCli.ParseForGitTaut(cliArgs);

        var exitCode = parseResult.Invoke(_invCfg);
        Assert.Equal(0, exitCode);

        using var hostRepo = Lg2Repository.New(".");
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
        _scene.SetupRepo0(_host);
        _scene.SetupRepo1(_host);
        _scene.SetupRepo2(_host);

        var repo2Path = Path.Join(_scene.DirPath, Repo2);
        Directory.SetCurrentDirectory(repo2Path);

        ProgramCommandLine progCli = new(_host);

        var invalidTarget = "invalid-target";

        string[] targetOpt = ["--target", invalidTarget];

        string[] cliArgs = ["site", .. targetOpt, "list"];
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
