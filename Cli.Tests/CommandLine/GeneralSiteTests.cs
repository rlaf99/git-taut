using System.CommandLine;
using Cli.Tests.TestSupport;
using Git.Taut;
using Microsoft.Extensions.Hosting;
using static Cli.Tests.TestSupport.TestScenePlanConstants;

namespace Cli.Tests.CommandLine;

[Collection("SetCurrentDirectory")]
public sealed class GeneralSiteTests(ITestOutputHelper testOutput) : IDisposable
{
    IHost _host => _plan.Host;

    TestScenePlan _plan = new(testOutput);

    public void Dispose()
    {
        _plan.PreserveContentWhenFailed(testOutput);
        _plan.Dispose();
    }

    [Fact]
    public void InvalidHostRepository()
    {
        _plan.SetupRepo0();

        const string dir0 = "dir0";

        Directory.SetCurrentDirectory(_plan.DirPath);
        Directory.CreateDirectory(dir0);
        Directory.SetCurrentDirectory(dir0);

        ProgramCommandLine progCli = new(_plan.Host);

        {
            string[] cliArgs = ["site", "add", Repo0, Path.Join("..", Repo0)];
            var parseResult = progCli.ParseForGitTaut(cliArgs);

            InvocationConfiguration invCfg = new()
            {
                Output = new StringWriter(),
                Error = new StringWriter(),
            };

            var exitCode = parseResult.Invoke(invCfg);
            Assert.NotEqual(0, exitCode);

            var wantedError = "Not inside a git repository" + Environment.NewLine;
            var actualError = invCfg.Error.ToString();
            Assert.Equal(wantedError, actualError);
        }

        {
            string[] cliArgs = ["site", "list"];
            var parseResult = progCli.ParseForGitTaut(cliArgs);

            InvocationConfiguration invCfg = new()
            {
                Output = new StringWriter(),
                Error = new StringWriter(),
            };

            var exitCode = parseResult.Invoke(invCfg);
            Assert.NotEqual(0, exitCode);

            var wantedError = "Not inside a git repository" + Environment.NewLine;
            var actualError = invCfg.Error.ToString();
            Assert.Equal(wantedError, actualError);
        }

        {
            string[] cliArgs = ["site", "remove", "--target", Repo0];
            var parseResult = progCli.ParseForGitTaut(cliArgs);

            InvocationConfiguration invCfg = new()
            {
                Output = new StringWriter(),
                Error = new StringWriter(),
            };

            var exitCode = parseResult.Invoke(invCfg);
            Assert.NotEqual(0, exitCode);

            var wantedError = "Not inside a git repository" + Environment.NewLine;
            var actualError = invCfg.Error.ToString();
            Assert.Equal(wantedError, actualError);
        }

        {
            string[] cliArgs = ["site", "run", "log"];
            var parseResult = progCli.ParseForGitTaut(cliArgs);

            InvocationConfiguration invCfg = new()
            {
                Output = new StringWriter(),
                Error = new StringWriter(),
            };

            var exitCode = parseResult.Invoke(invCfg);
            Assert.NotEqual(0, exitCode);

            var wantedError = "Not inside a git repository" + Environment.NewLine;
            var actualError = invCfg.Error.ToString();
            Assert.Equal(wantedError, actualError);
        }

        {
            string[] cliArgs = ["site", "reveal", "some-path"];
            var parseResult = progCli.ParseForGitTaut(cliArgs);

            InvocationConfiguration invCfg = new()
            {
                Output = new StringWriter(),
                Error = new StringWriter(),
            };

            var exitCode = parseResult.Invoke(invCfg);
            Assert.NotEqual(0, exitCode);

            var wantedError = "Not inside a git repository" + Environment.NewLine;
            var actualError = invCfg.Error.ToString();
            Assert.Equal(wantedError, actualError);
        }

        {
            string[] cliArgs = ["site", "rescan", "--target", "some-target"];
            var parseResult = progCli.ParseForGitTaut(cliArgs);

            InvocationConfiguration invCfg = new()
            {
                Output = new StringWriter(),
                Error = new StringWriter(),
            };

            var exitCode = parseResult.Invoke(invCfg);
            Assert.NotEqual(0, exitCode);

            var wantedError = "Not inside a git repository" + Environment.NewLine;
            var actualError = invCfg.Error.ToString();
            Assert.Equal(wantedError, actualError);
        }
    }
}
