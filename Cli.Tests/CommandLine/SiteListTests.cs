using System.CommandLine;
using Cli.Tests.TestSupport;
using Git.Taut;
using Lg2.Sharpy;
using Microsoft.Extensions.Hosting;
using ProgramHelpers;

namespace Cli.Tests.CommandLine;

[Collection("GitTautPaths")]
public sealed class SiteListTests(ITestOutputHelper testOutput, HostBuilderFixture hostBuilder)
    : IDisposable
{
    IHost _host = hostBuilder.BuildHost();

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

        const string repo0 = "repo0";
        const string repo1 = "repo1";
        const string repo2 = "repo2";

        var repo2Path = Path.Join(_scene.DirPath, repo2);
        Directory.SetCurrentDirectory(repo2Path);

        ProgramCommandLine progCli = new(_host);

        string[] cliArgs = ["site", "list"];
        var parseResult = progCli.Parse(cliArgs);

        var exitCode = parseResult.Invoke(_invCfg);
        Assert.Equal(0, exitCode);

        using var hostRepo = Lg2Repository.New(".");
        using var hostConfig = hostRepo.GetConfigSnapshot();

        var repo0SiteName = TautSiteConfig.FindSiteNameForRemote(hostConfig, repo0);
        var repo1SiteName = TautSiteConfig.FindSiteNameForRemote(hostConfig, repo1);

        var wantedOutput =
            $"{repo0SiteName} {repo0}"
            + Environment.NewLine
            + $"{repo1SiteName} {repo1}"
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

        const string repo0 = "repo0";
        const string repo1 = "repo1";
        const string repo2 = "repo2";

        var repo2Path = Path.Join(_scene.DirPath, repo2);
        Directory.SetCurrentDirectory(repo2Path);

        ProgramCommandLine progCli = new(_host);

        string[] targetOpt = ["--target", repo0];
        string[] cliArgs = ["site", .. targetOpt, "list"];
        var parseResult = progCli.Parse(cliArgs);

        var exitCode = parseResult.Invoke(_invCfg);
        Assert.Equal(0, exitCode);

        using var hostRepo = Lg2Repository.New(".");
        using var hostConfig = hostRepo.GetConfigSnapshot();

        var repo0SiteName = TautSiteConfig.FindSiteNameForRemote(hostConfig, repo0);
        var repo1SiteName = TautSiteConfig.FindSiteNameForRemote(hostConfig, repo1);

        var wantedOutput = $"{repo0SiteName} {repo0}" + Environment.NewLine;

        var actualOutput = _invCfg.Output.ToString();
        Assert.Equal(wantedOutput, actualOutput);
    }

    [Fact]
    public void ListOne_TargetNotExists()
    {
        _scene.SetupRepo0(_host);
        _scene.SetupRepo1(_host);
        _scene.SetupRepo2(_host);

        var repo2Path = Path.Join(_scene.DirPath, "repo2");
        Directory.SetCurrentDirectory(repo2Path);

        ProgramCommandLine progCli = new(_host);

        var invalidTarget = "invalid-target";

        string[] targetOpt = ["--target", invalidTarget];

        string[] cliArgs = ["site", .. targetOpt, "list"];
        var parseResult = progCli.Parse(cliArgs);

        var exitCode = parseResult.Invoke(_invCfg);
        Assert.NotEqual(0, exitCode);

        var wantedError =
            $"The value '{invalidTarget}' specified by {ProgramCommandLine.SiteTargetOption.Name} is invalid"
            + Environment.NewLine;

        var actualError = _invCfg.Error.ToString();
        Assert.Equal(wantedError, actualError);
    }
}
