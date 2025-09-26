using Microsoft.Extensions.Configuration;

namespace Git.Taut;

static class ProgramInfo
{
    internal const string CommandName = "git-taut";
}

static class KnownEnvironVars
{
    internal const string GitDir = "GIT_DIR";

    internal const string GitTautTrace = "GIT_TAUT_TRACE";

    internal const string GitAlternateObjectDirectories = "GIT_ALTERNATE_OBJECT_DIRECTORIES";

    internal static string? GetGitDir()
    {
        var result = Environment.GetEnvironmentVariable(GitDir);

        return result;
    }
}

static class AppConfigurationExtensions
{
    internal static bool GetGitTautTrace(this IConfiguration config)
    {
        var val = config[KnownEnvironVars.GitTautTrace];
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
