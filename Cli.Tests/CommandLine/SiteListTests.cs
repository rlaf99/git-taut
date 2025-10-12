using System.CommandLine;
using Cli.Tests.TestSupport;
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
    public void InvalidHostRepository()
    {
        Directory.SetCurrentDirectory(_scene.DirPath);

        const string dir0 = "dir0";
        Directory.CreateDirectory(dir0);
        Directory.SetCurrentDirectory(dir0);

        ProgramCommandLine progCli = new(_host);

        string[] cliArgs = ["site", "list"];
        var parseResult = progCli.Parse(cliArgs);

        var exitCode = parseResult.Invoke(_invCfg);
        Assert.NotEqual(0, exitCode);

        var errorText = "Not inside a git repository" + Environment.NewLine;
        Assert.Equal(errorText, _invCfg.Error.ToString());
    }

    [Fact]
    public void InvalidTargetOption()
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

        var errorText =
            $"The value '{invalidTarget}' specified by {ProgramCommandLine.SiteTargetOption.Name} is invalid"
            + Environment.NewLine;

        Assert.Equal(errorText, _invCfg.Error.ToString());
    }

    [Fact]
    public void TestName()
    {
        // Given

        // When

        // Then
    }
}
