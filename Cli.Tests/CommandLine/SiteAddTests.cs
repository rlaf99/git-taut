using System.CommandLine;
using Cli.Tests.TestSupport;
using Git.Taut;
using Lg2.Sharpy;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ProgramHelpers;

namespace Cli.Tests.CommandLine;

[Collection("GitTautPaths")]
public sealed class SiteAddTests(ITestOutputHelper testOutput, HostBuilderFixture hostBuilder)
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
    public void AddRepo9()
    {
        _scene.SetupRepo0(_host);
        _scene.SetupRepo1(_host);
        _scene.SetupRepo2(_host);
        _scene.SetupRepo9(_host);

        var repo2Path = Path.Join(_scene.DirPath, "repo2");
        Directory.SetCurrentDirectory(repo2Path);

        ProgramCommandLine progCli = new(_host);

        const string repo9 = "repo9";

        string[] cliArgs = ["site", "add", repo9, Path.Join("..", repo9)];
        var parseResult = progCli.Parse(cliArgs);

        var exitCode = parseResult.Invoke(_invCfg);
        Assert.Equal(0, exitCode);

        using var hostRepo = Lg2Repository.New(".");
        using var hostConfig = hostRepo.GetConfigSnapshot();

        Assert.True(TautSiteConfig.TryLoadByRemoteName(hostConfig, repo9, out var repo9SiteConfig));
        repo9SiteConfig.ResolveRemotes(hostConfig);
        Assert.Contains(repo9, repo9SiteConfig.Remotes);
    }

    [Fact]
    public void AddRepo9LinkRepo0_InvalidTarget()
    {
        _scene.SetupRepo0(_host);
        _scene.SetupRepo1(_host);
        _scene.SetupRepo2(_host);
        _scene.SetupRepo9(_host);

        var repo2Path = Path.Join(_scene.DirPath, "repo2");
        Directory.SetCurrentDirectory(repo2Path);

        ProgramCommandLine progCli = new(_host);

        const string repo9 = "repo9";

        string[] cliArgs = ["site", "add", repo9, Path.Join("..", repo9), "--link-existing"];
        var parseResult = progCli.Parse(cliArgs);

        var exitCode = parseResult.Invoke(_invCfg);
        Assert.Equal(1, exitCode);

        var errorText =
            $"No {ProgramCommandLine.SiteTargetOption.Name} is speficied when {ProgramCommandLine.LinkExistingOption.Name} is used"
            + Environment.NewLine;

        Assert.Equal(errorText, _invCfg.Error.ToString());
    }

    [Fact]
    public void AddRepo9LinkRepo0()
    {
        _scene.SetupRepo0(_host);
        _scene.SetupRepo1(_host);
        _scene.SetupRepo2(_host);
        _scene.SetupRepo9(_host);

        var repo2Path = Path.Join(_scene.DirPath, "repo2");
        Directory.SetCurrentDirectory(repo2Path);

        ProgramCommandLine progCli = new(_host);

        const string repo0 = "repo0";
        const string repo9 = "repo9";

        string[] targetOpt = ["--target", repo0];

        string[] cliArgs =
        [
            "site",
            .. targetOpt,
            "add",
            repo9,
            Path.Join("..", repo9),
            "--link-existing",
        ];

        var parseResult = progCli.Parse(cliArgs);

        var exitCode = parseResult.Invoke(_invCfg);
        Assert.Equal(0, exitCode);

        testOutput.DumpError(_invCfg);

        using var hostRepo = Lg2Repository.New(".");
        using var hostConfig = hostRepo.GetConfigSnapshot();

        var repo0SiteName = TautSiteConfig.FindSiteName(hostConfig, repo0);
        var repo9SiteName = TautSiteConfig.FindSiteName(hostConfig, repo9);
        var repo9SiteConfig = TautSiteConfig.LoadNew(hostConfig, repo9SiteName);

        Assert.NotNull(repo9SiteConfig.LinkTo);
        Assert.Equal(repo0SiteName, repo9SiteConfig.LinkTo.SiteName);
    }
}
