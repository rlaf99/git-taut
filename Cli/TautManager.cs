using System.Diagnostics.CodeAnalysis;
using Lg2.Sharpy;
using Microsoft.Extensions.Logging;
using ZLogger;

namespace Git.Taut;

class TautManager(ILogger<TautManager> logger, KeyValueStore kvStore, Aes256Cbc1 cipher)
{
    const string defaultDescription = $"Created by {ProgramInfo.CommandName}";

    [AllowNull]
    Lg2Repository _tautRepo;

    [AllowNull]
    Lg2Repository _hostRepo;

    internal Lg2Repository TautRepo => _tautRepo!;

    internal Lg2Repository HostRepo => _hostRepo!;

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

    const string TautenedRefGlob = "refs/tautened/*";
    const string RegainedRefGlob = "refs/regained/*";

    const string TautenedRefSpecText = "refs/*:refs/tautened/*";
    const string RegainedRefSpecText = "refs/*:refs/regained/*";
    const string RemoteRefSpecText = "refs/*:refs/remotes/*";

    [AllowNull]
    Lg2RefSpec _tautenedRefSpec;

    internal Lg2RefSpec TautenedRefSpec => _tautenedRefSpec!;

    [AllowNull]
    Lg2RefSpec _regainedRefSpec;

    internal Lg2RefSpec RegainedRefSpec => _regainedRefSpec!;

    [AllowNull]
    Lg2RefSpec _remoteRefSpec;

    internal Lg2RefSpec RemoteRefSpec => _remoteRefSpec!;

    readonly List<string> targetPathSpecList = ["*.tt", "*.taut"];

    [AllowNull]
    Lg2PathSpec _targetPathSpec;

    internal void Open(string repoPath, bool newSetup = false)
    {
        _tautRepo = Lg2Repository.New(repoPath);
        _hostRepo = Lg2Repository.New(Path.Join(repoPath, ".."));

        _tautenedRefSpec = Lg2RefSpec.NewForPush(TautenedRefSpecText);
        _regainedRefSpec = Lg2RefSpec.NewForFetch(RegainedRefSpecText);
        _remoteRefSpec = Lg2RefSpec.NewForPush(RemoteRefSpecText);

        _targetPathSpec = Lg2PathSpec.New(targetPathSpecList);

        var kvStoreLocation = Path.Join(TautRepoPath, GitRepoLayout.ObjectsInfoDir);
        kvStore.Init(kvStoreLocation);

        cipher.Init();

        if (newSetup)
        {
            SetupNewTautRepo();
        }

        logger.ZLogTrace($"{nameof(TautManager)}: {nameof(Open)} '{repoPath}'");
    }

    void SetupNewTautRepo()
    {
        void SetDefaultDescription()
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

        SetDefaultDescription();

        void SetDefaultConfig()
        {
            using var config = _tautRepo.GetConfig();
            config.SetString(GitConfig.Fetch_Prune, "true");
        }

        SetDefaultConfig();

        void AddHostObjects()
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

            logger.ZLogTrace($"Write '{relPathToHostObjects}' to '{objectsInfoAlternatesFile}'");
        }

