using System.Diagnostics.CodeAnalysis;
using Lg2.Sharpy;
using LightningDB;
using Microsoft.Extensions.Logging;
using ZLogger;

namespace Git.Taut;

class KeyValueStore(ILogger<KeyValueStore> logger)
{
    [AllowNull]
    LightningEnvironment _dbEnv;

    [AllowNull]
    LightningDatabase _host2TautDb;

    [AllowNull]
    LightningDatabase _taut2HostDb;

    const string DbDirectoryName = "tautened";
    const string Host2TautDbName = "host2taut";
    const string Taut2HostDbName = "taut2host";

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
            _host2TautDb = txn.OpenDatabase(Host2TautDbName, dbConfig);
            _taut2HostDb = txn.OpenDatabase(Taut2HostDbName, dbConfig);

            txn.Commit();
        }

        logger.ZLogTrace($"Initialize {nameof(KeyValueStore)}");
    }

    internal bool TryGetTautened(Lg2OidPlainRef oidRef, ref Lg2Oid resultOid)
    {
        using var txn = _dbEnv.BeginTransaction();
        var result = txn.TryGet(_host2TautDb, oidRef, ref resultOid);
        txn.Commit();

        return result;
    }

    internal void GetTautened(Lg2OidPlainRef oidRef, ref Lg2Oid resultOid)
    {
        using var txn = _dbEnv.BeginTransaction();
        txn.Get(_host2TautDb, oidRef, ref resultOid);
        txn.Commit();
    }

    internal void SetTautened(Lg2OidPlainRef hostOidRef, Lg2OidPlainRef tautOidRef)
    {
        using var txn = _dbEnv.BeginTransaction();
        txn.Put(_host2TautDb, hostOidRef, tautOidRef);
        txn.Commit();
    }
}

static class LightningExtensions
{
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
}
