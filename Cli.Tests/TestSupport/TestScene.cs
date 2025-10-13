using Git.Taut;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Cli.Tests.TestSupport;

sealed class TestScene : IDisposable
{
    const string NameSuffix = ".S";

    readonly TempPath _path;

    public string DirPath => _path.Value;

    public TestScene()
    {
        _path = Testbed.CreateDirectory(NameSuffix);
    }

    public bool ShouldPreserve { get; private set; }

    public void PreserveContentWhenFailed(ITestOutputHelper? output = null)
    {
        var state = TestContext.Current.TestState;
        if (state?.Result == TestResult.Failed)
        {
            ShouldPreserve = true;

            if (output is not null)
            {
                var caseName = TestContext.Current.TestCase?.TestCaseDisplayName;
                output.WriteLine($"Preserve'{DirPath}' for failed case {caseName}.");
            }
        }
    }

    public void PreserveContentWhenAsked(ITestOutputHelper? output = null)
    {
        ShouldPreserve = true;

        if (output is not null)
        {
            var caseName = TestContext.Current.TestCase?.TestCaseDisplayName;
            output.WriteLine($"Preserve'{DirPath}' for case {caseName} as asked.");
        }
    }

    bool _disposed;

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }
        _disposed = true;

        if (ShouldPreserve == false)
        {
            var workingDir = Directory.GetCurrentDirectory();
            var cleanupDir = Path.GetFullPath(_path.Value);

            if (workingDir.StartsWith(cleanupDir))
            {
                Directory.SetCurrentDirectory(Testbed.GetPath());
            }

            _path.Clear();
        }
    }
}

ref struct PushDirectory : IDisposable
{
    string? _pushed;

    internal PushDirectory(string? targetDirectory = null)
    {
        _pushed = Directory.GetCurrentDirectory();

        if (targetDirectory is not null)
        {
            Directory.SetCurrentDirectory(targetDirectory);
        }
    }

    public void Dispose()
    {
        var pushed = Interlocked.Exchange(ref _pushed, null);
        if (pushed is null)
        {
            return;
        }

        Directory.SetCurrentDirectory(pushed);
    }
}

static class SceneExtensions
{
    public static void SetupRepo0(this TestScene scene, IHost host)
    {
        var gitCli = host.Services.GetRequiredService<GitCli>();

        using var pushDir = new PushDirectory(scene.DirPath);

        gitCli.Run("init", "--bare", "repo0");

        var repo0Path = Path.Join(scene.DirPath, "repo0");
    }

    public static void SetupRepo1(this TestScene scene, IHost host)
    {
        var gitCli = host.Services.GetRequiredService<GitCli>();

        using var pushDir = new PushDirectory(scene.DirPath);

        gitCli.Run("clone", "repo0", "repo1");

        var repo1Path = Path.Join(scene.DirPath, "repo1");

        Directory.SetCurrentDirectory(repo1Path);

        File.WriteAllText("README", "repo1");
        File.WriteAllText(
            ".gitattributes",
            """
            *.tt taut
            tt taut
            tt/** taut
            """
        );

        gitCli.Run("add", "--all");
        gitCli.Run("commit", "-m", "repo1");
        gitCli.Run("push");
    }

    public static void SetupRepo2(this TestScene scene, IHost host)
    {
        var gitCli = host.Services.GetRequiredService<GitCli>();

        using var pushDir = new PushDirectory(scene.DirPath);

        const string repo0 = "repo0";
        const string repo2 = "repo2";

        gitCli.Run("clone", "--origin", repo0, "taut::repo0", repo2);
    }

    public static void ConfigRepo2AddingRepo1(this TestScene scene, IHost host)
    {
        var gitCli = host.Services.GetRequiredService<GitCli>();

        using var pushDir = new PushDirectory(scene.DirPath);

        const string repo1 = "repo1";
        const string repo2 = "repo2";

        Directory.SetCurrentDirectory(repo2);

        gitCli.Run("taut", "site", "add", repo1, Path.Join("..", repo1));
    }

    public static void ConfigRepo2AddingRepo1WithLinkToRepo0(this TestScene scene, IHost host)
    {
        var gitCli = host.Services.GetRequiredService<GitCli>();

        using var pushDir = new PushDirectory(scene.DirPath);

        const string repo0 = "repo0";
        const string repo1 = "repo1";
        const string repo2 = "repo2";

        Directory.SetCurrentDirectory(repo2);

        gitCli.Run(
            "taut",
            "site",
            "--target",
            repo0,
            "add",
            repo1,
            Path.Join("..", repo1),
            "--link-existing"
        );
    }

    public static void SetupRepo9(this TestScene scene, IHost host)
    {
        var gitCli = host.Services.GetRequiredService<GitCli>();

        using var pushDir = new PushDirectory(scene.DirPath);

        const string repo0 = "repo0";
        const string repo9 = "repo9";

        gitCli.Run("clone", repo0, repo9);
    }
}
