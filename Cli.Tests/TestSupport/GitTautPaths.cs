namespace Cli.Tests.TestSupport;

public class WithGitTautPathsFixture : IDisposable
{
    string? _savedPath;

    public WithGitTautPathsFixture()
    {
        _savedPath = Environment.GetEnvironmentVariable("PATH");

        var updatedPath = _savedPath ?? string.Empty;

        var scriptPath = Environment.GetEnvironmentVariable("GIT_REMOTE_TAUT_EXECUTABLE_PATH");
        if (scriptPath is not null)
        {
            updatedPath = string.IsNullOrEmpty(updatedPath)
                ? scriptPath
                : scriptPath + Path.PathSeparator + updatedPath;
        }

        var execPath = Environment.GetEnvironmentVariable("GIT_TAUT_EXECUTABLE_PATH");
        if (execPath is not null)
        {
            updatedPath = string.IsNullOrEmpty(updatedPath)
                ? execPath
                : execPath + Path.PathSeparator + updatedPath;
        }

        Environment.SetEnvironmentVariable("PATH", updatedPath);
    }

    public void Dispose()
    {
        Environment.SetEnvironmentVariable("PATH", _savedPath);
    }
}

[CollectionDefinition("WithGitTautPaths")]
public class WithGitTautPathsCollection : ICollectionFixture<WithGitTautPathsFixture> { }
