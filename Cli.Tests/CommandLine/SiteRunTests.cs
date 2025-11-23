using System.CommandLine;
using Cli.Tests.TestSupport;
using Git.Taut;
using Microsoft.Extensions.Hosting;
using static Cli.Tests.TestSupport.TestScenePlannerConstants;

namespace Cli.Tests.CommandLine;

[Collection("SetCurrentDirectory")]
public sealed class SiteRunTests(ITestOutputHelper testOutput) : IDisposable
{
    IHost _host = TestHostBuilder.BuildHost(testOutput);

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
    public void RunBranch()
    {
        _scene.SetupRepo0(_host);
        _scene.SetupRepo1(_host);
        _scene.SetupRepo2(_host);

        var repo2Path = Path.Join(_scene.DirPath, Repo2);
        Directory.SetCurrentDirectory(repo2Path);

        ProgramCommandLine progCli = new(_host);

        string[] cliArgs = ["site", "run", "branch"];
        var parseResult = progCli.ParseForGitTaut(cliArgs);

        var exitCode = parseResult.Invoke(_invCfg);
        Assert.Equal(0, exitCode);
    }
}
