using System.CommandLine;
using Cli.Tests.TestSupport;
using Git.Taut;
using Lg2.Sharpy;
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
    public void AddRepo1()
    {
        _scene.SetupRepo0(_host);
        _scene.SetupRepo1(_host);
        _scene.SetupRepo2(_host);

        const string repo1 = "repo1";
        const string repo2 = "repo2";

        var repo2Path = Path.Join(_scene.DirPath, repo2);
        Directory.SetCurrentDirectory(repo2Path);

        ProgramCommandLine progCli = new(_host);

        string[] cliArgs = ["site", "add", repo1, Path.Join("..", repo1)];
        var parseResult = progCli.Parse(cliArgs);
        var exitCode = parseResult.Invoke(_invCfg);
        Assert.Equal(0, exitCode);

        using var hostRepo = Lg2Repository.New(".");
        using var hostConfig = hostRepo.GetConfigSnapshot();

        Assert.True(TautSiteConfig.TryLoadByRemoteName(hostConfig, repo1, out var repo1SiteConfig));
        repo1SiteConfig.ResolveRemotes(hostConfig);
        Assert.Contains(repo1, repo1SiteConfig.Remotes);
    }

    [Fact]
    public void AddRepo1_RemoteNameExited()
    {
        _scene.SetupRepo0(_host);
        _scene.SetupRepo1(_host);
        _scene.SetupRepo2(_host);

        const string repo0 = "repo0";
        const string repo1 = "repo1";
        const string repo2 = "repo2";

        var repo2Path = Path.Join(_scene.DirPath, repo2);
        Directory.SetCurrentDirectory(repo2Path);

        ProgramCommandLine progCli = new(_host);

        var remoteNameToUse = repo0;
        string[] cliArgs = ["site", "add", remoteNameToUse, Path.Join("..", repo1)];
        var parseResult = progCli.Parse(cliArgs);
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
        _scene.SetupRepo0(_host);
        _scene.SetupRepo1(_host);
        _scene.SetupRepo2(_host);

        const string repo1 = "repo1";
        const string repo2 = "repo2";

        var repo2Path = Path.Join(_scene.DirPath, repo2);
        Directory.SetCurrentDirectory(repo2Path);

        ProgramCommandLine progCli = new(_host);

        string[] cliArgs = ["site", "add", repo1, Path.Join("..", repo1), "--link-existing"];
        var parseResult = progCli.Parse(cliArgs);
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
        _scene.SetupRepo0(_host);
        _scene.SetupRepo1(_host);
        _scene.SetupRepo2(_host);

        const string repo0 = "repo0";
        const string repo1 = "repo1";
        const string repo2 = "repo2";

        var repo2Path = Path.Join(_scene.DirPath, repo2);
        Directory.SetCurrentDirectory(repo2Path);

        ProgramCommandLine progCli = new(_host);

        {
            string[] targetOpt = ["--target", repo0];
            string[] cliArgs =
            [
                "site",
                .. targetOpt,
                "add",
                repo1,
                Path.Join("..", repo1),
                "--link-existing",
            ];
            var parseResult = progCli.Parse(cliArgs);

            var exitCode = parseResult.Invoke(_invCfg);
            Assert.Equal(0, exitCode);
        }

        {
            string[] targetOpt = ["--target", repo1];
            string[] cliArgs =
            [
                "site",
                .. targetOpt,
                "add",
                repo1 + "_again",
                Path.Join("..", repo1),
                "--link-existing",
            ];
            var parseResult = progCli.Parse(cliArgs);

            var exitCode = parseResult.Invoke(_invCfg);
            Assert.Equal(1, exitCode);

            var errorPattern = $"Cannot link to a site '.*' that already links to other site";

            Assert.Matches(errorPattern, _invCfg.Error.ToString());
        }
    }

    [Fact]
    public void AddRepo1_LinkRepo0()
    {
        _scene.SetupRepo0(_host);
        _scene.SetupRepo1(_host);
        _scene.SetupRepo2(_host);

        const string repo0 = "repo0";
        const string repo1 = "repo1";
        const string repo2 = "repo2";

        var repo2Path = Path.Join(_scene.DirPath, repo2);
        Directory.SetCurrentDirectory(repo2Path);

        ProgramCommandLine progCli = new(_host);

        string[] targetOpt = ["--target", repo0];

        string[] cliArgs =
        [
            "site",
            .. targetOpt,
            "add",
            repo1,
            Path.Join("..", repo1),
            "--link-existing",
        ];

        var parseResult = progCli.Parse(cliArgs);

        var exitCode = parseResult.Invoke(_invCfg);
        Assert.Equal(0, exitCode);

        testOutput.DumpError(_invCfg);

        using var hostRepo = Lg2Repository.New(".");
        using var hostConfig = hostRepo.GetConfigSnapshot();

        var repo0SiteName = TautSiteConfig.FindSiteNameForRemote(hostConfig, repo0);
        var repo9SiteName = TautSiteConfig.FindSiteNameForRemote(hostConfig, repo1);
        var repo9SiteConfig = TautSiteConfig.LoadNew(hostConfig, repo9SiteName);

        Assert.NotNull(repo9SiteConfig.LinkTo);
        Assert.Equal(repo0SiteName, repo9SiteConfig.LinkTo.SiteName);
    }
}
