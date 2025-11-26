using System.CommandLine;
using Cli.Tests.TestSupport;
using Git.Taut;
using static Cli.Tests.TestSupport.TestScenePlanConstants;

namespace Cli.Tests.CommandLine;

[Collection("SetCurrentDirectory")]
public sealed class SiteRunTests(ITestOutputHelper testOutput) : IDisposable
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
    public void RunBranch()
    {
        _plan.SetupRepo0();
        _plan.SetupRepo1();
        _plan.SetupRepo2();

        _plan.SetLaunchDirectory(_plan.Repo2Root);

        ProgramCommandLine progCli = new(_plan.Host);

        string[] cliArgs = ["site", "run", "branch"];
        var parseResult = progCli.ParseForGitTaut(cliArgs);

        var exitCode = parseResult.Invoke(_invCfg);
        Assert.Equal(0, exitCode);
    }
}
