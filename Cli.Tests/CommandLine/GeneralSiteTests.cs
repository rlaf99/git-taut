using System.CommandLine;
using Cli.Tests.TestSupport;
using Microsoft.Extensions.Hosting;
using ProgramHelpers;

namespace Cli.Tests.CommandLine;

[Collection("GitTautPaths")]
public sealed class GeneralSiteTests(ITestOutputHelper testOutput, HostBuilderFixture hostBuilder)
    : IDisposable
{
    IHost _host = hostBuilder.BuildHost();

    TestScene _scene = new();

    public void Dispose()
    {
        _host.Dispose();
        _scene.PreserveContentWhenFailed(testOutput);
        _scene.Dispose();
    }

    [Fact]
    public void InvalidHostRepository()
    {
        _scene.SetupRepo0(_host);

        const string repo0 = "repo0";
        const string dir0 = "dir0";

        Directory.SetCurrentDirectory(_scene.DirPath);
        Directory.CreateDirectory(dir0);
        Directory.SetCurrentDirectory(dir0);

        ProgramCommandLine progCli = new(_host);

        {
            string[] cliArgs = ["site", "add", repo0, Path.Join("..", repo0)];
            var parseResult = progCli.Parse(cliArgs);

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
            var parseResult = progCli.Parse(cliArgs);

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
            string[] cliArgs = ["site", "remove", "--target", repo0];
            var parseResult = progCli.Parse(cliArgs);

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
            var parseResult = progCli.Parse(cliArgs);

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
            var parseResult = progCli.Parse(cliArgs);

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
            var parseResult = progCli.Parse(cliArgs);

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
