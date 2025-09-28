using Lg2.Sharpy;

namespace Git.Taut;

class TautConfig
{
    internal const string SectionName = "taut";

    internal string TautRepoName { get; set; } = string.Empty;

    internal string CredentialUrl { get; set; } = string.Empty;

    internal string? CredentialUserName { get; set; }

    internal string CredentialKeyTrait { get; set; } = string.Empty;

    internal List<string> RemoteNames { get; } = [];

    string FormatTautItemName(string itemName) => $"taut.{TautRepoName}.{itemName}";

    internal void EnsureValues()
    {
        if (string.IsNullOrEmpty(TautRepoName))
        {
            throw new InvalidOperationException($"{nameof(TautRepoName)} is empty");
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

    internal void Save(Lg2Config config)
    {
        EnsureValues();

        config.SetString(FormatTautItemName(nameof(CredentialUrl)), CredentialUrl);

        if (string.IsNullOrEmpty(CredentialUserName) == false)
        {
            config.SetString(FormatTautItemName(nameof(CredentialUserName)), CredentialUserName);
        }

        config.SetString(FormatTautItemName(nameof(CredentialKeyTrait)), CredentialKeyTrait);

        foreach (var remoteName in RemoteNames)
        {
            config.SetString(FormatTautItemName("remote"), remoteName);
        }
    }

    internal void Load(Lg2Config config)
    {
        if (string.IsNullOrEmpty(TautRepoName))
        {
            throw new InvalidOperationException($"{nameof(TautRepoName)} is empty");
        }

        CredentialUrl = config.GetString(FormatTautItemName(nameof(CredentialUrl)));

        if (
            config.TryGetString(
                FormatTautItemName(nameof(CredentialUserName)),
                out var credUserName
            )
        )
        {
            CredentialUserName = credUserName;
        }

        CredentialKeyTrait = config.GetString(FormatTautItemName(nameof(CredentialKeyTrait)));
    }
}
