using Lg2.Sharpy;
using LightningDB;
using Microsoft.Extensions.Configuration;

namespace Git.Taut;

static class ProgramInfo
{
    internal const string CommandName = "git-taut";
}

static class KnownEnvironVars
{
    internal const string GitDir = "GIT_DIR";

    internal const string GitRemoteTautTrace = "GIT_TAUT_TRACE";

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

static class GitConfig
{
    internal const string Fetch_Prune = "fetch.prune";
}

static class ConfigurationExtensions
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

static class LightningExtensions
{
    public static bool TryGet(
        this LightningTransaction txn,
        LightningDatabase db,
        ReadOnlySpan<byte> key,
        ref Lg2Oid oid
    )
    {
        var (rc, _, value) = txn.Get(db, key);
        if (rc == MDBResultCode.Success)
        {
            var source = value.AsSpan();
            var target = oid.GetBytes();

            if (source.Length != target.Length)
            {
                throw new InvalidDataException(
                    $"Mismatched length, '{source.Length}' != '{target.Length}'"
                );
            }

            source.CopyTo(target);

            return true;
        }

        return false;
    }
}
