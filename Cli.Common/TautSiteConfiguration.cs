using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using Lg2.Sharpy;

namespace Git.Taut;

class TautSiteConfiguration
{
    internal const string SectionName = "taut";
    internal const string RemoteUrlMatchName = "remoteurl";
    internal const string CredentialUrlMatchName = "credentialurl";
    internal const string CredentialUserNameMatchName = "credentialusername";
    internal const string CredentialKeyTraitMatchName = "credentialkeytrait";

    internal const string LinkToMatchName = "linkto";

    internal string SiteName { get; }

    internal TautSiteConfiguration? LinkTo { get; private set; }

    internal string RemoteUrl { get; set; } = string.Empty;

    internal string CredentialUrl { get; set; } = string.Empty;

    internal string? CredentialUserName { get; set; }

    internal string CredentialKeyTrait { get; set; } = string.Empty;

    internal List<string> ReverseLinks { get; private set; } = [];

    internal TautSiteConfiguration(string tautSiteName, string? tautSiteNameToLink = null)
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

        if (string.IsNullOrEmpty(RemoteUrl))
        {
            throw new InvalidOperationException($"{nameof(RemoteUrl)} is empty");
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

    internal void SaveRemoteUrl(Lg2Config config)
    {
        if (string.IsNullOrEmpty(RemoteUrl))
        {
            throw new InvalidOperationException($"{nameof(RemoteUrl)} is empty");
        }

        config.SetString(FormatItemName(nameof(RemoteUrl)), RemoteUrl);
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

    internal void Save(Lg2Config config)
    {
        EnsureValues();

        config.SetString(FormatItemName(nameof(CredentialUrl)), CredentialUrl);

        if (string.IsNullOrEmpty(CredentialUserName) == false)
        {
            config.SetString(FormatItemName(nameof(CredentialUserName)), CredentialUserName);
        }

        config.SetString(FormatItemName(nameof(CredentialKeyTrait)), CredentialKeyTrait);

        config.SetString(FormatItemName(nameof(RemoteUrl)), RemoteUrl);
    }

    internal void Load(Lg2Config config)
    {
        CredentialUrl = config.GetString(FormatItemName(nameof(CredentialUrl)));

        if (config.TryGetString(FormatItemName(nameof(CredentialUserName)), out var credUserName))
        {
            CredentialUserName = credUserName;
        }

        CredentialKeyTrait = config.GetString(FormatItemName(nameof(CredentialKeyTrait)));

        RemoteUrl = config.GetString(FormatItemName(nameof(RemoteUrl)));
    }

    internal static TautSiteConfiguration LoadNew(Lg2Config config, string siteName)
    {
        ArgumentException.ThrowIfNullOrEmpty(siteName);

        TautSiteConfiguration result = new(siteName);

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
                    case RemoteUrlMatchName:
                        result.RemoteUrl = value;
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

    internal static bool TryLoad(
        Lg2Config config,
        string siteName,
        [NotNullWhen(true)] out TautSiteConfiguration? result
    )
    {
        TautSiteConfiguration siteConfig = new(siteName);
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

    internal static bool TryLoadForRemote(
        Lg2Config config,
        Lg2Remote remote,
        [NotNullWhen(true)] out TautSiteConfiguration? result
    )
    {
        if (TryFindSiteNameForRemote(config, remote, out var tautSiteName) == false)
        {
            result = null;

            return false;
        }

        TautSiteConfiguration siteConfig = new(tautSiteName);
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
        string ExtractSecondPart(string itemName, string firstPart)
        {
            var part1 = itemName[(firstPart.Length + 1)..];
            var variableStart = part1.IndexOf('.');
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

                var siteName = ExtractSecondPart(name, SectionName);

                if (targetSiteName is null || targetSiteName == siteName)
                {
                    siteNames.Add(siteName);
                }
            }
        }

        Dictionary<string, string> urlToRemoteNames = [];

        {
            var pattern = $@"remote\.(.*)\.url";
            using var cfgIter = config.NewIterator(pattern);
            while (cfgIter.Next(out var entry))
            {
                var url = entry.GetValue();
                var name = entry.GetName();
                var remoteName = ExtractSecondPart(name, "remote");

                urlToRemoteNames.Add(url, remoteName);
            }
        }

        foreach (var siteName in siteNames)
        {
            writer.Write($"{siteName}");

            {
                var pattern = $@"{SectionName}\.{siteName}\.linkTo";
                using var cfgIter = config.NewIterator(pattern);

                while (cfgIter.Next(out var entry))
                {
                    var val = entry.GetValue();

                    writer.Write($" @{val}");
                }
            }

            {
                var pattern = $@"{SectionName}\.{siteName}\.{RemoteUrlMatchName}";
                using var cfgIter = config.NewIterator(pattern);

                while (cfgIter.Next(out var entry))
                {
                    var val = entry.GetValue();
                    if (urlToRemoteNames.ContainsKey(val))
                    {
                        writer.Write($" {urlToRemoteNames[val]}");
                    }
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

    const string SiteNameFromRemoteUrlPattern = $@"{SectionName}\.(.*)\.{RemoteUrlMatchName}";

    static Regex SiteNameFromRemoteUrlRegex = new(SiteNameFromRemoteUrlPattern);

    internal static bool TryFindSiteNameForRemoteUrl(
        Lg2Config config,
        string remoteUrl,
        [NotNullWhen(true)] out string? siteName
    )
    {
        using var cfgIter = config.NewIterator(SiteNameFromRemoteUrlPattern);

        siteName = null;
        bool found = false;

        while (cfgIter.Next(out var entry))
        {
            var value = entry.GetValue();
            if (value == remoteUrl)
            {
                var name = entry.GetName();
                siteName = SiteNameFromRemoteUrlRegex.Match(name).Groups[1].Value;

                found = true;

                break;
            }
        }

        return found;
    }

    internal static bool TryFindSiteNameForRemote(
        Lg2Config config,
        Lg2Remote remote,
        [NotNullWhen(true)] out string? siteName
    )
    {
        var remoteUrl = remote.GetUrl();

        return TryFindSiteNameForRemoteUrl(config, remoteUrl, out siteName);
    }

    internal static string FindSiteNameForRemote(Lg2Config config, Lg2Remote remote)
    {
        var remoteUrl = remote.GetUrl();

        if (TryFindSiteNameForRemoteUrl(config, remoteUrl, out var siteName))
        {
            return siteName;
        }

        throw new InvalidOperationException(
            $"Cannot find taut site name for remote '{remote.GetName()}'"
        );
    }

    internal static Lg2Repository OpenSiteForRemote(Lg2Repository repo, string remoteName)
    {
        var siteName = repo.FindTautSiteNameForRemote(remoteName);
        var sitePath = GitRepoHelpers.GetTautSitePath(repo.GetPath(), siteName);

        var result = Lg2Repository.New(sitePath);

        return result;
    }
}

static partial class Lg2RepositoryExtensions
{
    internal static string FindTautSiteNameForRemote(this Lg2Repository repo, string remoteName)
    {
        using var config = repo.GetConfigSnapshot();
        using var remote = repo.LookupRemote(remoteName);

        var result = TautSiteConfiguration.FindSiteNameForRemote(config, remote);

        return result;
    }

    internal static bool TryFindTautSiteNameForRemote(
        this Lg2Repository repo,
        string remoteName,
        [NotNullWhen(true)] out string? siteName
    )
    {
        using var config = repo.GetConfigSnapshot();
        using var remote = repo.LookupRemote(remoteName);

        return TautSiteConfiguration.TryFindSiteNameForRemote(config, remote, out siteName);
    }
}
