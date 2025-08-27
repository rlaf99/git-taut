using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;

namespace Git.Taut;

sealed class UserKeyBase : IDisposable
{
    [AllowNull]
    byte[] _hashedPass;

    internal UserKeyBase() { }

    internal UserKeyBase(byte[] passwordData)
    {
        _hashedPass = SHA256.HashData(passwordData);
    }

    internal byte[] HashedPass => _hashedPass!;

    internal void SetPasswordData(ReadOnlySpan<byte> passwordData)
    {
        ClearPass();

        _hashedPass = SHA256.HashData(passwordData);
    }

    internal byte[] GenerateCipherKey(ReadOnlySpan<byte> salt, int keyLength, int iterationCount)
    {
        return Rfc2898DeriveBytes.Pbkdf2(
            HashedPass,
            salt,
            iterationCount,
            HashAlgorithmName.SHA256,
            keyLength
        );
    }

    internal string GenerateCredentialTag(byte[] info)
    {
        var resultData = new byte[16];
        HKDF.Expand(HashAlgorithmName.SHA256, _hashedPass!, resultData, info);
        var result = Convert.ToHexStringLower(resultData);

        return result;
    }

    void ClearPass()
    {
        if (_hashedPass is not null)
        {
            Array.Fill<byte>(_hashedPass, 0);
            _hashedPass = null;
        }
    }

    public void Dispose()
    {
        ClearPass();
    }
}
