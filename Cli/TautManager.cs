using System.Security.Cryptography;
using Lg2.Sharpy;
using Microsoft.Extensions.Logging;
using ZLogger;

namespace Git.Remote.Taut;

class TautManager(ILogger<TautManager> logger)
{
    const string defaultDescription = $"Created by {ProgramInfo.CommandName}";

    readonly Lg2Repository _repo = new();

    internal Lg2Repository Repo => _repo;

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

    internal List<string> RefList => _repo.GetRefList();

    internal Lg2Odb RepoOdb => _repo.GetOdb();

    internal void Open(string repoPath)
    {
        _repo.Open(repoPath);
    }

    internal void SetDefaultDescription()
    {
        var descriptionFile = Path.Join(Location, GitRepoLayout.Description);

        File.Delete(descriptionFile);

        using (var writer = File.AppendText(descriptionFile))
        {
            writer.NewLine = "\n";
            writer.WriteLine(defaultDescription);
        }

        logger.ZLogTrace($"Write '{defaultDescription}' to '{descriptionFile}'");
    }

    internal void SetDefaultConfig()
    {
        using var config = _repo.GetConfig();
        config.SetString(GitConfig.Fetch_Prune, "true");
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

    internal void TransferCommitToHost(ref Lg2Oid commitOid)
    {
        var hostRepoObjectsDir = Path.Join(HostPath, GitRepoLayout.ObjectsDir);
        using var hostRepoOdb = Lg2Odb.Open(hostRepoObjectsDir);
        using var tautRepoOdb = _repo.GetOdb();

        void CopyObjectToHost(ILg2ObjectInfo objInfo)
        {
            if (tautRepoOdb.TryCopyObjectToAnother(hostRepoOdb, objInfo))
            {
                var typeName = objInfo.GetObjectType().GetName();

                logger.ZLogTrace($"Write {typeName} {objInfo.GetOidString()} to the host repo");
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
                        continue;
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
        revWalk.Push(ref commitOid);

        Lg2Oid oid = new();

        while (revWalk.Next(ref oid))
        {
            var commit = _repo.LookupCommit(ref oid);

            var commitOidStr8 = oid.ToString(8);
            var commitSummary = commit.GetSummary();

            logger.ZLogTrace($"Start transferring commit {commitOidStr8} {commitSummary}");

            CopyObjectToHost(commit);

            var rootTree = commit.GetTree();

            TransferTree(rootTree);
        }
    }
}
