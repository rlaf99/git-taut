using System.Diagnostics.CodeAnalysis;
using Lg2.Sharpy;
using Microsoft.Extensions.Logging;
using ZLogger;

namespace Git.Taut;

class TautManager(ILogger<TautManager> logger, KeyValueStore kvStore, Aes256Cbc1 cipher)
{
    const string defaultDescription = $"Created by {ProgramInfo.CommandName}";

    readonly Lg2Repository _tautRepo = new();

    readonly Lg2Repository _hostRepo = new();

    internal Lg2Repository TautRepo => _tautRepo;

    internal Lg2Repository HostRepo => _hostRepo;

    string? _tautRepoPath = null;
    internal string TautRepoPath
    {
        get
        {
            _tautRepoPath ??= _tautRepo.GetPath();
            return _tautRepoPath;
        }
    }

    string? _hostRepoPath = null;
    internal string HostRepoPath
    {
        get
        {
            _hostRepoPath ??= _hostRepo.GetPath();
            return _hostRepoPath;
        }
    }

    const string TautenedRefSpecText = "refs/*:refs/tautened/*";

    [AllowNull]
    Lg2RefSpec _tautnedRefSpec;

    readonly List<string> TautenedPathSpecList = ["*.tt", "*.taut"];

    [AllowNull]
    Lg2PathSpec _tautenedPathSpec;

    internal void Open(string repoPath)
    {
        _tautRepo.Open(repoPath);
        _hostRepo.Open(Path.Join(repoPath, ".."));

        _tautnedRefSpec = Lg2RefSpec.NewForPush(TautenedRefSpecText);
        _tautenedPathSpec = Lg2PathSpec.New(TautenedPathSpecList);

        var kvStoreLocation = Path.Join(TautRepoPath, GitRepoLayout.ObjectsInfoDir);
        kvStore.Init(kvStoreLocation);

        cipher.Init();

        logger.ZLogTrace($"{nameof(TautManager)}: {nameof(Open)} '{repoPath}'");
    }

    internal void TautRepoSetDefaultDescription()
    {
        var descriptionFile = Path.Join(TautRepoPath, GitRepoLayout.Description);

        File.Delete(descriptionFile);

        using (var writer = File.AppendText(descriptionFile))
        {
            writer.NewLine = "\n";
            writer.WriteLine(defaultDescription);
        }

        logger.ZLogTrace($"Write '{defaultDescription}' to '{descriptionFile}'");
    }

    internal void TautRepoSetDefaultConfig()
    {
        using var config = _tautRepo.GetConfig();
        config.SetString(GitConfig.Fetch_Prune, "true");
    }

    internal List<string> GetRefsForTaut()
    {
        List<string> result = [];
        var refList = _tautRepo.GetRefList();

        foreach (var refName in refList)
        {
            if (_tautnedRefSpec.DstMatches(refName))
            {
                continue;
            }

            result.Add(refName);
        }

        return result;
    }

    internal void TautRepoAddHostObjects()
    {
        var objectsDir = Path.Join(TautRepoPath, GitRepoLayout.ObjectsDir);

        var objectsInfoAlternatesFile = Path.Join(
            TautRepoPath,
            GitRepoLayout.ObjectsInfoAlternatesFile
        );

        var hostRepoObjectsDir = Path.Join(HostRepoPath, GitRepoLayout.ObjectsDir);

        var relPathToHostObjects = Path.GetRelativePath(objectsDir, hostRepoObjectsDir);

        using (var writer = File.AppendText(objectsInfoAlternatesFile))
        {
            writer.NewLine = "\n";
            writer.WriteLine(relPathToHostObjects);
        }

        logger.ZLogTrace($"Append '{relPathToHostObjects}' to '{objectsInfoAlternatesFile}'");
    }

    void StoreTautened(Lg2OidPlainRef source, Lg2OidPlainRef target)
    {
        kvStore.PutTautened(source, target);

        if (logger.IsEnabled(LogLevel.Trace))
        {
            var sourceOidHex8 = source.GetOidHexDigits(8);
            var targetOidText = target.GetOidHexDigits();

            logger.ZLogTrace($"Tauten {sourceOidHex8} into {targetOidText}");
        }
    }

