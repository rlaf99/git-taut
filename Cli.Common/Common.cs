using Microsoft.Extensions.Configuration;

namespace Git.Taut;

static class ProgramInfo
{
    internal const string CommandName = "git-taut";
}

static class KnownEnvironVars
{
    internal const string GitDir = "GIT_DIR";

    internal const string GitInstallRoot = "GIT_INSTALL_ROOT";

    internal const string GitTautTrace = "GIT_TAUT_TRACE";

    internal const string GitListForPushNoFetch = "GIT_TAUT_LIST_FOR_PUSH_NO_FETCH";

    internal const string GitAlternateObjectDirectories = "GIT_ALTERNATE_OBJECT_DIRECTORIES";

    internal static bool TryGetGitDir(out string gitDir)
    {
        var value = Environment.GetEnvironmentVariable(GitDir);
        if (value is null)
        {
            gitDir = string.Empty;
            return false;
        }
        else
        {
            gitDir = value;
            return true;
        }
    }

    internal static string GetGitBinaryPath()
    {
        var installRoot = Environment.GetEnvironmentVariable(GitInstallRoot);
        if (installRoot is null)
        {
            throw new InvalidOperationException($"Invalid environment variable {GitInstallRoot}");
        }

        var binDir = Path.Join(installRoot, "bin");
        var gitBin = Path.Join(binDir, "git");

        if (OperatingSystem.IsWindows())
        {
            gitBin += ".exe";
        }

        return gitBin;
    }
}

static class AppConfigurationExtensions
{
    static bool GetBooleanValue(string? val)
    {
        if (val is null)
        {
            return false;
        }

        if (val == "0" || val.Equals("false", StringComparison.InvariantCultureIgnoreCase))
        {
            return false;
        }

        return true;
    }

    internal static bool GetGitTautTrace(this IConfiguration config)
    {
        var val = config[KnownEnvironVars.GitTautTrace];
        return GetBooleanValue(val);
    }

    internal static bool GetGitListForPushNoFetch(this IConfiguration config)
    {
        var val = config[KnownEnvironVars.GitListForPushNoFetch];
        return GetBooleanValue(val);
    }
}

static class ThrowHelper
{
    internal static void InvalidOperationIfAlreadyInitalized(bool initialized, string? name = null)
    {
        if (initialized == true)
        {
            if (name is not null)
            {
                throw new InvalidOperationException($"{name} is already initialized");
            }
            else
            {
                throw new InvalidOperationException($"Already initialized");
            }
        }
    }

    internal static void InvalidOperationIfNotInitialized(bool initialized, string? name = null)
    {
        if (initialized == false)
        {
            if (name is not null)
            {
                throw new InvalidOperationException($"{name} is not initialized");
            }
            else
            {
                throw new InvalidOperationException($"Not initialized");
            }
        }
    }
}
