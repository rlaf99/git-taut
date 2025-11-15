using Lg2.Sharpy;

namespace Git.Taut;

static class GitRefSpecs
{
    public const string RefsTautenedHeadsText = "refs/tautened/heads/*";
    public const string RefsTautenedTagsText = "refs/tautened/tags/*";
    public const string RefsTautenedText = "refs/tautened/*";

    public const string RefsRegainedHeadsText = "refs/regained/heads/*";
    public const string RefsRegainedTagsText = "refs/regained/tags/*";
    public const string RefsRegainedText = "refs/regained/*";

    public const string RefsTagsText = "refs/tags/*";

    public const string RefsToRefsTautenedText = "refs/*:refs/tautened/*";
    public const string RefsToRefsRegainedText = "refs/*:refs/regained/*";
    public const string RefsToRefsRemotesText = "refs/*:refs/remotes/*";
    public const string RefsHeadsToRefsHeadsText = "refs/heads/*:refs/heads/*";
    public const string RefsTagsToRefsTagsText = "refs/tags/*:refs/tags/*";

    public static readonly Lg2RefSpec RefsToRefsTautened = Lg2RefSpec.NewForPush(
        RefsToRefsTautenedText
    );

    public static readonly Lg2RefSpec RefsToRefsRegained = Lg2RefSpec.NewForFetch(
        RefsToRefsRegainedText
    );

    public static readonly Lg2RefSpec RefsToRefsRemote = Lg2RefSpec.NewForPush(
        RefsToRefsRemotesText
    );

    public static readonly Lg2RefSpec RefsHeadsToRefsHeads = Lg2RefSpec.NewForFetch(
        RefsHeadsToRefsHeadsText
    );

    public static readonly Lg2RefSpec RefsTagsToRefsTags = Lg2RefSpec.NewForFetch(
        RefsTagsToRefsTagsText
    );

    public static IEnumerable<string> FilterLocalRefHeads(IEnumerable<string> refList)
    {
        var result = new List<string>();

        foreach (var refName in refList)
        {
            if (RefsHeadsToRefsHeads.DstMatches(refName))
            {
                result.Add(refName);
            }
        }

        return result;
    }

    public static IEnumerable<string> FilterLocalRefTags(IEnumerable<string> refList)
    {
        var result = new List<string>();

        foreach (var refName in refList)
        {
            if (RefsTagsToRefsTags.DstMatches(refName))
            {
                result.Add(refName);
            }
        }

        return result;
    }

    internal static IEnumerable<string> FilterLocalRefHeadsAndTags(IEnumerable<string> refList)
    {
        var result = new List<string>();

        foreach (var refName in refList)
        {
            if (RefsHeadsToRefsHeads.DstMatches(refName) || RefsTagsToRefsTags.DstMatches(refName))
            {
                result.Add(refName);
            }
        }

        return result;
    }
}