        AddHostObjects();
    }

    internal List<string> RegainedTautRefs
    {
        get
        {
            var iter = _tautRepo.NewRefIteratorGlob(RegainedRefGlob);

            List<string> result = [];
            while (iter.NextName(out var refName))
            {
                result.Add(refName);
            }

            return result;
        }
    }

    internal List<string> TautenedTautRefs
    {
        get
        {
            var iter = _tautRepo.NewRefIteratorGlob(TautenedRefGlob);

            List<string> result = [];
            while (iter.NextName(out var refName))
            {
                result.Add(refName);
            }

            return result;
        }
    }

    internal List<string> OrdinaryTautRefs => FilterTautSpecificRefs(_tautRepo.GetRefList());

    List<string> FilterTautSpecificRefs(List<string> refList)
    {
        var result = new List<string>();

        foreach (var refName in refList)
        {
            if (_regainedRefSpec.DstMatches(refName))
            {
                continue;
            }

            if (_tautenedRefSpec.DstMatches(refName))
            {
                continue;
            }

            result.Add(refName);
        }

        return result;
    }

    List<string> FilterRemoteRefs(List<string> refList)
    {
        var result = new List<string>();

        foreach (var refName in refList)
        {
            if (_remoteRefSpec.DstMatches(refName))
            {
                continue;
            }

            result.Add(refName);
        }

        return result;
    }

    void StoreTautened(ILg2ObjectInfo objInfo, Lg2OidPlainRef target)
    {
        var source = objInfo.GetOidPlainRef();

        kvStore.PutTautened(source, target);

        if (logger.IsEnabled(LogLevel.Trace))
        {
            var sourceOidText = source.GetOidHexDigits();
            var targetOidText = target.GetOidHexDigits();
            var objTypeName = objInfo.GetObjectType().GetName();

            if (sourceOidText == targetOidText)
            {
                logger.ZLogTrace($"Tauten {targetOidText} from same {objTypeName}");
            }
            else
            {
                logger.ZLogTrace($"Tauten {targetOidText} from {objTypeName} {sourceOidText[..8]}");
            }
        }
    }

    void StoreRegained(ILg2ObjectInfo objInfo, Lg2OidPlainRef target)
    {
        var source = objInfo.GetOidPlainRef();

        kvStore.PutRegained(source, target);

        if (logger.IsEnabled(LogLevel.Trace))
        {
            var sourceOidText = source.GetOidHexDigits();
            var targetOidText = target.GetOidHexDigits();
            var objTypeName = objInfo.GetObjectType().GetName();

            if (sourceOidText == targetOidText)
            {
                logger.ZLogTrace($"Regain {targetOidText} from same {objTypeName}");
            }
            else
            {
                logger.ZLogTrace($"Regain {targetOidText} from {objTypeName} {sourceOidText[..8]}");
            }
        }
    }

    internal void RegainHostRefs()
    {
        logger.ZLogTrace($"Start regaining host refs");

        var tautRefList = FilterTautSpecificRefs(_tautRepo.GetRefList());

        var revWalk = _tautRepo.NewRevWalk();

        revWalk.ResetSorting(
            Lg2SortFlags.LG2_SORT_TOPOLOGICAL
                | Lg2SortFlags.LG2_SORT_TIME
                | Lg2SortFlags.LG2_SORT_REVERSE
        );

        revWalk.HideGlob(RegainedRefGlob);
        logger.ZLogTrace($"RevWalk.Hide {RegainedRefGlob}");

        foreach (var refName in tautRefList)
        {
            revWalk.Push(refName);
            logger.ZLogTrace($"RevWalk.Push {refName}");
        }

        for (var oid = new Lg2Oid(); revWalk.Next(ref oid); )
        {
            var tautCommit = _tautRepo.LookupCommit(oid);

            var commitOidHex8 = oid.ToHexDigits(8);
            var commitSummary = tautCommit.GetSummary();

            if (kvStore.HasRegained(tautCommit))
            {
                logger.ZLogTrace($"Ignore regained commit {commitOidHex8} '{commitSummary}'");

                continue;
            }

            logger.ZLogTrace($"Start regaining commit {commitOidHex8} '{commitSummary}'");

            RegainCommit(tautCommit);
        }

        foreach (var refName in tautRefList)
        {
            var tautOid = new Lg2Oid();
            _tautRepo.GetRefOid(refName, ref tautOid);
            var tautOidHex8 = tautOid.ToHexDigits(8);

            var hostOid = new Lg2Oid();
            kvStore.GetRegained(tautOid, ref hostOid);
            var hostOidHex8 = hostOid.ToHexDigits(8);

            var regainedRefName = _regainedRefSpec.TransformToTarget(refName);

            if (tautOid.Equals(ref hostOid))
            {
                var message = $"Regain {refName} {hostOidHex8} from same object";
                _tautRepo.SetRef(regainedRefName, hostOid, message);
                logger.ZLogTrace($"{message}");
            }
            else
            {
                var message = $"Regain {refName} {hostOidHex8} from {tautOidHex8}";
                _tautRepo.SetRef(regainedRefName, hostOid, message);
                logger.ZLogTrace($"{message}");
            }
        }
    }

    void RegainCommit(Lg2Commit tautCommit)
    {
        var tautTree = tautCommit.GetTree();
        var tautTreeOidHex8 = tautTree.GetOidHexDigits(8);
        var tautTreePath = string.Empty;

        if (kvStore.HasRegained(tautTree))
        {
            logger.ZLogTrace($"Ignore regained tree {tautTreeOidHex8} '{tautTreePath}'");
        }
        {
            logger.ZLogTrace($"Start regaining tree {tautTreeOidHex8} '{tautTreePath}'");

            var task = RegainTreeAsync(tautTree);
            task.GetAwaiter().GetResult();
        }

        var oid = new Lg2Oid();

        kvStore.GetRegained(tautTree, ref oid);

        var hostTree = _hostRepo.LookupTree(oid);

        var author = tautCommit.GetAuthor();
        var committer = tautCommit.GetCommitter();
        var message = tautCommit.GetMessage();
        var tautParents = tautCommit.GetParents();

        var hostParents = new List<Lg2Commit>();

        foreach (var parent in tautParents)
        {
            kvStore.GetRegained(parent, ref oid);

            var hostCommit = _hostRepo.LookupCommit(oid);
            hostParents.Add(hostCommit);
        }

        _hostRepo.NewCommit(author, committer, message, hostTree, hostParents, ref oid);

        StoreRegained(tautCommit, oid);
    }

    void RegainObjectByCopy(ILg2ObjectInfo objInfo)
    {
        using var tautRepoOdb = _tautRepo.GetOdb();
        using var hostRepoOdb = _hostRepo.GetOdb();

        tautRepoOdb.CopyObjectIfNotExists(hostRepoOdb, objInfo.GetOidPlainRef());

        StoreRegained(objInfo, objInfo.GetOidPlainRef());
    }

    void TautenObjectByCopy(ILg2ObjectInfo objInfo)
    {
        using var tautRepoOdb = _tautRepo.GetOdb();
        using var hostRepoOdb = _hostRepo.GetOdb();

        hostRepoOdb.CopyObjectIfNotExists(tautRepoOdb, objInfo.GetOidPlainRef());

        StoreTautened(objInfo, objInfo.GetOidPlainRef());
    }

    async Task RegainTreeAsync(Lg2Tree tautTree)
    {
        await Task.Yield(); // prevent it from running synchronously

        var treeBuilder = _tautRepo.NewTreeBuilder();
        var regainedSet = new HashSet<nuint>();

        for (nuint i = 0; i < tautTree.GetEntryCount(); i++)
        {
            var entry = tautTree.GetEntry(i);
            var entryName = entry.GetName();
            var entryFileMode = entry.GetFileModeRaw();
            var entryObjType = entry.GetObjectType();
            var entryOidHex8 = entry.GetOidHexDigits(8);

            if (entryObjType.IsTree())
            {
                logger.ZLogTrace($"Start regaining tree {entryOidHex8} '{entryName}'");

                if (kvStore.HasRegained(entry) == false)
                {
                    var tree = _tautRepo.LookupTree(entry);
                    await RegainTreeAsync(tree);
                }

                var oid = new Lg2Oid();
                kvStore.GetRegained(entry, ref oid);

                treeBuilder.Insert(entryName, oid, entryFileMode);
                regainedSet.Add(i);
            }
            else if (entryObjType.IsBlob())
            {
                logger.ZLogTrace($"Start regaining blob {entryOidHex8} '{entryName}'");

                if (_targetPathSpec.MatchPath(entryName, Lg2PathSpecFlags.LG2_PATHSPEC_IGNORE_CASE))
                {
                    var oid = new Lg2Oid();
                    if (kvStore.TryGetRegained(entry, ref oid) == false)
                    {
                        var blob = _tautRepo.LookupBlob(entry);
                        RegainBlob(blob, ref oid);
                    }

                    treeBuilder.Insert(entryName, oid, entryFileMode);
                    regainedSet.Add(i);
                }
                else
                {
                    RegainObjectByCopy(entry);
                }
            }
            else
            {
                logger.ZLogWarning($"Ignore invalid object type {entryObjType.GetName()}");
            }
        }

        if (treeBuilder.GetEntryCount() > 0)
        {
            for (nuint i = 0; i < tautTree.GetEntryCount(); i++)
            {
                if (regainedSet.Contains(i) == false)
                {
                    var entry = tautTree.GetEntry(i);
                    var entryObjType = entry.GetObjectType();

                    if (entryObjType.IsTree() || entryObjType.IsBlob())
                    {
                        var entryName = entry.GetName();
                        var entryFileMode = entry.GetFileModeRaw();

                        treeBuilder.Insert(entryName, entry, entryFileMode);
                    }
                }
            }

            var oid = new Lg2Oid();
            treeBuilder.Write(ref oid);

            StoreRegained(tautTree, oid);
        }
        else
        {
            RegainObjectByCopy(tautTree);
        }
    }

    void RegainBlob(Lg2Blob blob, ref Lg2Oid resultOid)
    {
        using var tautRepoOdb = _tautRepo.GetOdb();
        using var hostRepoOdb = _hostRepo.GetOdb();

        using var readStream = tautRepoOdb.OpenReadStream(blob);
        using var writeStream = hostRepoOdb.OpenWriteStream(blob);
        var isBinary = blob.IsBinary();

        cipher.Decrypt(writeStream, readStream, isBinary);

        writeStream.FinalizeWrite(ref resultOid);

        StoreRegained(blob, resultOid);
    }

    internal void TautenHostRefs()
    {
        logger.ZLogTrace($"Start tautening host refs");

        var hostRefList = FilterRemoteRefs(_hostRepo.GetRefList());

        var revWalk = _hostRepo.NewRevWalk();

        revWalk.ResetSorting(
            Lg2SortFlags.LG2_SORT_TOPOLOGICAL
                | Lg2SortFlags.LG2_SORT_TIME
                | Lg2SortFlags.LG2_SORT_REVERSE
        );

        revWalk.HideGlob(TautenedRefGlob);
        logger.ZLogTrace($"RevWalk.Hide {TautenedRefGlob}");

        foreach (var refName in hostRefList)
        {
            revWalk.Push(refName);
            logger.ZLogTrace($"RevWalk.Push {refName}");
        }

        for (var oid = new Lg2Oid(); revWalk.Next(ref oid); )
        {
            var hostCommit = _hostRepo.LookupCommit(oid);

            var commitOidHex8 = oid.ToHexDigits(8);
            var commitSummary = hostCommit.GetSummary();

            if (kvStore.HasTautened(hostCommit))
            {
                logger.ZLogTrace($"Ignore tautened commit {commitOidHex8} '{commitSummary}'");

                continue;
            }

            logger.ZLogTrace($"Start tautening commit {commitOidHex8} '{commitSummary}'");

            TautenCommit(hostCommit);
        }

        foreach (var refName in hostRefList)
        {
            var hostOid = new Lg2Oid();
            _hostRepo.GetRefOid(refName, ref hostOid);
            var hostOidHex8 = hostOid.ToHexDigits(8);

            var tautOid = new Lg2Oid();
            kvStore.GetTautened(hostOid, ref tautOid);
            var tautOidHex8 = tautOid.ToHexDigits(8);

            var tautenedRefName = _tautenedRefSpec.TransformToTarget(refName);

            if (hostOid.Equals(ref tautOid))
            {
                var message = $"Tauten {refName} {hostOidHex8} into same object";
                _tautRepo.SetRef(tautenedRefName, tautOid, message);
                logger.ZLogTrace($"{message}");
            }
            else
            {
                var message = $"Tauten {refName} {hostOidHex8} into {tautOidHex8}";
                _tautRepo.SetRef(tautenedRefName, tautOid, message);
                logger.ZLogTrace($"{message}");
            }
        }
    }

    void TautenCommit(Lg2Commit hostCommit)
    {
        var hostTree = hostCommit.GetTree();
        var hostTreeOidHex8 = hostTree.GetOidHexDigits(8);
        var hostTreePath = string.Empty;

        if (kvStore.HasTautened(hostTree))
        {
            logger.ZLogTrace($"Ignore tautened tree {hostTreeOidHex8} '{hostTreePath}'");
        }
        {
            logger.ZLogTrace($"Start tautening tree {hostTreeOidHex8} '{hostTreePath}'");

            var task = TautenTreeAsync(hostTree);
            task.GetAwaiter().GetResult();
        }

        var oid = new Lg2Oid();
        kvStore.GetTautened(hostTree, ref oid);

        var tautTree = _tautRepo.LookupTree(oid);

        var author = hostCommit.GetAuthor();
        var committer = hostCommit.GetCommitter();
        var message = hostCommit.GetMessage();
        var hostParents = hostCommit.GetParents();

        var tautParents = new List<Lg2Commit>();

        foreach (var parent in hostParents)
        {
            kvStore.GetTautened(parent, ref oid);

            var tautCommit = _tautRepo.LookupCommit(oid);
            tautParents.Add(tautCommit);
        }

        _tautRepo.NewCommit(author, committer, message, tautTree, tautParents, ref oid);

        StoreTautened(hostCommit, oid);
    }

    // The whole point of making this async is to avoid stack overflow when the tree is too deep.
    async Task TautenTreeAsync(Lg2Tree hostTree)
    {
        await Task.Yield(); // prevent it from running synchronously

        var treeBuilder = _tautRepo.NewTreeBuilder();
        var tautenedSet = new HashSet<nuint>();

        for (nuint i = 0; i < hostTree.GetEntryCount(); i++)
        {
            var entry = hostTree.GetEntry(i);
            var entryName = entry.GetName();
            var entryObjType = entry.GetObjectType();
            var entryFileMode = entry.GetFileModeRaw();
            var entryOidHex8 = entry.GetOidHexDigits(8);

            if (entryObjType.IsTree())
            {
                logger.ZLogTrace($"Start tautening tree {entryOidHex8} '{entryName}'");

                if (kvStore.HasTautened(entry) == false)
                {
                    var tree = _hostRepo.LookupTree(entry);
                    await TautenTreeAsync(tree);
                }

                var oid = new Lg2Oid();
                kvStore.GetTautened(entry, ref oid);

                treeBuilder.Insert(entryName, oid, entryFileMode);
                tautenedSet.Add(i);
            }
            else if (entryObjType.IsBlob())
            {
                logger.ZLogTrace($"Start tautening blob {entryOidHex8} '{entryName}'");

                if (_targetPathSpec.MatchPath(entryName, Lg2PathSpecFlags.LG2_PATHSPEC_IGNORE_CASE))
                {
                    var oid = new Lg2Oid();
                    if (kvStore.TryGetTautened(entry, ref oid) == false)
                    {
                        var blob = _hostRepo.LookupBlob(entry);
                        TautenBlob(blob, ref oid);
                    }

                    treeBuilder.Insert(entryName, oid, entryFileMode);
                    tautenedSet.Add(i);
                }
                else
                {
                    TautenObjectByCopy(entry);
                }
            }
            else
            {
                logger.ZLogWarning($"Ignore invalid object type {entryObjType.GetName()}");
            }
        }

        if (treeBuilder.GetEntryCount() > 0)
        {
            for (nuint i = 0; i < hostTree.GetEntryCount(); i++)
            {
                if (tautenedSet.Contains(i) == false)
                {
                    var entry = hostTree.GetEntry(i);
                    var entryObjType = entry.GetObjectType();

                    if (entryObjType.IsTree() || entryObjType.IsBlob())
                    {
                        var entryName = entry.GetName();
                        var entryFileMode = entry.GetFileModeRaw();

                        treeBuilder.Insert(entryName, entry, entryFileMode);
                    }
                }
            }

            var oid = new Lg2Oid();
            treeBuilder.Write(ref oid);

            StoreTautened(hostTree, oid);
        }
        else
        {
            TautenObjectByCopy(hostTree);
        }
    }

    void TautenBlob(Lg2Blob blob, scoped ref Lg2Oid resultOid)
    {
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

        var mappedSrcRefName = _tautenedRefSpec.TransformToTarget(srcRefName);

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
            if (tautRepoOdb.CopyObjectIfNotExists(hostRepoOdb, objInfo.GetOidPlainRef()))
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
                        _targetPathSpec.MatchPath(
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
