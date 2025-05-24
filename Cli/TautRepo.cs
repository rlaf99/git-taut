using Lg2.Sharpy;
using Microsoft.Extensions.Logging;
using ZLogger;

namespace Git.Remote.Taut;

class TautRepo(ILogger<TautRepo> logger)
{
    const string defaultDescription = "git-remote-taut";

    readonly Lg2Repository _repo = new();

    string? _location = null;
    internal string Location
    {
        get
        {
            _location ??= _repo.GetPath();
            return _location;
        }
    }

    internal string HostPath => Path.Join(Location, "..");

    internal void Open(string repoPath)
    {
        _repo.Open(repoPath);
    }

    internal void SetDescription()
    {
        var descriptionFile = Path.Join(Location, GitRepoLayout.Description);

        File.Delete(descriptionFile);

        using (var writer = File.AppendText(descriptionFile))
        {
            writer.NewLine = "\n";
            writer.WriteLine($"Created by {ProgramInfo.CommandName}");
        }

        logger.ZLogTrace($"Wrote '{defaultDescription}' to '{descriptionFile}'");
    }

    internal void SetHostRepoRefs()
    {
        using var config = _repo.GetConfig();
        config.SetString("hostRepo.refs", "dummy");
    }

    internal void AddHostObjects()
    {
        var objectsDir = Path.Join(Location, GitRepoLayout.ObjectsDir);

        var objectsInfoAlternatesFile = Path.Join(
            Location,
            GitRepoLayout.ObjectsInfoAlternatesFile
        );

        var hostRepoObjectsDir = Path.Join(HostPath, GitRepoLayout.ObjectsDir);

        var relPathToHostObjects = Path.GetRelativePath(objectsDir, hostRepoObjectsDir);

        using (var writer = File.AppendText(objectsInfoAlternatesFile))
        {
            writer.NewLine = "\n";
            writer.WriteLine(relPathToHostObjects);
        }

        logger.ZLogTrace($"Append '{relPathToHostObjects}' to '{objectsInfoAlternatesFile}'");
    }

    internal void TransferCommonObjectsToHostRepo()
    {
        var hostRepoObjectsDir = Path.Join(HostPath, GitRepoLayout.ObjectsDir);
        using var hostRepoOdb = Lg2Odb.Open(hostRepoObjectsDir);
        using var tautRepoOdb = _repo.GetOdb();

        void CopyObjectToHost(ILg2GetOidPlainRef objLike)
        {
            if (tautRepoOdb.TryCopyObjectToAnother(hostRepoOdb, objLike))
            {
                logger.ZLogTrace($"Wrote object {objLike.GetOidString()} to the host repo");
            }
        }

        void TransferTree(Lg2Tree rootTree)
        {
            Queue<Lg2Tree> unprocessed = new();
            unprocessed.Enqueue(rootTree);

            while (unprocessed.Count > 0)
            {
                var tree = unprocessed.Dequeue();
                var treeOidText = tree.GetOidString();

                CopyObjectToHost(tree);

                for (nuint idx = 0; idx < tree.GetEntryCount(); idx++)
                {
                    var entry = tree.GetEntryByIndex(idx);
                    var entryName = entry.GetName();
                    var entryOidText = entry.GetOidString();

                    var objType = entry.GetObjectType();
                    if (objType.IsValid() == false)
                    {
                        logger.ZLogWarning(
                            $"Invalid object type {objType.ToString()} for tree entry '{entryName}"
                        );
                    }

                    CopyObjectToHost(entry);

                    if (objType == Lg2ObjectType.LG2_OBJECT_TREE)
                    {
                        var subTree = _repo.LookupTree(entry);
                        unprocessed.Enqueue(subTree);
                    }
                }
            }
        }

        using var revWalk = _repo.NewRevWalk();

        var refList = _repo.GetRefList();

        foreach (var refName in refList)
        {
            revWalk.PushRef(refName);
        }

        Lg2Oid oid = new();

        while (revWalk.Next(ref oid))
        {
            var commit = _repo.LookupCommit(ref oid);

            var commitOidStr8 = oid.ToPartialString(8);
            var commitSummary = commit.GetSummary();

            logger.ZLogTrace($"Transferring commit {commitOidStr8} {commitSummary}");

            CopyObjectToHost(commit);

            var rootTree = commit.GetTree();

            TransferTree(rootTree);
        }
    }
}
