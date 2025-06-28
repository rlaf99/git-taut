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
    internal const string TautDir = "taut";
    internal const string ObjectsDir = "Objects";
    internal static readonly string ObjectsInfoDir = Path.Join(ObjectsDir, "info");
    internal static readonly string ObjectsInfoAlternatesFile = Path.Join(
        ObjectsInfoDir,
        "alternates"
    );

    internal const string DescriptionFile = "description";

    internal static string UseForwardSlash(string path)
    {
        if (Path.DirectorySeparatorChar == '\\')
        {
            return path.Replace('\\', '/');
        }

        return path;
    }

    internal static string GetDescriptionFile(this Lg2Repository repo)
    {
        repo.EnsureValid();

        var result = Path.Join(repo.GetPath(), DescriptionFile);

        result = UseForwardSlash(result);

        return result;
    }

    internal static string GetObjectDir(this Lg2Repository repo)
    {
        repo.EnsureValid();

        var result = Path.Join(repo.GetPath(), ObjectsDir);

        result = UseForwardSlash(result);

        return result;
    }

    internal static string GetObjectsInfoAlternatesFile(this Lg2Repository repo)
    {
        repo.EnsureValid();

        var result = Path.Join(repo.GetPath(), ObjectsInfoAlternatesFile);

        result = UseForwardSlash(result);

        return result;
    }

    internal static string GetObjectInfoDir(this Lg2Repository repo)
    {
        repo.EnsureValid();

        var result = Path.Join(repo.GetPath(), ObjectsInfoDir);

        result = UseForwardSlash(result);

        return result;
    }
}

static class GitConfigHelper
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
