using Cli.Tests.TestSupport;
using Git.Taut;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Cli.Tests.CommandLine;

[Collection("GitTautPaths")]
public sealed class CloneTests : IDisposable
{
    ITestOutputHelper _output;

    IHost _host;

    TestScene _scene;

    public CloneTests(ITestOutputHelper output, HostBuilderFixture hostBuilder)
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
    public void CloneRepo0AsTautToRepo2()
    {
        _scene.SetupRepo0(_host);
        _scene.SetupRepo1(_host);

        var gitCli = _host.Services.GetRequiredService<GitCli>();

        Directory.SetCurrentDirectory(_scene.DirPath);
        gitCli.Run("clone", "taut::repo0", "repo2");

        Directory.SetCurrentDirectory("repo2");

        string a_md = "a.md";
        string b_tt = "b.tt";

        string a_md_content = "Not encrypted";
        string b_tt_content = "Encrypted";

        File.AppendAllText(a_md, a_md_content);
        File.AppendAllText(b_tt, b_tt_content);

        gitCli.Run("add", "--all");
        gitCli.Run("commit", "-m", "init");
        gitCli.Run("push");

        Directory.SetCurrentDirectory(Path.Join("..", "repo1"));
        gitCli.Run("pull");

        var a_md_read = File.ReadAllText(a_md);

        Assert.Equal(a_md_content, a_md_read);
    }
}