    internal string TautenHostRef(string hostRefName)
    {
        var hostRefOid = new Lg2Oid();
        _hostRepo.GetRefOid(hostRefName, ref hostRefOid);

        var revWalk = _hostRepo.NewRevWalk();
        revWalk.Push(hostRefOid);

        for (var oid = new Lg2Oid(); revWalk.Next(ref oid); )
        {
            var commit = _tautRepo.LookupCommit(oid);

            if (logger.IsEnabled(LogLevel.Trace))
            {
                var commitOidHex8 = oid.ToHexDigits(8);
                var commitSummary = commit.GetSummary();

                logger.ZLogTrace($"Start tautening commit {commitOidHex8} {commitSummary}");
            }

            var resultCommit = TautenCommit(commit);

            if (resultCommit != commit)
            {
                StoreTautened(commit, resultCommit);
            }
        }

        var resultOid = new Lg2Oid();
        kvStore.GetTautened(hostRefOid, ref resultOid);
        var resultRefName = _tautnedRefSpec.TransformToTarget(hostRefName);
        var refLogMessage = $"Tauten host ref '{hostRefName}' to '{resultRefName}'";

        _tautRepo.SetRef(resultRefName, resultOid, refLogMessage);

        return resultRefName;
    }

    internal Lg2Commit TautenCommit(Lg2Commit commit)
    {
        var existingOid = new Lg2Oid();
        if (kvStore.TryGetTautened(commit, ref existingOid))
        {
            var resultCommit = _tautRepo.LookupCommit(existingOid);

            return resultCommit;
        }

        var tree = commit.GetTree();

        var task = TautenTreeInPostOrderAsync(tree);
        var resultTree = task.GetAwaiter().GetResult();

        if (tree != resultTree)
        {
            StoreTautened(tree, resultTree);

            var amend = commit.NewAmend();
            amend.Tree = resultTree;

            var resultOid = new Lg2Oid();
            amend.Write(ref resultOid);

            var resultCommit = _tautRepo.LookupCommit(resultOid);

            return resultCommit;
        }

        return commit;
    }

    internal async Task<Lg2Tree> TautenTreeInPostOrderAsync(Lg2Tree tree)
    {
        var existingOid = new Lg2Oid();

        if (kvStore.TryGetTautened(tree, ref existingOid))
        {
            var tautenedTree = _tautRepo.LookupTree(existingOid);

            return tautenedTree;
        }

        var treeBuilder = new Lg2TreeBuilder();
        var tautenedSet = new HashSet<nuint>();

        for (nuint i = 0; i < tree.GetEntryCount(); i++)
        {
            var entry = tree.GetEntry(i);
            var entryName = entry.GetName();
            var entryObjType = entry.GetObjectType();
            var entryFileMode = entry.GetFileModeRaw();

            if (entryObjType.IsTree())
            {
                var subTree = _tautRepo.LookupTree(entry);

                var resultSubTree = await TautenTreeInPostOrderAsync(subTree);

                if (subTree != resultSubTree)
                {
                    StoreTautened(subTree, resultSubTree);

                    treeBuilder.Insert(entryName, resultSubTree, entryFileMode);
                    tautenedSet.Add(i);
                }
            }
            else if (entryObjType.IsBlob())
            {
                if (
                    _tautenedPathSpec.MatchPath(
                        entryName,
                        Lg2PathSpecFlags.LG2_PATHSPEC_IGNORE_CASE
                    )
                )
                {
                    var blob = _hostRepo.LookupBlob(entry);

                    var oid = new Lg2Oid();
                    TautenBlob(blob, ref oid);

                    treeBuilder.Insert(entryName, oid, entryFileMode);
                    tautenedSet.Add(i);
                }
            }
            else
            {
                logger.ZLogWarning($"Ignore invalid object type {entryObjType.GetName()}");
            }
        }

        if (treeBuilder.GetEntryCount() > 0)
        {
            for (nuint i = 0; i < tree.GetEntryCount(); i++)
            {
                if (tautenedSet.Contains(i) == false)
                {
                    var entry = tree.GetEntry(i);
                    var entryObjType = entry.GetObjectType();

                    if (entryObjType.IsTree() || entryObjType.IsBlob())
                    {
                        var entryName = entry.GetName();
                        var entryFileMode = entry.GetFileModeRaw();

                        treeBuilder.Insert(entryName, entry, entryFileMode);
                    }
                }
            }

            var resultOid = new Lg2Oid();
            treeBuilder.Write(ref resultOid);

            var resultTree = _tautRepo.LookupTree(resultOid);

            return resultTree;
        }

        return tree;
    }

