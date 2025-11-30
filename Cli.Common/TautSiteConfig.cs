using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using Lg2.Sharpy;

namespace Git.Taut;

class TautSiteConfig
{
    internal const string SectionName = "taut";

    internal const string CredentialUrlMatchName = "credentialurl";
    internal const string CredentialUserNameMatchName = "credentialusername";
    internal const string CredentialKeyTraitMatchName = "credentialkeytrait";
    internal const string RemoteMatchName = "remote";
    internal const string LinkToMatchName = "linkto";

    internal string SiteName { get; }

    internal TautSiteConfig? LinkTo { get; private set; }

    internal string CredentialUrl { get; set; } = string.Empty;

    internal string? CredentialUserName { get; set; }

    internal string CredentialKeyTrait { get; set; } = string.Empty;

    internal List<string> Remotes { get; private set; } = [];

    internal List<string> ReverseLinks { get; private set; } = [];

    internal TautSiteConfig(string tautSiteName, string? tautSiteNameToLink = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(tautSiteName);

        SiteName = tautSiteName;

        if (tautSiteNameToLink is not null)
        {
            LinkTo = new(tautSiteNameToLink, null);
        }
    }

    string FormatItemName(string itemName) => $"{SectionName}.{SiteName}.{itemName}";

    internal void EnsureValues()
    {
        if (string.IsNullOrEmpty(SiteName))
        {
            throw new InvalidOperationException($"{nameof(SiteName)} is empty");
        }

        if (string.IsNullOrEmpty(CredentialUrl))
        {
            throw new InvalidOperationException($"{nameof(CredentialUrl)} is empty");
        }

        if (string.IsNullOrEmpty(CredentialKeyTrait))
        {
            throw new InvalidOperationException($"{nameof(CredentialKeyTrait)} is empty");
        }
    }

    internal void SaveCredentialUrl(Lg2Config config)
    {
        if (string.IsNullOrEmpty(CredentialUrl))
        {
            throw new InvalidOperationException($"{nameof(CredentialUrl)} is empty");
        }

        config.SetString(FormatItemName(nameof(CredentialUrl)), CredentialUrl);
    }

    internal void SaveLinkTo(Lg2Config config)
    {
        if (LinkTo is null)
        {
            throw new InvalidOperationException($"{nameof(LinkTo)} is null");
        }

        config.SetString(FormatItemName(nameof(LinkTo)), LinkTo.SiteName);
    }

    internal void SaveCredentialPair(Lg2Config config)
    {
        if (string.IsNullOrEmpty(CredentialKeyTrait))
        {
            throw new InvalidOperationException($"{nameof(CredentialKeyTrait)} is empty");
        }

        if (string.IsNullOrEmpty(CredentialUserName) == false)
        {
            config.SetString(FormatItemName(nameof(CredentialUserName)), CredentialUserName);
        }

        config.SetString(FormatItemName(nameof(CredentialKeyTrait)), CredentialKeyTrait);
    }

    internal void SaveRemotes(Lg2Config config)
    {
        foreach (var remoteName in Remotes)
        {
            config.SetString(FormatItemName("remote"), remoteName);
        }
    }

    internal void Save(Lg2Config config)
    {
        EnsureValues();

        config.SetString(FormatItemName(nameof(CredentialUrl)), CredentialUrl);

        if (string.IsNullOrEmpty(CredentialUserName) == false)
        {
            config.SetString(FormatItemName(nameof(CredentialUserName)), CredentialUserName);
        }

        config.SetString(FormatItemName(nameof(CredentialKeyTrait)), CredentialKeyTrait);

        foreach (var remoteName in Remotes)
        {
            config.SetString(FormatItemName("remote"), remoteName);
        }
    }

    internal void Load(Lg2Config config)
    {
        CredentialUrl = config.GetString(FormatItemName(nameof(CredentialUrl)));

        if (config.TryGetString(FormatItemName(nameof(CredentialUserName)), out var credUserName))
        {
            CredentialUserName = credUserName;
        }

        CredentialKeyTrait = config.GetString(FormatItemName(nameof(CredentialKeyTrait)));
    }

    internal static TautSiteConfig LoadNew(Lg2Config config, string siteName)
    {
        ArgumentException.ThrowIfNullOrEmpty(siteName);

        TautSiteConfig result = new(siteName);

        string? linkTo = null;

        {
            var prefix = $"{SectionName}.{siteName}.";
            var pattern = $@"{SectionName}\.{siteName}\.(.*)";
            var cfgIter = config.NewIterator(pattern);

            while (cfgIter.Next(out var entry))
            {
                var name = entry.GetName();
                var value = entry.GetValue();
                var variableName = name[prefix.Length..];

                switch (variableName)
                {
                    case CredentialUrlMatchName:
                        result.CredentialUrl = value;
                        break;
                    case CredentialKeyTraitMatchName:
                        result.CredentialKeyTrait = value;
                        break;
                    case CredentialUserNameMatchName:
                        result.CredentialUserName = value;
                        break;
                    case RemoteMatchName:
                        result.Remotes.Add(value);
                        break;
                    case LinkToMatchName:
                        linkTo = value;
                        break;
                    default:
                        // ignored
                        break;
                }
            }
        }

        if (linkTo is not null)
        {
            result.LinkTo = LoadNew(config, linkTo);
        }

        return result;
    }

    // libgit2 does not seem to support section removal
    // ref: <https://github.com/libgit2/libgit2/issues/1205>
    internal void RemoveAllFromConfig(Lg2Config config)
    {
        var pattern = $@"{SectionName}\.{SiteName}\..*";
        using var cfgIter = config.NewIterator(pattern);

        while (cfgIter.Next(out var entry))
        {
            var name = entry.GetName();

            config.DeleteEntry(name);
        }
    }

