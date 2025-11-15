using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Lg2.Sharpy;
using Microsoft.Extensions.Logging;
using ZLogger;
using static Git.Taut.GitAttrConstants;

namespace Git.Taut;

class TautManager(
    ILogger<TautManager> logger,
    TautAttributes tautAttrs,
    TautMapping tautMapping,
    Aes256Cbc1 tautCipher,
    GitCli gitCli
)
{
    [AllowNull]
    Lg2Repository _tautRepo;
    internal Lg2Repository TautRepo => _tautRepo!;

    [AllowNull]
    Lg2Repository _hostRepo;
    internal Lg2Repository HostRepo => _hostRepo!;

    [AllowNull]
    List<string> _gitCredFillOutput;

    bool _initialized;

    internal void Init(Lg2Repository hostRepo, Lg2Repository tautRepo)
    {
        ThrowHelper.InvalidOperationIfAlreadyInitalized(_initialized);

        _initialized = true;

        _hostRepo = hostRepo;
        _tautRepo = tautRepo;

        logger.ZLogTrace($"Initialized {nameof(TautManager)}");
    }

    internal List<string> RegainedTautRefs
    {
        get
        {
            var iter = TautRepo.NewRefIteratorGlob(GitRefSpecs.RefsRegainedText);

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
            var iter = TautRepo.NewRefIteratorGlob(GitRefSpecs.RefsTautenedText);

            List<string> result = [];
            while (iter.NextName(out var refName))
            {
                result.Add(refName);
            }

            return result;
        }
    }

    // NEXT: filter only refs relevant to this taut site
    List<string> FilterRemoteRefs(List<string> refList)
    {
        var result = new List<string>();

        foreach (var refName in refList)
        {
            if (GitRefSpecs.RefsToRefsRemote.DstMatches(refName))
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
        tautMapping.PutSameTautened(source, target);

        if (logger.IsEnabled(LogLevel.Trace))
        {
            var sourceOidText = source.GetOidHexDigits();
            var targetOidText = target.GetOidHexDigits();
            var objTypeName = objType.GetName();

            if (sourceOidText == targetOidText)
            {
                logger.ZLogTrace(
                    $"Tautened {objTypeName} {sourceOidText[..8]} into same {objTypeName}"
                );
            }
            else
            {
                logger.ZLogTrace(
                    $"Tautened {objTypeName} {sourceOidText[..8]} into {targetOidText}"
                );
            }
        }
    }

    void StoreRegained(ILg2ObjectInfo objInfo, Lg2OidPlainRef target)
    {
        var source = objInfo.GetOidPlainRef();

        tautMapping.PutSameRegained(source, target);

        if (logger.IsEnabled(LogLevel.Trace))
        {
            var sourceOidText = source.GetOidHexDigits();
            var targetOidText = target.GetOidHexDigits();
            var objTypeName = objInfo.GetObjectType().GetName();

            if (sourceOidText == targetOidText)
            {
                logger.ZLogTrace($"Regained {targetOidText} from same {objTypeName}");
            }
            else
            {
                logger.ZLogTrace(
                    $"Regained {targetOidText} from {objTypeName} {sourceOidText[..8]}"
                );
            }
        }
    }

    #region Regaining

    internal void RegainRefTags()
    {
        logger.ZLogTrace($"Start regaining ref tags");

        var refList = TautRepo.GetRefList();
        var refTags = GitRefSpecs.FilterLocalRefTags(refList);

        foreach (var refTag in refTags)
        {
            using var tagRef = TautRepo.LookupRef(refTag);
            var tagOid = tagRef.GetTarget();

            if (tautMapping.HasRegained(tagOid) == false)
            {
                var tagOidHex8 = tagOid.GetOidHexDigits(8);

                logger.ZLogTrace($"Start regaining {tagOidHex8}");

                var tautCommit = TautRepo.LookupCommit(tagOid);
                RegainCommit(tautCommit);

                logger.ZLogTrace($"Done regaining {tagOidHex8}");
            }

            Lg2Oid regainedTagOid = new();
            tautMapping.GetRegained(tagOid, ref regainedTagOid);

            var regainedRefTag = GitRefSpecs.RefsToRefsRegained.TransformToTarget(refTag);

            if (TautRepo.TryLookupRef(regainedRefTag, out var regainedTagRef) == false)
            {
                TautRepo.NewRef(regainedRefTag, regainedTagOid, force: false);

                logger.ZLogTrace(
                    $"Created new tag '{regainedRefTag}' '{regainedTagOid.GetOidHexDigits(8)}'"
                );
            }
            else
            {
                var oldRegainedTagOid = regainedTagRef.GetTarget();
                if (oldRegainedTagOid.Equals(regainedTagOid) == false)
                {
                    regainedTagRef.SetTarget(regainedTagOid);

                    logger.ZLogTrace(
                        $"Updated tag '{regainedRefTag}' from '{oldRegainedTagOid.GetOidHexDigits(8)}' to '{regainedTagOid.GetOidHexDigits(8)}'"
                    );
                }
            }
        }

        logger.ZLogTrace($"Done regaining ref tags");
    }

    internal void RegainRefHeads()
    {
        RegainRefHeadsPrivate();

        if (_gitCredFillOutput is not null)
        {
            gitCli.Execute(_gitCredFillOutput, "credential", "approve");
            _gitCredFillOutput = null;
        }
    }

    void RegainRefHeadsPrivate()
    {
        logger.ZLogTrace($"Start regaining ref heads");

        var refHeads = GitRefSpecs.FilterLocalRefHeads(TautRepo.GetRefList());

        using var revWalk = TautRepo.NewRevWalk();

        revWalk.ResetSorting(
            Lg2SortFlags.LG2_SORT_TOPOLOGICAL
                | Lg2SortFlags.LG2_SORT_TIME
                | Lg2SortFlags.LG2_SORT_REVERSE
        );

        using (var regainedRefIter = TautRepo.NewRefIteratorGlob(GitRefSpecs.RefsRegainedHeadsText))
        {
            for (string refName; regainedRefIter.NextName(out refName); )
            {
                Lg2Oid hostOid = new();
                TautRepo.GetRefOid(refName, ref hostOid);

                Lg2Oid tautOid = new();
                tautMapping.GetTautened(hostOid, ref tautOid);

                logger.ZLogTrace($"RevWalk.Hide {tautOid.GetOidHexDigits(8)} ({refName})");

                revWalk.Hide(tautOid);
            }
        }

        foreach (var refName in refHeads)
        {
            logger.ZLogTrace($"RevWalk.Push {refName}");

            revWalk.Push(refName);
        }

        for (Lg2Oid oid = new(); revWalk.Next(ref oid); )
        {
            var tautCommit = TautRepo.LookupCommit(oid);

            var commitOidHex8 = oid.GetOidHexDigits(8);
            var commitSummary = tautCommit.GetSummary();

            if (tautMapping.HasRegained(tautCommit))
            {
                logger.ZLogTrace($"Ignore regained commit {commitOidHex8} '{commitSummary}'");

                continue;
            }

            logger.ZLogTrace($"Start regaining commit {commitOidHex8} '{commitSummary}'");

            RegainCommit(tautCommit);

            logger.ZLogTrace($"Done regaining commit {commitOidHex8} '{commitSummary}'");
        }

        foreach (var refName in refHeads)
        {
            Lg2Oid tautOid = new();
            TautRepo.GetRefOid(refName, ref tautOid);
            var tautOidHex8 = tautOid.GetOidHexDigits(8);

            Lg2Oid hostOid = new();
            tautMapping.GetRegained(tautOid, ref hostOid);
            var hostOidHex8 = hostOid.GetOidHexDigits(8);

            var regainedRefName = GitRefSpecs.RefsToRefsRegained.TransformToTarget(refName);

            if (tautOid.Equals(ref hostOid))
            {
                var message = $"Regained {refName} {hostOidHex8} from same object";

                TautRepo.SetRef(regainedRefName, hostOid, message);
                logger.ZLogTrace($"{message}");
            }
            else
            {
                var message = $"Regained {refName} {hostOidHex8} from {tautOidHex8}";

                TautRepo.SetRef(regainedRefName, hostOid, message);
                logger.ZLogTrace($"{message}");
            }
        }

        logger.ZLogTrace($"Done regaining ref heads");
    }

    internal void RegainCommit(Lg2Commit tautCommit)
    {
        var tautTree = tautCommit.GetTree();
        var tautTreeOidHex8 = tautTree.GetOidHexDigits(8);
        var tautTreePath = string.Empty;

        if (tautMapping.HasRegained(tautTree))
        {
            logger.ZLogTrace($"Ignore regained tree {tautTreeOidHex8} '{tautTreePath}'");
        }
        {
            logger.ZLogTrace($"Start regaining tree {tautTreeOidHex8} '{tautTreePath}'");

            var task = RegainTreeAsync(tautTree);
            task.GetAwaiter().GetResult();

            logger.ZLogTrace($"Done regaining tree {tautTreeOidHex8} '{tautTreePath}'");
        }

        Lg2Oid oid = new();
        tautMapping.GetRegained(tautTree, ref oid);

        var hostTree = HostRepo.LookupTree(oid);

        var author = tautCommit.GetAuthor();
        var committer = tautCommit.GetCommitter();
        var message = tautCommit.GetMessage();
        var tautParents = tautCommit.GetAllParents();

        List<Lg2Commit> hostParents = [];

        foreach (var parent in tautParents)
        {
            tautMapping.GetRegained(parent, ref oid);

            var hostCommit = HostRepo.LookupCommit(oid);
            hostParents.Add(hostCommit);
        }

        HostRepo.NewCommit(author, committer, message, hostTree, hostParents, ref oid);

        StoreRegained(tautCommit, oid);
    }

    void RegainSameObject(ILg2ObjectInfo objInfo)
    {
        using var tautRepoOdb = TautRepo.GetOdb();
        using var hostRepoOdb = HostRepo.GetOdb();

        // XXX use hardlink to improve performance
        tautRepoOdb.CopyObjectIfNotExists(hostRepoOdb, objInfo.GetOidPlainRef());

        StoreRegained(objInfo, objInfo.GetOidPlainRef());
    }

    bool TryDecryptEntryName(
        string entryName,
        Lg2OidPlainRef entryOid,
        out string entryNameToUse,
        out Lg2FileMode fileModeToUse
    )
    {
        byte[] entryNameData;

        try
        {
            entryNameData = Base32Hex.GetBytes(entryName);
        }
        catch (FormatException)
        {
            entryNameToUse = entryName;
            fileModeToUse = Lg2FileMode.LG2_FILEMODE_UNREADABLE;

            return false;
        }

        var entryOidData = entryOid.GetRawData();
        var decDataStream = tautCipher.DecryptName(entryNameData, entryOidData);
        var decData = decDataStream.GetBuffer().AsSpan(0, (int)decDataStream.Length);

        var entryNameToUseData = decData[..^4];
        entryNameToUse = Encoding.UTF8.GetString(entryNameToUseData);

        var fileModeData = decData[^4..].ToArray();
        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(fileModeData);
        }
        var fileMode = BitConverter.ToInt32(fileModeData);

        fileModeToUse = (Lg2FileMode)fileMode;

        if (Enum.IsDefined(fileModeToUse) == false)
        {
            throw new InvalidCastException(
                $"Failed to restore {nameof(Lg2FileMode)} from integer value '{fileMode}'"
            );
        }

        logger.ZLogTrace($"Regained name '{entryNameToUse}' from '{entryName}'");

        return true;
    }

    async Task RegainTreeAsync(Lg2Tree tautTree)
    {
        await Task.Yield(); // prevent it from running synchronously

        var treeBuilder = HostRepo.NewTreeBuilder();
        var regainedSet = new HashSet<nuint>();

        for (nuint i = 0, count = tautTree.GetEntryCount(); i < count; i++)
        {
            var entry = tautTree.GetEntry(i);
            var entryName = entry.GetName();
            var entryObjType = entry.GetObjectType();
            var entryOidHex8 = entry.GetOidHexDigits(8);

            bool isTautened = TryDecryptEntryName(
                entryName,
                entry,
                out var entryNameToUse,
                out var entryFileModeToUse
            );

            if (entryObjType.IsTree())
            {
                logger.ZLogTrace($"Start regaining tree {entryOidHex8} '{entryName}'");

                if (tautMapping.HasRegained(entry) == false)
                {
                    var tree = TautRepo.LookupTree(entry);
                    await RegainTreeAsync(tree);
                }

                var oid = new Lg2Oid();
                tautMapping.GetRegained(entry, ref oid);

                if (
                    oid.PlainRef.Equals(entry.GetOidPlainRef()) == false
                    || entryNameToUse != entryName
                )
                {
                    treeBuilder.Insert(entryNameToUse, oid, entryFileModeToUse);
                    regainedSet.Add(i);
                }

                logger.ZLogTrace($"Done regaining tree {entryOidHex8} '{entryName}'");
            }
            else if (entryObjType.IsBlob())
            {
                logger.ZLogTrace($"Start regaining blob {entryOidHex8} '{entryName}'");

                if (isTautened)
                {
                    var blob = TautRepo.LookupBlob(entry);

                    Lg2Oid oid = new();
                    if (tautMapping.TryGetRegained(entry, ref oid) == false)
                    {
                        RegainBlob(blob, ref oid);
                    }

                    treeBuilder.Insert(entryNameToUse, oid, entryFileModeToUse);
                    regainedSet.Add(i);
                }
                else
                {
                    RegainSameObject(entry);
                }

                logger.ZLogTrace($"Done regaining blob {entryOidHex8} '{entryName}'");
            }
            else if (entryObjType.IsCommit())
            {
                logger.ZLogTrace($"Skip submodule {entryName}");
            }
            else
            {
                throw new InvalidOperationException(
                    $"Unexpected object type {entryObjType.GetName()}"
                );
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

            Lg2Oid oid = new();
            treeBuilder.Write(ref oid);

            StoreRegained(tautTree, oid);
        }
        else
        {
            RegainSameObject(tautTree);
        }
    }

    void RegainBlob(Lg2Blob blob, ref Lg2Oid resultOid)
    {
        using var tautRepoOdb = TautRepo.GetOdb();
        using var hostRepoOdb = HostRepo.GetOdb();

        using var readStream = tautRepoOdb.ReadAsStream(blob);

        var decryptor = tautCipher.CreateDecryptor(readStream);
        var extraPayload = decryptor.GetExtraPayload();
        if (extraPayload.Length > 0)
        {
            var resultStream = new PatchRegainStream();
            decryptor.ProduceOutput(resultStream);

            var resultData = resultStream.GetBuffer().AsSpan(0, (int)resultStream.Length);

            var diff = Lg2Diff.New(resultData);
            if (diff.GetDeltaCount() != 1)
            {
                throw new InvalidDataException("There should be one delta on the diff");
            }
            var patch = diff.NewPatch(0);

            Lg2Oid oid = new();
            oid.FromRaw(extraPayload);

            using var baseOdbOject = hostRepoOdb.Read(oid);

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
            decryptor.ProduceOutput(writeStream);
            writeStream.FinalizeWrite(ref resultOid);

            StoreRegained(blob, resultOid);
        }
    }

    #endregion Regaining

    #region Tautening

    internal void TautenRefTags()
    {
        logger.ZLogTrace($"Start tautening ref tags");

        var refList = HostRepo.GetRefList();
        var refTags = GitRefSpecs.FilterLocalRefTags(refList);

        foreach (var refTag in refTags)
        {
            using var tagRef = HostRepo.LookupRef(refTag);
            var tagOid = tagRef.GetTarget();

            if (tautMapping.HasTautened(tagOid) == false)
            {
                var tagOidHex8 = tagOid.GetOidHexDigits(8);

                logger.ZLogTrace($"Start tautening {tagOidHex8}");

                var hostCommit = HostRepo.LookupCommit(tagOid);
                TautenCommit(hostCommit);

                logger.ZLogTrace($"Done tautening {tagOidHex8}");
            }

            Lg2Oid tautenedTagOid = new();
            tautMapping.GetTautened(tagOid, ref tautenedTagOid);

            var tautenedRefTag = GitRefSpecs.RefsToRefsTautened.TransformToTarget(refTag);

            if (TautRepo.TryLookupRef(tautenedRefTag, out var tautenedTagRef) == false)
            {
                TautRepo.NewRef(tautenedRefTag, tautenedTagOid, force: false);

                logger.ZLogTrace(
                    $"Created new tag '{tautenedRefTag}' '{tautenedTagOid.GetOidHexDigits(8)}'"
                );
            }
            else
            {
                var oldTautenedTagOid = tautenedTagRef.GetTarget();
                if (oldTautenedTagOid.Equals(tautenedTagOid) == false)
                {
                    tautenedTagRef.SetTarget(tautenedTagOid);

                    logger.ZLogTrace(
                        $"Updated tag '{tautenedRefTag}' from '{oldTautenedTagOid.GetOidHexDigits(8)}' to '{tautenedTagOid.GetOidHexDigits(8)}'"
                    );
                }
            }
        }

        logger.ZLogTrace($"Done tautening ref tags");
    }

    internal void TautenRefHeads()
    {
        logger.ZLogTrace($"Start tautening ref heads");

        var hostRefHeads = FilterRemoteRefs(HostRepo.GetRefList());

        using var revWalk = HostRepo.NewRevWalk();

        revWalk.ResetSorting(
            Lg2SortFlags.LG2_SORT_TOPOLOGICAL
                | Lg2SortFlags.LG2_SORT_TIME
                | Lg2SortFlags.LG2_SORT_REVERSE
        );

        using (var tautenedRefIter = TautRepo.NewRefIteratorGlob(GitRefSpecs.RefsTautenedHeadsText))
        {
            for (string refName; tautenedRefIter.NextName(out refName); )
            {
                Lg2Oid tautOid = new();
                TautRepo.GetRefOid(refName, ref tautOid);

                Lg2Oid hostOid = new();
                tautMapping.GetRegained(tautOid, ref hostOid);

                logger.ZLogTrace($"RevWalk.Hide {hostOid.GetOidHexDigits(8)} ({refName})");

                revWalk.Hide(hostOid);
            }
        }

        foreach (var refName in hostRefHeads)
        {
            logger.ZLogTrace($"RevWalk.Push {refName}");

            revWalk.Push(refName);
        }

        for (Lg2Oid oid = new(); revWalk.Next(ref oid); )
        {
            var hostCommit = HostRepo.LookupCommit(oid);

            var commitOidHex8 = oid.GetOidHexDigits(8);
            var commitSummary = hostCommit.GetSummary();

            if (tautMapping.HasTautened(hostCommit))
            {
                logger.ZLogTrace($"Skip tautened commit {commitOidHex8} '{commitSummary}'");

                continue;
            }

            logger.ZLogTrace($"Start tautening commit {commitOidHex8} '{commitSummary}'");

            TautenCommit(hostCommit);

            logger.ZLogTrace($"Done tautening commit {commitOidHex8} '{commitSummary}'");
        }

        foreach (var refName in hostRefHeads)
        {
            Lg2Oid hostOid = new();
            HostRepo.GetRefOid(refName, ref hostOid);
            var hostOidHex8 = hostOid.GetOidHexDigits(8);

            Lg2Oid tautenedOid = new();
            tautMapping.GetTautened(hostOid, ref tautenedOid);
            var tautenedOidHex8 = tautenedOid.GetOidHexDigits(8);

            var tautenedRefName = GitRefSpecs.RefsToRefsTautened.TransformToTarget(refName);

            if (hostOid.Equals(ref tautenedOid))
            {
                var message = $"Tautened {refName} {tautenedOidHex8} from same object";

                TautRepo.SetRef(tautenedRefName, tautenedOid, message);
                logger.ZLogTrace($"{message}");
            }
            else
            {
                var message = $"Tautened {refName} {tautenedOidHex8} from {hostOidHex8}";

                TautRepo.SetRef(tautenedRefName, tautenedOid, message);
                logger.ZLogTrace($"{message}");
            }
        }

        // NEXT: do in while handling pushes
        var hostHeadRef = HostRepo.GetHead();
        var hostHeadRefName = hostHeadRef.GetName();
        var tautHeadRefName = GitRefSpecs.RefsToRefsTautened.TransformToTarget(hostHeadRefName);
        TautRepo.SetHead(tautHeadRefName);

        logger.ZLogTrace($"Done tautening ref heads");
    }

    void TautenFilesInDiff(Lg2Commit hostCommit, Lg2AttrOptions hostAttrOpts)
    {
        var hostParentCommit = hostCommit.GetParent(0);
        var hostParentTree = hostParentCommit.GetTree();
        var hostTree = hostCommit.GetTree();

        Lg2DiffOptions diffOptions = new()
        {
            Flags =
                Lg2DiffOptionFlags.LG2_DIFF_IGNORE_FILEMODE
                | Lg2DiffOptionFlags.LG2_DIFF_IGNORE_SUBMODULES,
        };

        var hostDiffToParent = HostRepo.NewDiff(hostParentTree, hostTree, ref diffOptions);

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

            if (tautMapping.HasTautened(newFile))
            {
                continue;
            }

            var newFilePath = newFile.GetPath();

            var tautAttrVal = HostRepo.GetTautAttrValue(newFilePath, hostAttrOpts);
            if (tautAttrVal.IsSetOrSpecified == false)
            {
                continue;
            }

            var deltaEncodingEnalbingSize = tautAttrs.GetDeltaEncodingEnablingSize(
                newFilePath,
                HostRepo,
                hostAttrOpts
            );
            if (deltaEncodingEnalbingSize == 0)
            {
                continue;
            }

            var newFileSize = newFile.GetSize();
            if (newFileSize < (ulong)deltaEncodingEnalbingSize)
            {
                continue;
            }

            var deltaEncodingTargetRatio = tautAttrs.GetDeltaEncodingTargetRatio(
                newFilePath,
                HostRepo,
                hostAttrOpts
            );
            if (deltaEncodingTargetRatio == DELTA_ENCODING_ENABLING_SIZE_DISABLED_VALUE)
            {
                continue;
            }

            var patch = hostDiffToParent.NewPatch(i);
            var patchSize = patch.GetSize();

            var ratio = (double)patchSize / newFileSize;
            if (ratio > deltaEncodingTargetRatio)
            {
                continue;
            }

            using var tautenStream = new PatchTautenStream(patch.NewReadStream());

            var compressionTargetRatio = tautAttrs.GetCompressionTargetRatio(
                newFilePath,
                HostRepo,
                hostAttrOpts
            );

            var oldFileOidRef = oldFile.GetOidPlainRef();

            var encryptor = tautCipher.CreateEncryptor(
                tautenStream,
                compressionTargetRatio,
                oldFileOidRef.GetRawData()
            );

            using var tautRepoOdb = TautRepo.GetOdb();

            using var writeStream = tautRepoOdb.OpenWriteStream(
                encryptor.GetOutputLength(),
                Lg2ObjectType.LG2_OBJECT_BLOB
            );

            encryptor.ProduceOutput(writeStream);

            Lg2Oid resultOid = new();
            writeStream.FinalizeWrite(ref resultOid);

            StoreTautened(newFile, resultOid, Lg2ObjectType.LG2_OBJECT_BLOB);

            logger.ZLogTrace(
                $"Tautened patch ({tautenStream.Length}:{newFileSize}) for '{newFilePath}'"
            );
        }
    }

    internal void TautenCommit(Lg2Commit hostCommit)
    {
        var hostTree = hostCommit.GetTree();
        var hostTreeOidHex8 = hostTree.GetOidHexDigits(8);
        var hostTreePath = string.Empty;

        if (tautMapping.HasTautened(hostTree))
        {
            logger.ZLogTrace($"Skip tautened tree {hostTreeOidHex8} '{hostTreePath}'");
        }
        else
        {
            Lg2AttrOptions hostAttrOpts = new()
            {
                Flags =
                    Lg2AttrCheckFlags.LG2_ATTR_CHECK_NO_SYSTEM
                    | Lg2AttrCheckFlags.LG2_ATTR_CHECK_INCLUDE_COMMIT,
            };

            hostAttrOpts.SetCommitId(hostCommit);

            if (hostCommit.GetParentCount() == 1)
            {
                logger.ZLogTrace($"Start tautening diff for commit {hostTreeOidHex8}");

                TautenFilesInDiff(hostCommit, hostAttrOpts);

                logger.ZLogTrace($"Done tautening diff for commit {hostTreeOidHex8}");
            }

            logger.ZLogTrace($"Start tautening tree {hostTreeOidHex8} '{hostTreePath}'");

            var task = TautenTreeAsync(hostTree, hostTreePath, hostAttrOpts);
            task.GetAwaiter().GetResult();

            logger.ZLogTrace($"Done tautening tree {hostTreeOidHex8} '{hostTreePath}'");

            HostRepo.FlushAttrCache();
        }

        Lg2Oid oid = new();
        tautMapping.GetTautened(hostTree, ref oid);

        var tautTree = TautRepo.LookupTree(oid);

        var author = hostCommit.GetAuthor();
        var committer = hostCommit.GetCommitter();
        var message = hostCommit.GetMessage();
        var hostParents = hostCommit.GetAllParents();

        var tautParents = new List<Lg2Commit>();

        foreach (var parent in hostParents)
        {
            tautMapping.GetTautened(parent, ref oid);

            var tautCommit = TautRepo.LookupCommit(oid);
            tautParents.Add(tautCommit);
        }

        TautRepo.NewCommit(author, committer, message, tautTree, tautParents, ref oid);

        StoreTautened(hostCommit, oid);
    }

    string EncryptEntryName(string entryName, Lg2FileMode entryFileMode, scoped ref Lg2Oid oid)
    {
        var entryNameDataBuffer = new ArrayBufferWriter<byte>();

        Encoding.UTF8.GetBytes(entryName, entryNameDataBuffer);

        var fileModeData = BitConverter.GetBytes((uint)entryFileMode);
        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(fileModeData);
        }

        entryNameDataBuffer.Write(fileModeData);

        var entryNameData = entryNameDataBuffer.WrittenSpan.ToArray();
        var oidRawData = oid.GetRawData();
        var encStream = tautCipher.EncryptName(entryNameData, oidRawData);
        var encData = encStream.GetBuffer().AsSpan(0, (int)encStream.Length);

        var result = Base32Hex.GetString(encData);

        return result;
    }

    async Task TautenTreeAsync(Lg2Tree hostTree, string hostTreePath, Lg2AttrOptions hostAttrOpts)
    {
        await Task.Yield(); // prevent it from running synchronously

        var treeBuilder = TautRepo.NewTreeBuilder();
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

            var entryNameToUse = entryName;

            var tautAttrVal = HostRepo.GetTautAttrValue(entryPath, hostAttrOpts);

            if (entryObjType.IsTree())
            {
                if (tautMapping.HasTautened(entry) == false)
                {
                    logger.ZLogTrace($"Start tautening tree {entryOidHex8} '{entryPath}'");

                    var tree = HostRepo.LookupTree(entry);
                    await TautenTreeAsync(tree, entryPath, hostAttrOpts);

                    logger.ZLogTrace($"Done tautening tree {entryOidHex8} '{entryPath}'");
                }
                else
                {
                    logger.ZLogTrace($"Skip tautened tree {entryOidHex8} '{entryPath}'");
                }

                Lg2Oid oid = new();
                tautMapping.GetTautened(entry, ref oid);

                if (tautAttrVal.IsSetOrSpecified)
                {
                    entryNameToUse = EncryptEntryName(entryName, entryFileMode, ref oid);
                }

                if (oid.PlainRefEquals(entry) == false || entryNameToUse != entryName)
                {
                    treeBuilder.Insert(entryNameToUse, oid, entryFileMode);
                    tautenedSet.Add(i);
                }
            }
            else if (entryObjType.IsBlob())
            {
                if (tautAttrVal.IsSetOrSpecified)
                {
                    Lg2Oid oid = new();
                    if (tautMapping.TryGetTautened(entry, ref oid) == false)
                    {
                        logger.ZLogTrace($"Start tautening blob {entryOidHex8} '{entryPath}'");

                        var blob = HostRepo.LookupBlob(entry);
                        TautenBlob(blob, ref oid, entryPath, hostAttrOpts);

                        logger.ZLogTrace($"Done tautening blob {entryOidHex8} '{entryPath}'");
                    }
                    else
                    {
                        logger.ZLogTrace($"Skip tautened blob {entryOidHex8} '{entryPath}'");
                    }

                    entryNameToUse = EncryptEntryName(entryName, entryFileMode, ref oid);

                    treeBuilder.Insert(entryNameToUse, oid, Lg2FileMode.LG2_FILEMODE_BLOB);
                    tautenedSet.Add(i);
                }
                else
                {
                    logger.ZLogTrace($"Pass through blob {entryOidHex8} '{entryPath}'");
                }
            }
            else if (entryObjType.IsCommit())
            {
                logger.ZLogTrace($"Skip submodule {entryName}");
            }
            else
            {
                throw new InvalidOperationException(
                    $"Unexpected object type {entryObjType.GetName()}"
                );
            }
        }

        if (treeBuilder.GetEntryCount() > 0)
        {
            for (nuint i = 0, count = hostTree.GetEntryCount(); i < count; i++)
            {
                if (tautenedSet.Contains(i) == false)
                {
                    var entry = hostTree.GetEntry(i);
                    var entryName = entry.GetName();
                    var entryFileMode = entry.GetFileModeRaw();

                    treeBuilder.Insert(entryName, entry, entryFileMode);
                }
            }

            Lg2Oid oid = new();
            treeBuilder.Write(ref oid);

            StoreTautened(hostTree, oid);
        }
        else
        {
            StoreTautened(hostTree, hostTree);
        }
    }

    void TautenBlob(
        Lg2Blob blob,
        scoped ref Lg2Oid resultOid,
        string filePath,
        Lg2AttrOptions hostAttrOpts
    )
    {
        using var hostRepoOdb = HostRepo.GetOdb();
        using var tautRepoOdb = TautRepo.GetOdb();

        using var readStream = hostRepoOdb.ReadAsStream(blob);

        var compressionTargetRatio = tautAttrs.GetCompressionTargetRatio(
            filePath,
            HostRepo,
            hostAttrOpts
        );

        var encryptor = tautCipher.CreateEncryptor(readStream, compressionTargetRatio);

        using var writeStream = tautRepoOdb.OpenWriteStream(
            encryptor.GetOutputLength(),
            blob.GetObjectType()
        );

        encryptor.ProduceOutput(writeStream);

        writeStream.FinalizeWrite(ref resultOid);

        StoreTautened(blob, resultOid);
    }

    #endregion Tautening

    internal void RebuildKvStore()
    {
        if (TautRepo is null)
        {
            throw new InvalidOperationException($"Taut repo is null");
        }

        logger.ZLogTrace($"Rebuilding {nameof(tautMapping)}");

        tautMapping.Truncate();

        foreach (var refName in RegainedTautRefs)
        {
            TautRepo.DeleteRef(refName);
            logger.ZLogTrace($"Delete {refName}");
        }

        foreach (var refName in TautenedTautRefs)
        {
            TautRepo.DeleteRef(refName);
            logger.ZLogTrace($"Delete {refName}");
        }

        RegainRefHeads();

        TautenRefHeads();
    }
}
