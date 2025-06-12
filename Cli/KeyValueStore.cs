using System.Diagnostics.CodeAnalysis;
using System.Text;
using Lg2.Sharpy;
using LightningDB;
using Microsoft.Extensions.Logging;
using ZLogger;

namespace Git.Taut;

sealed class KeyValueStore(ILogger<KeyValueStore> logger) : IDisposable
{
    [AllowNull]
    LightningEnvironment _dbEnv;

    [AllowNull]
    LightningDatabase _tautenedDb;

    [AllowNull]
    LightningDatabase _regainedDb;

    const string DbDirectoryName = "taut";
    const string Host2TautDbName = "tautened";
    const string Taut2HostDbName = "regained";

    [AllowNull]
    string _dbPath;

    internal string DbPath => _dbPath;

    internal void Init(string location)
    {
        _dbPath = Path.Join(location, DbDirectoryName);

        Directory.CreateDirectory(_dbPath);

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

        logger.ZLogTrace($"Initialize {nameof(KeyValueStore)}");
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

    internal void PutTautened(Lg2OidPlainRef hostOidRef, Lg2OidPlainRef tautOidRef)
    {
        using var txn = _dbEnv.BeginTransaction();
        txn.Put(_tautenedDb, hostOidRef, tautOidRef);
        txn.PutSame(_regainedDb, tautOidRef, hostOidRef);
        txn.Commit();
    }

    internal void PutRegained(Lg2OidPlainRef tautOidRef, Lg2OidPlainRef hostOidRef)
    {
        using var txn = _dbEnv.BeginTransaction();
        txn.Put(_regainedDb, tautOidRef, hostOidRef);
        txn.PutSame(_tautenedDb, hostOidRef, tautOidRef);
        txn.Commit();
    }

    bool _disposed = false;

    void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            _disposed = true;

            if (disposing)
            {
                _tautenedDb?.Dispose();
                _regainedDb?.Dispose();
                _dbEnv?.Dispose();
            } // managed resource
        }
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
        var key = oidRef.GetReadOnlyBytes();
        return txn.ContainsKey(db, key);
    }

    internal static bool TryGet(
        this LightningTransaction txn,
        LightningDatabase db,
        Lg2OidPlainRef oidRef,
        ref Lg2Oid oid
    )
    {
        var key = oidRef.GetReadOnlyBytes();
        var (rc, _, value) = txn.Get(db, key);

        if (rc != MDBResultCode.Success)
        {
            return false;
        }

        var source = value.AsSpan();
        var target = oid.GetBytes();

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
        var key = oidRef.GetReadOnlyBytes();
        var (rc, _, value) = txn.Get(db, key);

        if (rc != MDBResultCode.Success)
        {
            throw new InvalidOperationException($"Failed to get value");
        }

        var source = value.AsSpan();
        var target = oid.GetBytes();

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
        Lg2OidPlainRef targetOidRef
    )
    {
        var key = sourceOidRef.GetReadOnlyBytes();
        var val = targetOidRef.GetReadOnlyBytes();

        var rc = txn.Put(db, key, val);
        if (rc != MDBResultCode.Success)
        {
            throw new InvalidOperationException($"Failed to put value");
        }
    }

    internal static void PutSame(
        this LightningTransaction txn,
        LightningDatabase db,
        Lg2OidPlainRef sourceOidRef,
        Lg2OidPlainRef targetOidRef
    )
    {
        var key = sourceOidRef.GetReadOnlyBytes();
        var val = targetOidRef.GetReadOnlyBytes();

        var (rc, _, value) = txn.Get(db, key);
        if (rc == MDBResultCode.Success)
        {
            var storedValue = value.AsSpan();

            if (storedValue.SequenceEqual(val) == false)
            {
                var valString = Encoding.UTF8.GetString(val);
                var storedValueString = Encoding.UTF8.GetString(storedValue);

                throw new InvalidDataException(
                    $"{valString} does not match stored {storedValueString}"
                );
            }
        }
        else
        {
            if (rc != MDBResultCode.NotFound)
            {
                throw new InvalidOperationException($"Failed to get value");
            }
            rc = txn.Put(db, key, val);
            if (rc != MDBResultCode.Success)
            {
                throw new InvalidOperationException($"Failed to put value");
            }
        }
    }
}