    internal void RemoveRemoteFromConfig(Lg2Config config, string remoteName)
    {
        if (Remotes.Remove(remoteName) == false)
        {
            throw new ArgumentException($"{remoteName} not found", nameof(remoteName));
        }

        var entryName = FormatItemName("remote");
        var valuePattern = $@"{remoteName}";

        config.DeleteMultiVar(entryName, valuePattern);
    }

    internal void ResolveRemotes(Lg2Config config)
    {
        {
            var pattern = $@"{SectionName}\.{SiteName}\.remote";
            using var cfgIter = config.NewIterator(pattern);

            while (cfgIter.Next(out var entry))
            {
                var val = entry.GetValue();

                Remotes.Add(val);
            }
        }
    }

    internal void ResolveReverseLinks(Lg2Config config)
    {
        string ExtractSubSection(string itemName)
        {
            var part1 = itemName[(SectionName.Length + 1)..];
            var variableStart = part1.LastIndexOf('.');
            var part2 = part1[..variableStart];

            return part2;
        }

        {
            var pattern = $@"{SectionName}\.(.*)\.linkto";
            using var cfgIter = config.NewIterator(pattern);

            while (cfgIter.Next(out var entry))
            {
                var val = entry.GetValue();
                if (val == SiteName)
                {
                    var name = entry.GetName();
                    var tautSiteName = ExtractSubSection(name);
                    ReverseLinks.Add(tautSiteName);
                }
            }
        }
    }

    internal static bool TryLoadBySiteName(
        Lg2Config config,
        string siteName,
        [NotNullWhen(true)] out TautSiteConfig? result
    )
    {
        TautSiteConfig siteConfig = new(siteName);
        try
        {
            siteConfig.Load(config);
        }
        catch (Lg2Exception)
        {
            result = null;

            return false;
        }

        result = siteConfig;

        return true;
    }

    internal static bool TryLoadByRemoteName(
        Lg2Config config,
        string remoteName,
        [NotNullWhen(true)] out TautSiteConfig? result
    )
    {
        if (TryFindSiteNameForRemote(config, remoteName, out var tautSiteName) == false)
        {
            result = null;

            return false;
        }

        TautSiteConfig siteConfig = new(tautSiteName);
        try
        {
            siteConfig.Load(config);
        }
        catch (Lg2Exception)
        {
            result = null;

            return false;
        }

        result = siteConfig;

        return true;
    }

    internal static void PrintSites(
        Lg2Config config,
        TextWriter writer,
        string? targetSiteName = null
    )
    {
        string ExtractSubSection(string itemName)
        {
            var part1 = itemName[(SectionName.Length + 1)..];
            var variableStart = part1.LastIndexOf('.');
            var part2 = part1[..variableStart];

            return part2;
        }

        HashSet<string> siteNames = [];

        {
            var pattern = $@"{SectionName}\..*";
            using var cfgIter = config.NewIterator(pattern);

            while (cfgIter.Next(out var entry))
            {
                var name = entry.GetName();

                var siteName = ExtractSubSection(name);

                if (targetSiteName is null || targetSiteName == siteName)
                {
                    siteNames.Add(siteName);
                }
            }
        }

        foreach (var tautSiteName in siteNames)
        {
            writer.Write($"{tautSiteName}");

            {
                var pattern = $@"{SectionName}\.{tautSiteName}\.linkTo";
                using var cfgIter = config.NewIterator(pattern);

                while (cfgIter.Next(out var entry))
                {
                    var val = entry.GetValue();

                    writer.Write($" @{val}");
                }
            }

            {
                var pattern = $@"{SectionName}\.{tautSiteName}\.remote";
                using var cfgIter = config.NewIterator(pattern);

                while (cfgIter.Next(out var entry))
                {
                    var val = entry.GetValue();

                    writer.Write($" {val}");
                }
            }

            writer.WriteLine();
        }
    }

    internal static bool IsExistingSite(Lg2Config config, string siteName)
    {
        var pattern = $@"{SectionName}\.{siteName}\..*";

        using (var cfgIter = config.NewIterator(pattern))
        {
            if (cfgIter.Next(out _))
            {
                return true;
            }
        }

        return false;
    }

    const string RemoteOfAnySitePattern = $@"{SectionName}\.(.*)\.remote";
    static Regex RemoteOfAnySiteRegex = new(RemoteOfAnySitePattern);

    internal static bool TryFindSiteNameForRemote(
        Lg2Config config,
        string remoteName,
        [NotNullWhen(true)] out string? siteName
    )
    {
        using var cfgIter = config.NewIterator(RemoteOfAnySitePattern);

        siteName = null;
        bool found = false;

        while (cfgIter.Next(out var entry))
        {
            var value = entry.GetValue();
            if (value == remoteName)
            {
                var name = entry.GetName();
                siteName = RemoteOfAnySiteRegex.Match(name).Groups[1].Value;

                found = true;

                break;
            }
        }

        return found;
    }

    internal static string FindSiteNameForRemote(Lg2Config config, string remoteName)
    {
        if (TryFindSiteNameForRemote(config, remoteName, out var result))
        {
            return result;
        }

        throw new InvalidOperationException(
            $"Taut site name is not found for remote '{remoteName}'"
        );
    }

    internal static Lg2Repository OpenSiteForRemote(Lg2Repository hostRepo, string remoteName)
    {
        using var hostConfig = hostRepo.GetConfigSnapshot();
        var siteName = FindSiteNameForRemote(hostConfig, remoteName);
        var sitePath = GitRepoHelpers.GetTautSitePath(hostRepo.GetPath(), siteName);

        var result = Lg2Repository.New(sitePath);

        return result;
    }
}
