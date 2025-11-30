using Git.Taut;
using Lg2.Sharpy;

// Only captures direct output, see https://github.com/xunit/xunit/issues/1730
[assembly: CaptureConsole]

[assembly: AssemblyFixture(typeof(Cli.Tests.TestSupport.Lg2InitFixture))]
[assembly: AssemblyFixture(typeof(Cli.Tests.TestSupport.EnvInitFixture))]

namespace Cli.Tests.TestSupport;

public sealed class Lg2InitFixture : IDisposable
{
    Lg2Global _lg2 = new();

    public Lg2InitFixture()
    {
        _lg2.Init();
    }

    public void Dispose()
    {
        _lg2.Dispose();
    }
}

public sealed class EnvInitFixture : IDisposable
{
    string? _savedPath;
    string? _savedCeilingDirs;

    readonly string _netShortVersion =
        $"net{Environment.Version.Major}.{Environment.Version.Minor}";

    string GetSolutionDirectory()
    {
        var baseDirectory = AppContext.BaseDirectory;

        // baseDirectory -> Debug/Release -> bin -> ProjectDir -> SolutionDir
        var result = Path.Join(baseDirectory, "..", "..", "..", "..");

        result = Path.GetFullPath(result);

        return result;
    }

    string GetProjectDirectory()
    {
        var baseDirectory = AppContext.BaseDirectory;

        // baseDirectory -> Debug/Release -> bin -> ProjectDir
        var result = Path.Join(baseDirectory, "..", "..", "..");

        result = Path.GetFullPath(result);

        return result;
    }

    string GetGitTautBinDirectory(string solutionDir)
    {
        var result = Path.Join(solutionDir, "Cli.Taut", "bin", "Debug", _netShortVersion);

        return result;
    }

    string GetGitRemoteTautBinDirectory(string solutionDir)
    {
        var result = Path.Join(solutionDir, "Cli.Remote.Taut", "bin", "Debug", _netShortVersion);

        return result;
    }

    void SetupPath()
    {
        _savedPath = Environment.GetEnvironmentVariable("PATH");

        var updatedPath = _savedPath ?? string.Empty;

        var solutionDir = GetSolutionDirectory();
        var gitTautBinDir = GetGitTautBinDirectory(solutionDir);
        var gitRemoteTautBinDir = GetGitRemoteTautBinDirectory(solutionDir);

        updatedPath = gitTautBinDir + Path.PathSeparator + updatedPath;
        updatedPath = gitRemoteTautBinDir + Path.PathSeparator + updatedPath;

        Environment.SetEnvironmentVariable("PATH", updatedPath);
    }

    string GetScriptsDirectory()
    {
        var projectDir = GetProjectDirectory();
        var result = Path.Join(projectDir, "scripts");

        return result;
    }

    void SetupGitRelated()
    {
        Environment.SetEnvironmentVariable("GIT_CONFIG_SYSTEM", "/dev/null");

        var scriptsDir = GetScriptsDirectory();
        var globalConfig = Path.Join(scriptsDir, "test.gitconfig");
        Environment.SetEnvironmentVariable("GIT_CONFIG_GLOBAL", globalConfig);

        var askpass = Path.Join(scriptsDir, "getpass.sh");
        Environment.SetEnvironmentVariable("GIT_ASKPASS", askpass);

        Environment.SetEnvironmentVariable("GIT_SSH_COMMAND", "git-taut dbg-ssh-bypass");
    }

    void SetupTestbed()
    {
        var projectDir = GetProjectDirectory();
        var testbedDir = Path.Join(projectDir, "Testbed");

        Environment.SetEnvironmentVariable("CliTests__TestbedLocation", testbedDir);

        _savedCeilingDirs = AppEnvironment.GetGitCeilingDirectories();

        var updatedCeilingDirs = _savedCeilingDirs ?? string.Empty;
        updatedCeilingDirs = testbedDir + Path.PathSeparator + updatedCeilingDirs;

        AppEnvironment.SetGitCeilingDirectories(updatedCeilingDirs);
    }

    public EnvInitFixture()
    {
        SetupPath();
        SetupTestbed();
        SetupGitRelated();
    }

    public void Dispose()
    {
        Environment.SetEnvironmentVariable("PATH", _savedPath);
    }
}
