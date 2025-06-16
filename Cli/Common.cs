using Lg2.Sharpy;
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
}

static class GitRepoHelper
{
    internal const string TautRepoDir = "taut";
    internal const string ObjectsDir = "Objects";
    internal static readonly string ObjectsInfoDir = Path.Join(ObjectsDir, "info");
    internal static readonly string ObjectsInfoAlternatesFile = Path.Join(
        ObjectsInfoDir,
        "alternates"
    );

    internal const string Description = "description";

    internal static string GetObjectInfoDir(this Lg2Repository repo)
    {
        repo.EnsureValid();

        var result = Path.Join(repo.GetPath(), ObjectsInfoDir);

        return result;
    }
}

static class GitConfig
{
    internal const string Fetch_Prune = "fetch.prune";
}

static class ConfigurationExtensions
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
