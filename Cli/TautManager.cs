using System.Diagnostics.CodeAnalysis;
using Lg2.Sharpy;
using LightningDB;
using Microsoft.Extensions.Logging;
using ZLogger;

namespace Git.Remote.Taut;

class TautManager(ILogger<TautManager> logger, Aes256Cbc1 cipher)
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

    const string KvStoreDirName = "tautened";
    const string KvStoreHost2TautFileName = "host2taut";
    const string KvStoreTaut2HostFileName = "taut2host";

    [AllowNull]
    LightningEnvironment _kvStoreEnv;

    [AllowNull]
    LightningDatabase _kvStoreHost2Taut;

    [AllowNull]
    LightningDatabase _kvStoreTaut2Host;

    internal void Open(string repoPath)
    {
        _tautRepo.Open(repoPath);
        _hostRepo.Open(Path.Join(repoPath, ".."));

        _tautnedRefSpec = Lg2RefSpec.NewForPush(TautenedRefSpecText);
        _tautenedPathSpec = Lg2PathSpec.New(TautenedPathSpecList);

        void InitKvStore()
        {
            var kvStoreDirPath = Path.Join(
                TautRepoPath,
                GitRepoLayout.ObjectsInfoDir,
                KvStoreDirName
            );
            Directory.CreateDirectory(kvStoreDirPath);

            var envConfig = new EnvironmentConfiguration() { MaxDatabases = 2 };
            _kvStoreEnv = new LightningEnvironment(kvStoreDirPath, envConfig);
            _kvStoreEnv.Open();

            var config = new DatabaseConfiguration { Flags = DatabaseOpenFlags.Create };

            using (var txn = _kvStoreEnv.BeginTransaction())
            {
                _kvStoreHost2Taut = txn.OpenDatabase(KvStoreHost2TautFileName, config);
                _kvStoreTaut2Host = txn.OpenDatabase(KvStoreTaut2HostFileName, config);
                txn.Commit();
            }
        }

        InitKvStore();

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

    internal string TautenHostRef(string hostRefName)
    {
        var revWalk = _hostRepo.NewRevWalk();
        revWalk.PushRef(hostRefName);

        using var txn = _kvStoreEnv.BeginTransaction();

        Lg2Oid oid = new();
        while (revWalk.Next(ref oid))
        {
            var oidBytes = oid.GetBytes();

            if (txn.ContainsKey(_kvStoreHost2Taut, oidBytes))
            {
                continue;
            }

            var commit = _tautRepo.LookupCommit(ref oid);

            var commitOidStr8 = oid.ToString(8);
            var commitSummary = commit.GetSummary();

            logger.ZLogTrace($"Start tautening commit {commitOidStr8} {commitSummary}");

            var rootTree = commit.GetTree();
            TautenTree(rootTree);
        }

        txn.Commit();

        throw new NotImplementedException();
    }

    internal void TautenTree(Lg2Tree rootTree)
    {
        Queue<Lg2Tree> unprocessed = new();
        unprocessed.Enqueue(rootTree);

        using var txn = _kvStoreEnv.BeginTransaction();

        while (unprocessed.Count > 0)
        {
            var tree = unprocessed.Dequeue();

            for (nuint i = 0; i < tree.GetEntryCount(); i++)
            {
                var entry = tree.GetEntry(i);
                var entryObjType = entry.GetObjectType();

                if (entryObjType.IsValid() == false)
                {
                    logger.ZLogWarning($"Ignore invalid object type {entryObjType.GetName()}");

                    continue;
                }

                var oidRef = entry.GetOidPlainRef();
                var oidBytes = oidRef.GetBytes();

                if (txn.ContainsKey(_kvStoreHost2Taut, oidBytes))
                {
                    continue;
                }

                if (entryObjType.IsBlob())
                {
                    var fileName = entry.GetName();

                    if (
                        _tautenedPathSpec.MatchPath(
                            fileName,
                            Lg2PathSpecFlags.LG2_PATHSPEC_IGNORE_CASE
                        )
                    )
                    {
                        var blob = _hostRepo.LookupBlob(entry);

                        Lg2Oid oid = new();
                        TautenBlob(blob, ref oid);
                    }
                }

                if (entryObjType.IsTree())
                {
                    var subTree = _tautRepo.LookupTree(entry);
                    unprocessed.Enqueue(subTree);
                }
            }
        }

        txn.Commit();
    }

    void TautenBlob(Lg2Blob blob, scoped ref Lg2Oid tautenedOid)
    {
        var hostRepoOdb = _hostRepo.GetOdb();
        var tautRepoOdb = _tautRepo.GetOdb();

        var readStream = hostRepoOdb.OpenReadStream(blob);
        var isBinary = blob.IsBinary();
        var writeStream = tautRepoOdb.OpenWriteStream(blob);

        cipher.Encrypt(writeStream, readStream, isBinary);

        writeStream.FinalizeWrite(ref tautenedOid);
    }

    internal string MapHostRefToTautened(Lg2Reference hostRef)
    {
        var srcRefName = hostRef.GetName();
        var srcRefOid = hostRef.GetTarget();

        var mappedSrcRefName = _tautnedRefSpec.TransformToTarget(srcRefName);

        if (_tautRepo.TryLookupRef(mappedSrcRefName, out var mappedSrcRef) == false)
        {
            var logMessage = $"Create a mapped reference '{mappedSrcRefName}'";
            mappedSrcRef = _tautRepo.NewReference(mappedSrcRefName, srcRefOid, false, logMessage);

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

    internal void TransferCommitToHost(ref Lg2Oid commitOid)
    {
        using var hostRepoOdb = _hostRepo.GetOdb();
        using var tautRepoOdb = _tautRepo.GetOdb();

        void CopyObjectToHost(ILg2ObjectInfo objInfo)
        {
            if (tautRepoOdb.TryCopyObjectToAnother(hostRepoOdb, objInfo))
            {
                var typeName = objInfo.GetObjectType().GetName();

                logger.ZLogTrace($"Write {typeName} {objInfo.GetOidString()} to host");
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
                    var entry = tree.GetEntry(idx);
                    var entryFileName = entry.GetName();
                    var entryOidText = entry.GetOidString();

                    var objType = entry.GetObjectType();
                    if (objType.IsValid() == false)
                    {
                        logger.ZLogWarning(
                            $"Invalid object type {objType.GetName()} for tree entry '{entryFileName}"
                        );
                        continue;
                    }
                    if (
                        _tautenedPathSpec.MatchPath(
                            entryFileName,
                            Lg2PathSpecFlags.LG2_PATHSPEC_IGNORE_CASE
                        )
                    )
                    {
                        logger.ZLogDebug($"{entryFileName} should be tautened");
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
        revWalk.Push(ref commitOid);

        Lg2Oid oid = new();

        while (revWalk.Next(ref oid))
        {
            var commit = _tautRepo.LookupCommit(ref oid);

            var commitOidStr8 = oid.ToString(8);
            var commitSummary = commit.GetSummary();

            logger.ZLogTrace($"Start transferring commit {commitOidStr8} {commitSummary}");

            CopyObjectToHost(commit);

            var rootTree = commit.GetTree();

            TransferTree(rootTree);
        }
    }
}
