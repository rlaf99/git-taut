using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using Lg2.Sharpy;
using LightningDB;
using LightningDB.Native;
using Microsoft.Extensions.Logging;
using ZLogger;

namespace Git.Taut;

sealed class TautMapping(ILogger<TautMapping> logger) : IDisposable
{
    [AllowNull]
    LightningEnvironment _dbEnv;

    [AllowNull]
    LightningDatabase _tautenedDb;

    [AllowNull]
    LightningDatabase _regainedDb;

    const string Host2TautDbName = "tautened";
    const string Taut2HostDbName = "regained";

    [AllowNull]
    string _dbPath;

    internal string DbPath => _dbPath;

    bool _initialized;

    internal void Init(string location)
    {
        ThrowHelper.InvalidOperationIfAlreadyInitalized(_initialized);

        _initialized = true;

        _dbPath = Path.Join(location, nameof(TautMapping));
        Directory.CreateDirectory(_dbPath);

        OpenDb();

        logger.ZLogTrace($"Initialized {nameof(TautMapping)}");
    }

    void OpenDb()
    {
        var envConfig = new EnvironmentConfiguration() { MaxDatabases = 2 };
        _dbEnv = new LightningEnvironment(_dbPath, envConfig);
        _dbEnv.Open();

        var dbConfig = new DatabaseConfiguration { Flags = DatabaseOpenFlags.Create };

        using (var txn = _dbEnv.BeginTransaction())
        {
            _tautenedDb = txn.OpenDatabase(Host2TautDbName, dbConfig);
            _regainedDb = txn.OpenDatabase(Taut2HostDbName, dbConfig);

            txn.Commit();
        }
    }

    internal void Truncate()
    {
        if (_dbPath is null)
        {
            throw new InvalidOperationException($"DbPath is null");
        }

        logger.ZLogTrace($"Truncating '{_dbPath}'");

        _tautenedDb?.Dispose();
        _tautenedDb = null;
        _regainedDb?.Dispose();
        _regainedDb = null;
        _dbEnv?.Dispose();
        _dbEnv = null;

        Directory.Delete(_dbPath, recursive: true);
        Directory.CreateDirectory(_dbPath);

        OpenDb();
    }

    internal bool HasTautened(Lg2OidPlainRef oidRef)
    {
        using var txn = _dbEnv.BeginTransaction();
        var result = txn.ContainsKey(_tautenedDb, oidRef);
        txn.Commit();

        return result;
    }

    internal bool HasRegained(Lg2OidPlainRef oidRef)
    {
        using var txn = _dbEnv.BeginTransaction();
        var result = txn.ContainsKey(_regainedDb, oidRef);
        txn.Commit();

        return result;
    }

    internal bool TryGetTautened(Lg2OidPlainRef oidRef, ref Lg2Oid resultOid)
    {
        using var txn = _dbEnv.BeginTransaction();
        var result = txn.TryGet(_tautenedDb, oidRef, ref resultOid);
        txn.Commit();

        return result;
    }

    internal bool TryGetRegained(Lg2OidPlainRef oidRef, ref Lg2Oid resultOid)
    {
        using var txn = _dbEnv.BeginTransaction();
        var result = txn.TryGet(_regainedDb, oidRef, ref resultOid);
        txn.Commit();

        return result;
    }

    internal void GetTautened(Lg2OidPlainRef oidRef, ref Lg2Oid resultOid)
    {
        using var txn = _dbEnv.BeginTransaction();
        txn.Get(_tautenedDb, oidRef, ref resultOid);
        txn.Commit();
    }

    internal void GetRegained(Lg2OidPlainRef oidRef, ref Lg2Oid resultOid)
    {
        using var txn = _dbEnv.BeginTransaction();
        txn.Get(_regainedDb, oidRef, ref resultOid);
        txn.Commit();
    }

    internal void PutSameTautened(Lg2OidPlainRef hostOidRef, Lg2OidPlainRef tautOidRef)
    {
        using var txn = _dbEnv.BeginTransaction();
        txn.PutSame(_tautenedDb, hostOidRef, tautOidRef);
        txn.PutSame(_regainedDb, tautOidRef, hostOidRef);
        txn.Commit();
    }

