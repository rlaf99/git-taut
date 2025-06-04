using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using ZLogger;

namespace Git.Remote.Taut;

class UserKeyBase
{
    internal const int ITERATION_COUNT = 64007;

    [AllowNull]
    byte[] _hashedPass;

    internal byte[] HashedPass
    {
        get
        {
            if (_hashedPass is null)
            {
                throw new InvalidOperationException($"No user password hashed");
            }
            return _hashedPass;
        }
    }

    byte[] GetUserPasswordInBytes()
    {
        return Encoding.UTF8.GetBytes("Hello!");
    }

    void Generate()
    {
        var userPassBytes = GetUserPasswordInBytes();
        _hashedPass = SHA256.HashData(userPassBytes);
    }

    internal byte[] GenerateCipherKeyData(ReadOnlySpan<byte> salt, int keyLength)
    {
        return Rfc2898DeriveBytes.Pbkdf2(
            _hashedPass,
            salt,
            ITERATION_COUNT,
            HashAlgorithmName.SHA256,
            keyLength
        );
    }
}

class Aes256Cbc1(ILogger<Aes256Cbc1> logger)
{
    internal const int TAUTENED_BYTES = 4;
    internal const int RESERVED_BYTES = 4;
    internal const int RANDOM_BYTES = 12;
    internal const int VERIFY_KEY_BYTES = 4;

    internal const int CIPHER_TEXT_OFFSET =
        TAUTENED_BYTES + RESERVED_BYTES + RANDOM_BYTES + VERIFY_KEY_BYTES;
    internal const int CIPHER_BLOCK_BYTES = 16;
    internal const int CIPHER_KEY_BYTES = 32;

#pragma warning disable IDE0300 // Simplify collection initialization
    internal static readonly byte[] TAUTENED_DATA = new byte[TAUTENED_BYTES] { 0, 9, 9, 0xa1 };
    internal static readonly byte[] RESERVED_DATA = new byte[RESERVED_BYTES] { 0, 0, 0, 0 };
#pragma warning restore IDE0300 // Simplify collection initialization

    internal static CipherMode UsedCipherMode => CipherMode.CBC;
    internal static PaddingMode UsedPaddingMode => PaddingMode.PKCS7;

    [AllowNull]
    Aes _aes;

    [AllowNull]
    UserKeyBase _keyBase;

    readonly MemoryStream _buffer = new();

    void Init()
    {
        _aes = Aes.Create();

        _aes.Mode = UsedCipherMode;
        _aes.Padding = UsedPaddingMode;

        _keyBase = new();

        Debug.Assert(RANDOM_BYTES + VERIFY_KEY_BYTES == CIPHER_BLOCK_BYTES);

        logger.ZLogTrace(
            $"Initalize AES mode '{Enum.GetName(UsedCipherMode)}' padding '{Enum.GetName(UsedPaddingMode)}'"
        );
    }

    MemoryStream Encrypt(Stream inputStream, bool isBinary)
    {
        var outputStream = new MemoryStream();
        outputStream.Write(TAUTENED_DATA);
        outputStream.Write(RESERVED_DATA);

        Debug.Assert(RANDOM_BYTES + VERIFY_KEY_BYTES == CIPHER_BLOCK_BYTES);

        var ivData = new byte[CIPHER_BLOCK_BYTES];

        var randomData = new Span<byte>(ivData, 0, RANDOM_BYTES);
        RandomNumberGenerator.Fill(randomData);

        outputStream.Write(randomData);

        var verifyKeyData = new Span<byte>(ivData, RANDOM_BYTES, VERIFY_KEY_BYTES);
        var verifyKeyBytes = HMACSHA256.HashData(_keyBase.HashedPass, randomData, verifyKeyData);
        Debug.Assert(verifyKeyBytes == VERIFY_KEY_BYTES);

        outputStream.Write(verifyKeyData);

        _aes.Key = _keyBase.GenerateCipherKeyData(randomData, CIPHER_KEY_BYTES);
        _aes.IV = ivData;

        using var cryptoStream = new CryptoStream(
            inputStream,
            _aes.CreateEncryptor(),
            CryptoStreamMode.Read
        );

        var readBuf = new byte[CIPHER_BLOCK_BYTES];
        int readLen = 0;

        while ((readLen = cryptoStream.Read(readBuf)) > 0)
        {
            outputStream.Write(readBuf, 0, readLen);
        }

        return outputStream;
    }

    MemoryStream Decrypt(Stream inputStream, bool isBinary)
    {
        var outputStream = new MemoryStream();

        var tautenedData = new byte[TAUTENED_BYTES];
        var tautenedSize = inputStream.Read(tautenedData);
        if (tautenedSize != TAUTENED_BYTES)
        {
            throw new InvalidDataException($"Cannot read {nameof(TAUTENED_BYTES)}");
        }
        if (tautenedData.SequenceEqual(TAUTENED_DATA) == false)
        {
            throw new InvalidDataException($"Invalid {nameof(TAUTENED_DATA)}");
        }

        var reservedData = new byte[RESERVED_BYTES];
        var reservedSize = inputStream.Read(reservedData);
        if (reservedSize != RESERVED_BYTES)
        {
            throw new InvalidDataException($"Cannot read {nameof(RESERVED_BYTES)}");
        }

        var ivData = new byte[CIPHER_BLOCK_BYTES];

        var randomData = new Span<byte>(ivData, 0, RANDOM_BYTES);
        var randomSize = inputStream.Read(randomData);
        if (randomSize != RANDOM_BYTES)
        {
            throw new InvalidDataException($"Cannot read {nameof(RANDOM_BYTES)}");
        }

        var verifyKeyData = new Span<byte>(ivData, RANDOM_BYTES, VERIFY_KEY_BYTES);
        var verifyKeySize = inputStream.Read(verifyKeyData);
        if (verifyKeySize != VERIFY_KEY_BYTES)
        {
            throw new InvalidDataException($"Cannot read {nameof(VERIFY_KEY_BYTES)}");
        }

        var expectedVerifiKeyData = HMACSHA256.HashData(_keyBase.HashedPass, randomData);
        if (verifyKeyData.SequenceEqual(expectedVerifiKeyData) == false)
        {
            throw new InvalidDataException($"Failed to verify key");
        }

        _aes.Key = _keyBase.GenerateCipherKeyData(randomData, CIPHER_KEY_BYTES);
        _aes.IV = ivData;

        using var cryptoStream = new CryptoStream(
            inputStream,
            _aes.CreateDecryptor(),
            CryptoStreamMode.Read
        );

        var readBuf = new byte[CIPHER_BLOCK_BYTES];
        int readLen;

        while ((readLen = cryptoStream.Read(readBuf)) > 0)
        {
            outputStream.Write(readBuf, 0, readLen);
        }

        return outputStream;
    }
}
