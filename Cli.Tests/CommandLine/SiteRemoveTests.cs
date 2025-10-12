using Cli.Tests.TestSupport;
using Microsoft.Extensions.Hosting;
using ProgramHelpers;

namespace Cli.Tests.CommandLine;

[Collection("GitTautPaths")]
public sealed class SiteRemoveTests(ITestOutputHelper testOutput, HostBuilderFixture hostBuilder)
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
    public void InvalidHostRepo()
    {
        Directory.SetCurrentDirectory(_scene.DirPath);

        const string dir0 = "dir0";
        Directory.CreateDirectory(dir0);
        Directory.SetCurrentDirectory(dir0);

        ProgramCommandLine progCli = new(_host);
        string[] cliArgs = ["site", "list"];
        var parseResult = progCli.Parse(cliArgs);
        var exitCode = parseResult.Invoke();
        Assert.NotEqual(0, exitCode);
    }
}