    internal void PutSameRegained(Lg2OidPlainRef tautOidRef, Lg2OidPlainRef hostOidRef)
    {
        using var txn = _dbEnv.BeginTransaction();
        txn.PutSame(_regainedDb, tautOidRef, hostOidRef);
        txn.PutSame(_tautenedDb, hostOidRef, tautOidRef);
        txn.Commit();
    }

    bool _disposed = false;

    void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }
        _disposed = true;

        if (disposing)
        {
            _tautenedDb?.Dispose();
            _regainedDb?.Dispose();
            _dbEnv?.Dispose();
        } // managed resource
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}

static class LightningExtensions
{
    internal static bool ContainsKey(
        this LightningTransaction txn,
        LightningDatabase db,
        Lg2OidPlainRef oidRef
    )
    {
        var key = oidRef.GetRawData();
        return txn.ContainsKey(db, key);
    }

    internal static bool TryGet(
        this LightningTransaction txn,
        LightningDatabase db,
        Lg2OidPlainRef oidRef,
        ref Lg2Oid oid
    )
    {
        var key = oidRef.GetRawData();
        var (rc, _, value) = txn.Get(db, key);

        if (rc != MDBResultCode.Success)
        {
            return false;
        }

        var source = value.AsSpan();
        var target = oid.GetRawData();

        if (source.Length != target.Length)
        {
            throw new InvalidDataException(
                $"Mismatched length, '{source.Length}' != '{target.Length}'"
            );
        }

        source.CopyTo(target);

        return true;
    }

    internal static void Get(
        this LightningTransaction txn,
        LightningDatabase db,
        Lg2OidPlainRef oidRef,
        ref Lg2Oid oid
    )
    {
        var key = oidRef.GetRawData();
        var (rc, _, value) = txn.Get(db, key);
        if (rc != MDBResultCode.Success)
        {
            var mdbError = Marshal.PtrToStringUTF8(Lmdb.mdb_strerror((int)rc));
            throw new InvalidOperationException($"Failed to get: {mdbError}");
        }

        var source = value.AsSpan();
        var target = oid.GetRawData();

        if (source.Length != target.Length)
        {
            throw new InvalidDataException(
                $"Mismatched length, '{source.Length}' != '{target.Length}'"
            );
        }

        source.CopyTo(target);
    }

    internal static void Put(
        this LightningTransaction txn,
        LightningDatabase db,
        Lg2OidPlainRef sourceOidRef,
        Lg2OidPlainRef targetOidRef,
        PutOptions options = PutOptions.None
    )
    {
        var key = sourceOidRef.GetRawData();
        var val = targetOidRef.GetRawData();

        var rc = txn.Put(db, key, val, options);
        if (rc != MDBResultCode.Success)
        {
            var mdbError = Marshal.PtrToStringUTF8(Lmdb.mdb_strerror((int)rc));
            throw new InvalidOperationException($"Failed to put: {mdbError}");
        }
    }

    internal static void PutSame(
        this LightningTransaction txn,
        LightningDatabase db,
        Lg2OidPlainRef sourceOidRef,
        Lg2OidPlainRef targetOidRef
    )
    {
        var key = sourceOidRef.GetRawData();
        var val = targetOidRef.GetRawData();

        var (rc, _, value) = txn.Get(db, key);
        if (rc == MDBResultCode.Success)
        {
            var storedValue = value.AsSpan();

            if (storedValue.SequenceEqual(val) == false)
            {
                var targetOidText = targetOidRef.GetOidHexDigits();

                Lg2Oid oid = new();
                oid.FromRaw(storedValue);
                var storedOidText = oid.GetOidHexDigits();

                throw new InvalidDataException(
                    $"{targetOidText} does not match stored {storedOidText}"
                );
            }
        }
        else
        {
            if (rc != MDBResultCode.NotFound)
            {
                var mdbError = Marshal.PtrToStringUTF8(Lmdb.mdb_strerror((int)rc));
                throw new InvalidOperationException($"Failed to get: {mdbError}");
            }
            rc = txn.Put(db, key, val, PutOptions.NoOverwrite);
            if (rc != MDBResultCode.Success)
            {
                var mdbError = Marshal.PtrToStringUTF8(Lmdb.mdb_strerror((int)rc));
                throw new InvalidOperationException($"Failed to put: {mdbError}");
            }
        }
    }
}
