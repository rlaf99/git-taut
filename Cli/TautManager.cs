using System.Diagnostics.CodeAnalysis;
using System.Text;
using Lg2.Sharpy;
using Microsoft.Extensions.Logging;
using ZLogger;
using ZstdSharp;

namespace Git.Taut;

class TautManager(
    ILogger<TautManager> logger,
    KeyValueStore kvStore,
    Aes256Cbc1 cipher,
    ILoggerFactory loggerFactory
)
{
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

    internal string KvStoreLocation => TautRepo.GetObjectInfoDir();

    const string TautenedRefGlob = "refs/tautened/*";
    const string RegainedRefGlob = "refs/regained/*";

    const string TautenedRefSpecText = "refs/*:refs/tautened/*";
    const string RegainedRefSpecText = "refs/*:refs/regained/*";
    const string RemoteRefSpecText = "refs/*:refs/remotes/*";

    readonly Lg2RefSpec _tautenedRefSpec = Lg2RefSpec.NewForPush(TautenedRefSpecText);

    internal Lg2RefSpec TautenedRefSpec => _tautenedRefSpec!;

    readonly Lg2RefSpec _regainedRefSpec = Lg2RefSpec.NewForFetch(RegainedRefSpecText);

    internal Lg2RefSpec RegainedRefSpec => _regainedRefSpec!;

    readonly Lg2RefSpec _remoteRefSpec = Lg2RefSpec.NewForPush(RemoteRefSpecText);

    internal Lg2RefSpec RemoteRefSpec => _remoteRefSpec!;

    readonly List<string> targetPathSpecList = ["*.tt", "*.taut"];

    [AllowNull]
    Lg2PathSpec _targetPathSpec;

    [AllowNull]
    string _remoteName;

    const int DELTA_ENCODING_MIN_BYTES = 100;
    const double DELTA_ENCODING_MAX_RATIO = 0.6;
    const double DATA_COMPRESSION_MAX_RATIO = 0.8;

    internal byte[] GetUserPasswordInBytes()
    {
        var result = Encoding.ASCII.GetBytes("Hello!");

        logger.ZLogTrace($"Invoke {nameof(GetUserPasswordInBytes)}");

        return result;
    }

    internal void Open(string repoPath, string? remoteName, bool newSetup = false)
    {
        _tautRepo = Lg2Repository.New(repoPath);
        _hostRepo = Lg2Repository.New(Path.Join(repoPath, ".."));
        _remoteName = remoteName;

        if (_hostRepo.GetOidType() != Lg2OidType.LG2_OID_SHA1)
        {
            var name = Enum.GetName(Lg2OidType.LG2_OID_SHA1);
            throw new InvalidOperationException($"Only oid type {name} is supported");
        }

        _targetPathSpec = Lg2PathSpec.New(targetPathSpecList);

        kvStore.Init(KvStoreLocation);

        cipher.Init(GetUserPasswordInBytes);

        if (newSetup)
        {
            ArgumentNullException.ThrowIfNull(remoteName);
            SetupTautAndHost(remoteName);
        }
        else
        {
            if (remoteName is not null)
            {
                CheckTautAndHost(remoteName);
            }
        }

        logger.ZLogTrace($"Open {nameof(TautManager)} with '{repoPath}'");
    }

    void SetupTautAndHost(string remoteName)
    {
        var setupHelper = new TautSetupHelper(
            loggerFactory.CreateLogger<TautSetupHelper>(),
            remoteName,
            _tautRepo,
            _hostRepo
        );
        setupHelper.SetupTautAndHost();
    }

    void CheckTautAndHost(string remoteName)
    {
        var tautRemote = _tautRepo.LookupRemote(remoteName);
        var tautRemoteUrl = tautRemote.GetUrl();
        var tautRemoteUri = new Uri(tautRemoteUrl);

        var hostRemote = _hostRepo.LookupRemote(remoteName);
        var hostRemoteUrl = hostRemote.GetUrl();
        hostRemoteUrl = GitRepoHelper.RemoveTautRemoteHelperPrefix(hostRemoteUrl);
        var hostRemoteUri = new Uri(hostRemoteUrl);

        if (hostRemoteUri.IsFile)
        {
            if (hostRemoteUri.AbsolutePath != tautRemoteUri.AbsolutePath)
            {
                throw new InvalidOperationException(
                    $"host remote '{remoteName}':'{hostRemoteUri.AbsolutePath}'"
                        + $" and taut remote '{remoteName}':'{tautRemoteUri.AbsolutePath}'"
                        + " do not have the same path"
                );
            }
        }
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
        StoreTautened(objInfo.GetOidPlainRef(), target, objInfo.GetObjectType());
    }

    void StoreTautened(Lg2OidPlainRef source, Lg2OidPlainRef target, Lg2ObjectType objType)
    {
        kvStore.PutSameTautened(source, target);

        if (logger.IsEnabled(LogLevel.Trace))
        {
            var sourceOidText = source.GetOidHexDigits();
            var targetOidText = target.GetOidHexDigits();
            var objTypeName = objType.GetName();

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

        kvStore.PutSameRegained(source, target);

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

    #region Regaining

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

        using (var regainedRefIter = _tautRepo.NewRefIteratorGlob(RegainedRefGlob))
        {
            for (string refName; regainedRefIter.NextName(out refName); )
            {
                Lg2Oid hostOid = new();
                _tautRepo.GetRefOid(refName, ref hostOid);
                Lg2Oid tautOid = new();
                kvStore.GetTautened(hostOid, ref tautOid);
                revWalk.Hide(tautOid);
                logger.ZLogTrace($"RevWalk.Hide {tautOid.ToHexDigits(8)} ({refName})");
            }
        }

        foreach (var refName in tautRefList)
        {
            revWalk.Push(refName);
            logger.ZLogTrace($"RevWalk.Push {refName}");
        }

        for (Lg2Oid oid = new(); revWalk.Next(ref oid); )
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

        Lg2Oid oid = new();
        kvStore.GetRegained(tautTree, ref oid);

        var hostTree = _hostRepo.LookupTree(oid);

        var author = tautCommit.GetAuthor();
        var committer = tautCommit.GetCommitter();
        var message = tautCommit.GetMessage();
        var tautParents = tautCommit.GetAllParents();

        List<Lg2Commit> hostParents = [];

        foreach (var parent in tautParents)
        {
            kvStore.GetRegained(parent, ref oid);

            var hostCommit = _hostRepo.LookupCommit(oid);
            hostParents.Add(hostCommit);
        }

        _hostRepo.NewCommit(author, committer, message, hostTree, hostParents, ref oid);

        StoreRegained(tautCommit, oid);
    }

    void RegainSameObject(ILg2ObjectInfo objInfo)
    {
        using var tautRepoOdb = _tautRepo.GetOdb();
        using var hostRepoOdb = _hostRepo.GetOdb();

        // XXX use hardlink to improve performance
        tautRepoOdb.CopyObjectIfNotExists(hostRepoOdb, objInfo.GetOidPlainRef());

        StoreRegained(objInfo, objInfo.GetOidPlainRef());
    }

    bool IsPossiblyTautened(string entryName)
    {
        if (entryName.Length % 32 != 0)
        {
            return false;
        }

        foreach (char ch in entryName)
        {
            if (char.IsAsciiHexDigitLower(ch) == false)
            {
                return false;
            }
        }

        return true;
    }

    async Task RegainTreeAsync(Lg2Tree tautTree)
    {
        await Task.Yield(); // prevent it from running synchronously

        var treeBuilder = _hostRepo.NewTreeBuilder();
        var regainedSet = new HashSet<nuint>();

        for (nuint i = 0, count = tautTree.GetEntryCount(); i < count; i++)
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

                if (oid.PlainRef.Equals(entry.GetOidPlainRef()) == false)
                {
                    treeBuilder.Insert(entryName, oid, entryFileMode);
                    regainedSet.Add(i);
                }
            }
            else if (entryObjType.IsBlob())
            {
                logger.ZLogTrace($"Start regaining blob {entryOidHex8} '{entryName}'");

                bool isReallyTautened = false;
                if (IsPossiblyTautened(entryName))
                {
                    try
                    {
                        var blob = _tautRepo.LookupBlob(entry);
                        string regainedFileName;
                        Lg2Oid oid = new();
                        if (kvStore.TryGetRegained(entry, ref oid) == false)
                        {
                            regainedFileName = RegainBlob(blob, ref oid, entryName);
                        }
                        else
                        {
                            regainedFileName = RegainFileName(blob, entryName);
                        }

                        if (oid.PlainRef.Equals(entry.GetOidPlainRef()) == false)
                        {
                            treeBuilder.Insert(regainedFileName, oid, entryFileMode);
                            regainedSet.Add(i);
                        }

                        isReallyTautened = true;
                    }
                    catch (InvalidTautenedDataException exn)
                    {
                        if (exn.HasTautenedBytes)
                        {
                            // it is marked as tautened, but not processed correctly,
                            // thus rethrow it
                            throw;
                        }
                    }
                }

                if (isReallyTautened == false)
                {
                    RegainSameObject(entry);
                }
            }
            else
            {
                logger.ZLogWarning($"Ignore invalid object type {entryObjType.GetName()}");
            }
        }

        if (treeBuilder.GetEntryCount() > 0)
        {
            for (nuint i = 0, count = tautTree.GetEntryCount(); i < count; i++)
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
            RegainSameObject(tautTree);
        }
    }

    string RegainFileName(Lg2Blob blob, string fileName)
    {
        using var tautRepoOdb = _tautRepo.GetOdb();
        using var hostRepoOdb = _hostRepo.GetOdb();

        using var readStream = tautRepoOdb.ReadAsStream(blob);

        var fileNameStream = new MemoryStream(Convert.FromHexString(fileName), writable: false);
        var regainedFileNameStream = new MemoryStream();

        var recryptor = cipher.CreateRecryptor(readStream);
        recryptor.Decrypt(fileNameStream, regainedFileNameStream);

        var regainedFileNameData = regainedFileNameStream
            .GetBuffer()
            .AsSpan(0, (int)regainedFileNameStream.Length);

        var regainedFileName = Encoding.UTF8.GetString(regainedFileNameData);

        logger.ZLogTrace($"Regain '{regainedFileName}' from file name '{fileName}'");

        return regainedFileName;
    }

    string RegainBlob(Lg2Blob blob, ref Lg2Oid resultOid, string fileName)
    {
        using var tautRepoOdb = _tautRepo.GetOdb();
        using var hostRepoOdb = _hostRepo.GetOdb();

        using var readStream = tautRepoOdb.ReadAsStream(blob);

        var fileNameStream = new MemoryStream(Convert.FromHexString(fileName), writable: false);
        var regainedFileNameStream = new MemoryStream();

        var decryptor = cipher.CreateDecryptor(readStream);
        var extraPayload = decryptor.GetExtraPayload();
        if (extraPayload.Length > 0)
        {
            var resultStream = new PatchRegainStream();
            decryptor.ProduceOutput(resultStream, fileNameStream, regainedFileNameStream);

            var resultData = resultStream.GetBuffer().AsSpan(0, (int)resultStream.Length);

            var diff = Lg2Diff.New(resultData);
            if (diff.GetDeltaCount() != 1)
            {
                throw new InvalidDataException("There should be one delta on the diff");
            }
            var patch = diff.NewPatch(0);

            Lg2Oid oid = new();
            oid.FromRaw(extraPayload);
            var baseOdbOject = hostRepoOdb.Read(oid);

            var resultBuf = patch.Apply(baseOdbOject.GetObjectData());
            using var writeStream = hostRepoOdb.OpenWriteStream(
                resultBuf.Length,
                blob.GetObjectType()
            );
            resultBuf.DumpTo(writeStream);

            writeStream.FinalizeWrite(ref resultOid);

            StoreRegained(blob, resultOid);
        }
        else
        {
            var outputLength = decryptor.GetOutputLength();
            using var writeStream = hostRepoOdb.OpenWriteStream(outputLength, blob.GetObjectType());
            decryptor.ProduceOutput(writeStream, fileNameStream, regainedFileNameStream);
            writeStream.FinalizeWrite(ref resultOid);

            StoreRegained(blob, resultOid);
        }

        var regainedFileNameData = regainedFileNameStream
            .GetBuffer()
            .AsSpan(0, (int)regainedFileNameStream.Length);

        var regainedFileName = Encoding.UTF8.GetString(regainedFileNameData);

        logger.ZLogTrace($"Regain '{regainedFileName}' from file name '{fileName}'");

        return regainedFileName;
    }

    #endregion Regaining

    #region Tautening

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

        using (var tautenedRefIter = _tautRepo.NewRefIteratorGlob(TautenedRefGlob))
        {
            for (string refName; tautenedRefIter.NextName(out refName); )
            {
                Lg2Oid tautOid = new();
                _tautRepo.GetRefOid(refName, ref tautOid);
                Lg2Oid hostOid = new();
                kvStore.GetRegained(tautOid, ref hostOid);
                revWalk.Hide(hostOid);
                logger.ZLogTrace($"RevWalk.Hide {hostOid.ToHexDigits(8)} ({refName})");
            }
        }

        foreach (var refName in hostRefList)
        {
            revWalk.Push(refName);
            logger.ZLogTrace($"RevWalk.Push {refName}");
        }

        for (Lg2Oid oid = new(); revWalk.Next(ref oid); )
        {
            var hostCommit = _hostRepo.LookupCommit(oid);

            var commitOidHex8 = oid.ToHexDigits(8);
            var commitSummary = hostCommit.GetSummary();

            if (kvStore.HasTautened(hostCommit))
            {
                logger.ZLogTrace($"Ignore tautened commit {commitOidHex8} '{commitSummary}'");

                continue;
            }

            Dictionary<string, string> tautenedFilePaths = [];

            if (hostCommit.GetParentCount() == 1)
            {
                logger.ZLogTrace($"Start tautening files in diff");

                TautenFilesInDiff(hostCommit, tautenedFilePaths);
            }

            logger.ZLogTrace($"Start tautening commit {commitOidHex8} '{commitSummary}'");

            TautenCommit(hostCommit, tautenedFilePaths);
        }

        foreach (var refName in hostRefList)
        {
            var hostOid = new Lg2Oid();
            _hostRepo.GetRefOid(refName, ref hostOid);
            var hostOidHex8 = hostOid.ToHexDigits(8);

            var tautenedOid = new Lg2Oid();
            kvStore.GetTautened(hostOid, ref tautenedOid);
            var tautenedOidHex8 = tautenedOid.ToHexDigits(8);

            var tautenedRefName = _tautenedRefSpec.TransformToTarget(refName);

            if (hostOid.Equals(ref tautenedOid))
            {
                var message = $"Tauten {refName} {tautenedOidHex8} from same object";
                _tautRepo.SetRef(tautenedRefName, tautenedOid, message);
                logger.ZLogTrace($"{message}");
            }
            else
            {
                var message = $"Tauten {refName} {tautenedOidHex8} from {hostOidHex8}";
                _tautRepo.SetRef(tautenedRefName, tautenedOid, message);
                logger.ZLogTrace($"{message}");
            }
        }

        var hostHeadRef = _hostRepo.GetHead();
        var hostHeadRefName = hostHeadRef.GetName();
        var tautHeadRefName = _tautenedRefSpec.TransformToTarget(hostHeadRefName);
        _tautRepo.SetHead(tautHeadRefName);
    }

    void TautenFilesInDiff(Lg2Commit hostCommit, Dictionary<string, string> tautenedFilePaths)
    {
        var hostParentCommit = hostCommit.GetParent(0);
        var hostParentTree = hostParentCommit.GetTree();
        var hostTree = hostCommit.GetTree();
        var tautRepoOdb = _tautRepo.GetOdb();

        Lg2DiffOptions diffOptions = new()
        {
            Flags =
                Lg2DiffOptionFlags.LG2_DIFF_IGNORE_FILEMODE
                | Lg2DiffOptionFlags.LG2_DIFF_IGNORE_SUBMODULES,
        };

        var hostDiffToParent = _hostRepo.NewDiff(hostParentTree, hostTree, ref diffOptions);

        Lg2DiffFindOptions diffFindOpts = new()
        {
            Flags =
                Lg2DiffFindFlags.LG2_DIFF_FIND_AND_BREAK_REWRITES
                | Lg2DiffFindFlags.LG2_DIFF_FIND_RENAMES
                | Lg2DiffFindFlags.LG2_DIFF_FIND_COPIES,
        };

        hostDiffToParent.FindSimilar(ref diffFindOpts);

        for (nuint i = 0, count = hostDiffToParent.GetDeltaCount(); i < count; i++)
        {
            var delta = hostDiffToParent.GetDelta(i);

            var status = delta.GetStatus();

            if (
                status != Lg2DeltaType.LG2_DELTA_MODIFIED
                && status != Lg2DeltaType.LG2_DELTA_RENAMED
                && status != Lg2DeltaType.LG2_DELTA_COPIED
            )
            {
                continue;
            }

            if (delta.GetNumOfFiles() != 2)
            {
                throw new InvalidDataException($"There should be two files in the delta");
            }

            var newFile = delta.GetNewFile();
            var oldFile = delta.GetOldFile();

            if (kvStore.HasTautened(newFile))
            {
                continue;
            }

            var newFilePath = newFile.GetPath();

            if (
                _targetPathSpec.MatchPath(newFilePath, Lg2PathSpecFlags.LG2_PATHSPEC_IGNORE_CASE)
                == false
            )
            {
                continue;
            }

            var newFileSize = newFile.GetSize();

            if (newFileSize < DELTA_ENCODING_MIN_BYTES)
            {
                continue;
            }

            var patch = hostDiffToParent.NewPatch(i);
            var patchSize = patch.GetSize();

            var ratio = (double)patchSize / newFileSize;

            if (ratio > DELTA_ENCODING_MAX_RATIO)
            {
                continue;
            }

            using var tautenStream = new PatchTautenStream(patch.NewReadStream());

            var oldFileOidRef = oldFile.GetOidPlainRef();

            var encryptor = cipher.CreateEncryptor(
                tautenStream,
                DATA_COMPRESSION_MAX_RATIO,
                oldFileOidRef.GetRawData()
            );

            using var writeStream = tautRepoOdb.OpenWriteStream(
                encryptor.GetOutputLength(),
                Lg2ObjectType.LG2_OBJECT_BLOB
            );

            var fileName = Path.GetFileName(newFilePath);
            var fileNameStream = new MemoryStream(
                Encoding.UTF8.GetBytes(fileName),
                writable: false
            );
            var compressedFileNameStream = new CompressionStream(fileNameStream);
            var tautenedFileNameStream = new MemoryStream();
            encryptor.ProduceOutput(writeStream, compressedFileNameStream, tautenedFileNameStream);

            Lg2Oid resultOid = new();
            writeStream.FinalizeWrite(ref resultOid);

            StoreTautened(newFile, resultOid, Lg2ObjectType.LG2_OBJECT_BLOB);

            logger.ZLogTrace(
                $"Tauten patch ({tautenStream.Length}:{newFileSize}) for '{newFilePath}'"
            );

            var tautenedFilenameData = tautenedFileNameStream
                .GetBuffer()
                .AsSpan(0, (int)tautenedFileNameStream.Length);

            var tautenedFilename = Convert.ToHexStringLower(tautenedFilenameData);

            tautenedFilePaths.Add(newFilePath, tautenedFilename);

            logger.ZLogTrace($"Tauten name '{fileName}' to '{tautenedFilename}'");
        }
    }

    void TautenCommit(Lg2Commit hostCommit, Dictionary<string, string> tautenedFilePaths)
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

            var task = TautenTreeAsync(hostTree, hostTreePath, tautenedFilePaths);
            task.GetAwaiter().GetResult();
        }

        var oid = new Lg2Oid();
        kvStore.GetTautened(hostTree, ref oid);

        var tautTree = _tautRepo.LookupTree(oid);

        var author = hostCommit.GetAuthor();
        var committer = hostCommit.GetCommitter();
        var message = hostCommit.GetMessage();
        var hostParents = hostCommit.GetAllParents();

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

    async Task TautenTreeAsync(
        Lg2Tree hostTree,
        string hostTreePath,
        Dictionary<string, string> tautenedFilePaths
    )
    {
        await Task.Yield(); // prevent it from running synchronously

        var treeBuilder = _tautRepo.NewTreeBuilder();
        var tautenedSet = new HashSet<nuint>();

        for (nuint i = 0, count = hostTree.GetEntryCount(); i < count; i++)
        {
            var entry = hostTree.GetEntry(i);
            var entryName = entry.GetName();
            var entryPath = string.IsNullOrEmpty(hostTreePath)
                ? entryName
                : string.Join('/', hostTreePath, entryName);
            var entryObjType = entry.GetObjectType();
            var entryFileMode = entry.GetFileModeRaw();
            var entryOidHex8 = entry.GetOidHexDigits(8);

            if (entryObjType.IsTree())
            {
                logger.ZLogTrace($"Start tautening tree {entryOidHex8} '{entryPath}'");

                if (kvStore.HasTautened(entry) == false)
                {
                    var tree = _hostRepo.LookupTree(entry);
                    await TautenTreeAsync(tree, entryPath, tautenedFilePaths);
                }

                Lg2Oid oid = new();
                kvStore.GetTautened(entry, ref oid);

                if (oid.PlainRef.Equals(entry.GetOidPlainRef()) == false)
                {
                    treeBuilder.Insert(entryName, oid, entryFileMode);
                    tautenedSet.Add(i);
                }
            }
            else if (entryObjType.IsBlob())
            {
                logger.ZLogTrace($"Start tautening blob {entryOidHex8} '{entryPath}'");

                if (_targetPathSpec.MatchPath(entryName, Lg2PathSpecFlags.LG2_PATHSPEC_IGNORE_CASE))
                {
                    Lg2Oid oid = new();
                    if (kvStore.TryGetTautened(entry, ref oid) == false)
                    {
                        var blob = _hostRepo.LookupBlob(entry);
                        TautenBlob(blob, ref oid, entryPath, tautenedFilePaths);
                    }
                    else
                    {
                        if (tautenedFilePaths.ContainsKey(entryPath) == false)
                        {
                            var tautenedBlob = _tautRepo.LookupBlob(oid);
                            TautenFileName(tautenedBlob, entryPath, tautenedFilePaths);
                        }
                    }

                    if (oid.PlainRef.Equals(entry.GetOidPlainRef()) == false)
                    {
                        var tautenedFileName = tautenedFilePaths[entryPath];
                        treeBuilder.Insert(tautenedFileName, oid, entryFileMode);
                        tautenedSet.Add(i);
                    }
                }
            }
            else
            {
                logger.ZLogWarning($"Ignore invalid object type {entryObjType.GetName()}");
            }
        }

        if (treeBuilder.GetEntryCount() > 0)
        {
            for (nuint i = 0, count = hostTree.GetEntryCount(); i < count; i++)
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

            Lg2Oid oid = new();
            treeBuilder.Write(ref oid);

            StoreTautened(hostTree, oid);
        }
    }

    void TautenFileName(
        Lg2Blob tautenedBlob,
        string filePath,
        Dictionary<string, string> tautenedFilePaths
    )
    {
        using var tautRepoOdb = _tautRepo.GetOdb();

        var readStream = tautRepoOdb.ReadAsStream(tautenedBlob);
        var recryptor = cipher.CreateRecryptor(readStream);

        var fileName = Path.GetFileName(filePath);
        var fileNameStream = new MemoryStream(Encoding.UTF8.GetBytes(filePath), writable: false);
        var encryptedFileNameStream = new MemoryStream();
        recryptor.Encrypt(fileNameStream, encryptedFileNameStream);

        var tautenedFileNameData = encryptedFileNameStream
            .GetBuffer()
            .AsSpan(0, (int)encryptedFileNameStream.Length);

        var tautenedFilename = Convert.ToHexStringLower(tautenedFileNameData);

        tautenedFilePaths.Add(filePath, tautenedFilename);

        logger.ZLogTrace($"Tauten '{tautenedFilename}' from file name '{filePath}'");
    }

    void TautenBlob(
        Lg2Blob blob,
        scoped ref Lg2Oid resultOid,
        string filePath,
        Dictionary<string, string> tautenedFileNames
    )
    {
        using var hostRepoOdb = _hostRepo.GetOdb();
        using var tautRepoOdb = _tautRepo.GetOdb();

        var readStream = hostRepoOdb.ReadAsStream(blob);

        var encryptor = cipher.CreateEncryptor(readStream, DATA_COMPRESSION_MAX_RATIO);

        using var writeStream = tautRepoOdb.OpenWriteStream(
            encryptor.GetOutputLength(),
            blob.GetObjectType()
        );

        var fileName = Path.GetFileName(filePath);
        var fileNameStream = new MemoryStream(Encoding.UTF8.GetBytes(filePath), writable: false);
        var encryptedFileNameStream = new MemoryStream();

        encryptor.ProduceOutput(writeStream, fileNameStream, encryptedFileNameStream);

        writeStream.FinalizeWrite(ref resultOid);

        StoreTautened(blob, resultOid);

        var tautenedFileNameData = encryptedFileNameStream
            .GetBuffer()
            .AsSpan(0, (int)encryptedFileNameStream.Length);

        var tautenedFilename = Convert.ToHexStringLower(tautenedFileNameData);

        tautenedFileNames.Add(filePath, tautenedFilename);

        logger.ZLogTrace($"Tauten '{tautenedFilename}' from file name '{filePath}'");
    }

    internal void RebuildKvStore()
    {
        if (_tautRepo is null)
        {
            throw new InvalidOperationException($"Taut repo is null");
        }

        logger.ZLogTrace($"Rebuilding {nameof(kvStore)}");

        kvStore.Truncate();

        foreach (var refName in RegainedTautRefs)
        {
            _tautRepo.DeleteRef(refName);
            logger.ZLogTrace($"Delete {refName}");
        }

        foreach (var refName in TautenedTautRefs)
        {
            _tautRepo.DeleteRef(refName);
            logger.ZLogTrace($"Delete {refName}");
        }

        RegainHostRefs();

        TautenHostRefs();
    }

    #endregion Tautening


    class PatchRegainStream : Stream
    {
        readonly MemoryStream _targetStream;

        static readonly byte[] s_diffGitDummyLine = Encoding.ASCII.GetBytes(
            $"diff --git a/dummy b/dummy\n"
        );

        internal PatchRegainStream()
        {
            _targetStream = new();

            _targetStream.Write(s_diffGitDummyLine);
        }

        public override bool CanRead => false;

        public override bool CanSeek => false;

        public override bool CanWrite => true;

        public override long Length => _targetStream.Length;

        public override long Position
        {
            get => _targetStream.Position;
            set => throw new NotSupportedException();
        }

        public override void Flush()
        {
            throw new NotSupportedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            _targetStream.Write(buffer, offset, count);
        }

        internal byte[] GetBuffer()
        {
            return _targetStream.GetBuffer();
        }
    }

    class PatchTautenStream : Stream
    {
        readonly Stream _sourceStream;
        readonly MemoryStream _headerStream;

        readonly long _length;
        readonly long _sourceHeaderOffset;

        long _totalRead;

        static readonly byte[] s_diffGit = Encoding.ASCII.GetBytes("diff --git");
        static readonly byte[] s_tripleMinus = Encoding.ASCII.GetBytes("---");
        static readonly byte[] s_tripleMinusDummyLine = Encoding.ASCII.GetBytes($"--- a/dummy\n");
        static readonly byte[] s_triplePlus = Encoding.ASCII.GetBytes("+++");
        static readonly byte[] s_triplePlusDummyLine = Encoding.ASCII.GetBytes($"+++ b/dummy\n");
        static readonly byte[] s_doubleAt = Encoding.ASCII.GetBytes("@@");

        internal PatchTautenStream(Stream patchInput)
        {
            _sourceStream = patchInput;
            _headerStream = new();

            PrepareHeaderStream();

            _headerStream.Position = 0;
            _sourceHeaderOffset = _sourceStream.Position;
            _length = _headerStream.Length + _sourceStream.Length - _sourceHeaderOffset;
        }

        int ReadLine()
        {
            int dataRead = 0;

            for (; ; )
            {
                var val = _sourceStream.ReadByte();
                if (val == -1)
                {
                    break;
                }

                _headerStream.WriteByte((byte)val);
                dataRead++;

                if (val == '\n')
                {
                    break;
                }
            }

            return dataRead;
        }

        void PrepareHeaderStream()
        {
            for (int totalRead = 0; ; )
            {
                var dataRead = ReadLine();
                if (dataRead == 0)
                {
                    break;
                }

                var buffer = _headerStream.GetBuffer();
                var lineData = buffer.AsSpan(totalRead, dataRead);
                if (lineData.StartsWith(s_diffGit))
                {
                    _headerStream.SetLength(totalRead); // rewind

                    continue;
                }
                if (lineData.StartsWith(s_tripleMinus))
                {
                    _headerStream.SetLength(totalRead); // rewind
                    _headerStream.Write(s_tripleMinusDummyLine);
                    totalRead = (int)_headerStream.Length;

                    continue;
                }
                if (lineData.StartsWith(s_triplePlus))
                {
                    _headerStream.SetLength(totalRead); // rewind
                    _headerStream.Write(s_triplePlusDummyLine);
                    totalRead = (int)_headerStream.Length;

                    continue;
                }
                if (lineData.StartsWith(s_doubleAt))
                {
                    break; // we have passed the diff header, time to break
                }

                totalRead = (int)_headerStream.Length;
            }
        }

        public override bool CanRead => _sourceStream.CanRead;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => _length;

        public override long Position
        {
            get => _totalRead;
            set
            {
                ArgumentOutOfRangeException.ThrowIfNotEqual(value, 0);

                _totalRead = 0;
                _headerStream.Position = 0;
                _sourceStream.Position = _sourceHeaderOffset;
            }
        }

        public override void Flush()
        {
            throw new NotSupportedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int dataRead;

            if (_headerStream.Position != _headerStream.Length)
            {
                dataRead = _headerStream.Read(buffer, offset, count);
            }
            else
            {
                dataRead = _sourceStream.Read(buffer, offset, count);
            }

            _totalRead += dataRead;

            return dataRead;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }
    }
}
