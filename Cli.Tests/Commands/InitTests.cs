using Cli.Tests.Support;
using Git.Taut;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Cli.Tests.Commands;

[Collection("GitTautPaths")]
public sealed class InitTests : IDisposable
{
    ITestOutputHelper _output;

    IHost _host;

    TestScene _scene;

    public InitTests(ITestOutputHelper output, HostBuilderFixture hostBuilder)
    {
        _output = output;
        _host = hostBuilder.BuildHost(_output);
        _scene = new TestScene();
    }

    public void Dispose()
    {
        _scene.PreserveContentWhenFailed(_output);
        _scene.Dispose();
    }

    [Fact]
    public void InitRepo0AsTautForRepo3()
    {
        _scene.SetupRepo0(_host);
        _scene.SetupRepo1(_host);

        var gitCli = _host.Services.GetRequiredService<GitCli>();

        Directory.SetCurrentDirectory(_scene.DirPath);

        gitCli.Run("init", "repo3");
        gitCli.Run("--git-dir", "repo3", "taut", "--init", @".\repo0", "--remote-name", "origin");

        _scene.ShouldPreserve = true;
    }
}
