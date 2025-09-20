using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;

namespace Git.Taut;

sealed class UserKeyHolder : IDisposable
{
    const int KeyIterationCount = 64000;
    const int KeyBytes = 32;

    [AllowNull]
    byte[] _crudeKey;

    internal UserKeyHolder() { }

    internal ReadOnlySpan<byte> CrudeKey => _crudeKey!;

    internal bool CrudeKeyIsNull => _crudeKey is null;

    internal void DeriveCrudeKey(ReadOnlySpan<byte> passwordData, ReadOnlySpan<byte> passwordSalt)
    {
        CleanUp();

        _crudeKey = Rfc2898DeriveBytes.Pbkdf2(
            passwordData,
            passwordSalt,
            KeyIterationCount,
            HashAlgorithmName.SHA256,
            KeyBytes
        );
    }

    internal byte[] DeriveCipherKey(ReadOnlySpan<byte> salt, int iterationCount)
    {
        if (_crudeKey is null)
        {
            throw new InvalidOperationException($"{nameof(_crudeKey)} is null");
        }

        var result = Rfc2898DeriveBytes.Pbkdf2(
            CrudeKey,
            salt,
            iterationCount,
            HashAlgorithmName.SHA256,
            KeyBytes
        );

        return result;
    }

    internal string DeriveCredentialKeyTrait(byte[] info)
    {
        var resultData = new byte[16];
        HKDF.Expand(HashAlgorithmName.SHA256, _crudeKey!, resultData, info);

        var result = Convert.ToHexStringLower(resultData);

        return result;
    }

    void CleanUp()
    {
        if (_crudeKey is not null)
        {
            Array.Fill<byte>(_crudeKey, 0);

            _crudeKey = null;
        }
    }

    public void Dispose()
    {
        CleanUp();
    }
}