    void TautenBlob(Lg2Blob blob, scoped ref Lg2Oid resultOid)
    {
        if (kvStore.TryGetTautened(blob, ref resultOid))
        {
            return;
        }

        using var hostRepoOdb = _hostRepo.GetOdb();
        using var tautRepoOdb = _tautRepo.GetOdb();

        using var readStream = hostRepoOdb.OpenReadStream(blob);
        using var writeStream = tautRepoOdb.OpenWriteStream(blob);
        var isBinary = blob.IsBinary();

        cipher.Encrypt(writeStream, readStream, isBinary);

        writeStream.FinalizeWrite(ref resultOid);

        StoreTautened(blob, resultOid);
    }

    internal string MapHostRefToTautened(Lg2Reference hostRef)
    {
        var srcRefName = hostRef.GetName();
        var srcRefOid = hostRef.GetTarget();

        var mappedSrcRefName = _tautnedRefSpec.TransformToTarget(srcRefName);

        if (_tautRepo.TryLookupRef(mappedSrcRefName, out var mappedSrcRef) == false)
        {
            var logMessage = $"Create a mapped reference '{mappedSrcRefName}'";
            mappedSrcRef = _tautRepo.NewRef(mappedSrcRefName, srcRefOid, false, logMessage);

            logger.ZLogTrace($"{logMessage}");
        }
        else
        {
            var logMessage = $"Update mapped reference '{mappedSrcRefName}'";
            mappedSrcRef.SetTarget(srcRefOid, logMessage);

            logger.ZLogTrace($"{logMessage}");
        }

        return mappedSrcRefName;
    }

    internal void RegainCommit(ref Lg2Oid commitOid)
    {
        using var hostRepoOdb = _hostRepo.GetOdb();
        using var tautRepoOdb = _tautRepo.GetOdb();

        void CopyObjectToHost(ILg2ObjectInfo objInfo)
        {
            if (tautRepoOdb.TryCopyObjectToAnother(hostRepoOdb, objInfo))
            {
                var typeName = objInfo.GetObjectType().GetName();

                logger.ZLogTrace($"Write {typeName} {objInfo.GetOidHexDigits()} to host");
            }
        }

        void RegainTree(Lg2Tree rootTree)
        {
            var unprocessed = new Queue<Lg2Tree>();
            unprocessed.Enqueue(rootTree);

            while (unprocessed.Count > 0)
            {
                var tree = unprocessed.Dequeue();
                var treeOidText = tree.GetOidHexDigits();

                CopyObjectToHost(tree);

                for (nuint idx = 0; idx < tree.GetEntryCount(); idx++)
                {
                    var entry = tree.GetEntry(idx);
                    var entryName = entry.GetName();
                    var entryOidText = entry.GetOidHexDigits();

                    var objType = entry.GetObjectType();
                    if (objType.IsValid() == false)
                    {
                        logger.ZLogWarning(
                            $"Invalid object type {objType.GetName()} for tree entry '{entryName}"
                        );
                        continue;
                    }
                    if (
                        _tautenedPathSpec.MatchPath(
                            entryName,
                            Lg2PathSpecFlags.LG2_PATHSPEC_IGNORE_CASE
                        )
                    )
                    {
                        logger.ZLogDebug($"{entryName} should be tautened");
                    }

                    CopyObjectToHost(entry);

                    if (objType.IsTree())
                    {
                        var subTree = _tautRepo.LookupTree(entry);
                        unprocessed.Enqueue(subTree);
                    }
                }
            }
        }

        using var revWalk = _tautRepo.NewRevWalk();
        revWalk.Push(commitOid);

        for (var oid = new Lg2Oid(); revWalk.Next(ref oid); )
        {
            var commit = _tautRepo.LookupCommit(oid);

            var commitOidHex8 = oid.ToHexDigits(8);
            var commitSummary = commit.GetSummary();

            logger.ZLogTrace($"Start transferring commit {commitOidHex8} {commitSummary}");

            CopyObjectToHost(commit);

            var rootTree = commit.GetTree();

            RegainTree(rootTree);
        }
    }
}
