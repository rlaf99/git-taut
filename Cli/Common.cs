using Microsoft.Extensions.Configuration;

namespace Git.Remote.Taut;

static class ProgramInfo
{
    internal const string CommandName = "git-remote-taut";
}

static class KnownEnvironVars
{
    internal const string GitDir = "GIT_DIR";

    internal const string GitRemoteTautTrace = "GIT_REMOTE_TAUT_TRACE";

    internal const string GitAlternateObjectDirectories = "GIT_ALTERNATE_OBJECT_DIRECTORIES";
}

static class GitRepoLayout
{
    internal const string ObjectsDir = "Objects";
    internal static readonly string ObjectsInfoDir = Path.Join(ObjectsDir, "info");
    internal static readonly string ObjectsInfoAlternatesFile = Path.Join(
        ObjectsInfoDir,
        "alternates"
    );

    internal const string Description = "description";
}

internal static class ConfigurationExtensions
{
    internal static bool GetGitRemoteTautTrace(this IConfiguration config)
    {
        var val = config[KnownEnvironVars.GitRemoteTautTrace];
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
