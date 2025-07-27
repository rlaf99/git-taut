using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;

namespace Git.Taut;

class UserKeyBase
{
    [AllowNull]
    byte[] _hashedPass;

    internal byte[] HashedPass
    {
        get
        {
            if (_hashedPass is null)
            {
                if (GetUserPassword is null)
                {
                    throw new InvalidOperationException($"{nameof(GetUserPassword)} is null");
                }

                var userPassBytes = GetUserPassword();
                _hashedPass = SHA256.HashData(userPassBytes);
            }

            return _hashedPass;
        }
    }

    internal Func<byte[]>? GetUserPassword { get; set; }

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
}
