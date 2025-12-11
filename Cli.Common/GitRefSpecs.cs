using Lg2.Sharpy;

namespace Git.Taut;

static class GitRefSpecs
{
    public const string RefsTautenedHeadsAll = "refs/tautened/heads/*";
    public const string RefsTautenedTagsAll = "refs/tautened/tags/*";
    public const string RefsTautenedAll = "refs/tautened/*";

    public const string RefsRegainedHeadsAll = "refs/regained/heads/*";
    public const string RefsRegainedTagsAll = "refs/regained/tags/*";
    public const string RefsRegainedAll = "refs/regained/*";

    public const string RefsHeads = "refs/heads/";
    public const string RefsHeadsAll = "refs/heads/*";
    public const string RefsHeadsMaster = "refs/heads/master";
    public const string RefsTags = "refs/tags/";
    public const string RefsTagsAll = "refs/tags/*";

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

static partial class Lg2RepositoryExtensions
{
    internal static List<Lg2Reference> GetTagRefs(this Lg2Repository repo)
    {
        repo.EnsureValid();

        List<Lg2Reference> tagRefs = [];

        using (var tagIter = repo.NewRefIteratorGlob(GitRefSpecs.RefsTagsAll))
        {
            for (Lg2Reference tagRef; tagIter.Next(out tagRef); )
            {
                tagRefs.Add(tagRef);
            }
        }

        return tagRefs;
    }

    internal static List<string> GetTagRefNames(this Lg2Repository repo)
    {
        repo.EnsureValid();

        List<string> refNames = [];

        using (var tagIter = repo.NewRefIteratorGlob(GitRefSpecs.RefsTagsAll))
        {
            for (string refName; tagIter.NextName(out refName); )
            {
                refNames.Add(refName);
            }
        }

        return refNames;
    }

    internal static List<Lg2Reference> GetHeadRefs(this Lg2Repository repo)
    {
        repo.EnsureValid();

        List<Lg2Reference> tagRefs = [];

        using (var tagIter = repo.NewRefIteratorGlob(GitRefSpecs.RefsHeadsAll))
        {
            for (Lg2Reference tagRef; tagIter.Next(out tagRef); )
            {
                tagRefs.Add(tagRef);
            }
        }

        return tagRefs;
    }

    internal static List<string> GetHeadRefNames(this Lg2Repository repo)
    {
        repo.EnsureValid();

        List<string> refNames = [];

        using (var tagIter = repo.NewRefIteratorGlob(GitRefSpecs.RefsHeadsAll))
        {
            for (string refName; tagIter.NextName(out refName); )
            {
                refNames.Add(refName);
            }
        }

        return refNames;
    }

    internal static List<string> GetRegainedRefs(this Lg2Repository repo)
    {
        repo.EnsureValid();

        List<string> refNames = [];

        using (var iter = repo.NewRefIteratorGlob(GitRefSpecs.RefsRegainedAll))
        {
            while (iter.NextName(out var refName))
            {
                refNames.Add(refName);
            }
        }

        return refNames;
    }

    internal static List<string> GetTautenedRefs(this Lg2Repository repo)
    {
        repo.EnsureValid();

        List<string> refNames = [];

        using (var iter = repo.NewRefIteratorGlob(GitRefSpecs.RefsTautenedAll))
        {
            while (iter.NextName(out var refName))
            {
                refNames.Add(refName);
            }
        }

        return refNames;
    }
}
