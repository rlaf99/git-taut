using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using ZLogger;

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
                var userPassBytes = GetUserPasswordInBytes();
                _hashedPass = SHA256.HashData(userPassBytes);
            }

            return _hashedPass;
        }
    }

    byte[] GetUserPasswordInBytes()
    {
        return Encoding.UTF8.GetBytes("Hello!");
    }

    internal byte[] GenerateCipherKey(ReadOnlySpan<byte> salt, int keyLength, int iteration)
    {
        return Rfc2898DeriveBytes.Pbkdf2(
            _hashedPass,
            salt,
            iteration,
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
    internal const int KEY_TAG_BYTES = 4;

    internal const int CIPHER_TEXT_OFFSET =
        TAUTENED_BYTES + RESERVED_BYTES + RANDOM_BYTES + KEY_TAG_BYTES;
    internal const int CIPHER_BLOCK_BYTES = 16;
    internal const int CIPHER_KEY_BYTES = 32;
    internal const int PLAIN_TEXT_LENGTH_BYTES = 4;

    internal const int KEY_ITERATION_COUNT = 64007;

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

    bool _initialized = false;

    public void Init()
    {
        if (_initialized)
        {
            return;
        }

        _initialized = true;

        _aes = Aes.Create();

        _aes.Mode = UsedCipherMode;
        _aes.Padding = UsedPaddingMode;

        _keyBase = new();

        Debug.Assert(RANDOM_BYTES + KEY_TAG_BYTES == CIPHER_BLOCK_BYTES);

        logger.ZLogTrace(
            $"Initialize {nameof(Aes256Cbc1)} mode '{Enum.GetName(UsedCipherMode)}' padding '{Enum.GetName(UsedPaddingMode)}'"
        );
    }

    void EnsureInitialized()
    {
        if (_initialized == false)
        {
            throw new InvalidOperationException($"{nameof(Aes256Cbc1)} not initailized");
        }
    }

    internal int GetEncryptedLength(int intputSize)
    {
        var cipherTextSize = _aes.GetCiphertextLengthCbc(intputSize, _aes.Padding);
        var result = cipherTextSize + CIPHER_TEXT_OFFSET;

        return result;
    }

    internal ICryptoTransform PrepareForEncryption(byte[] ivData)
    {
        if (ivData.Length != CIPHER_BLOCK_BYTES)
        {
            throw new ArgumentException($"Length must be {CIPHER_BLOCK_BYTES}", nameof(ivData));
        }

        var randomData = new Span<byte>(ivData, 0, RANDOM_BYTES);
        RandomNumberGenerator.Fill(randomData);

        var keyTagData = new Span<byte>(ivData, RANDOM_BYTES, KEY_TAG_BYTES);
        HKDF.Expand(HashAlgorithmName.SHA256, _keyBase.HashedPass, keyTagData, randomData);

        _aes.Key = _keyBase.GenerateCipherKey(randomData, CIPHER_KEY_BYTES, KEY_ITERATION_COUNT);
        _aes.IV = ivData;

        return _aes.CreateEncryptor();
    }

    internal class EncryptorInputStream : Stream
    {
        readonly Stream _sourceInput;
        long _totalRead;
        readonly byte[] _lengthData;
        int _lengthDataRead;

        internal EncryptorInputStream(Stream sourceInput)
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(sourceInput.Length, 0);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(sourceInput.Length, int.MaxValue);

            _sourceInput = sourceInput;

            var length = (int)sourceInput.Length;
            _lengthData = BitConverter.GetBytes(length);
            Debug.Assert(_lengthData.Length == PLAIN_TEXT_LENGTH_BYTES);

            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(_lengthData);
            }
        }

        public override bool CanRead => _sourceInput.CanRead;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => _lengthData.Length + _sourceInput.Length;

        public override long Position
        {
            get => _totalRead;
            set => throw new NotSupportedException();
        }

        public override void Flush()
        {
            if (_totalRead > _lengthData.Length)
            {
                _sourceInput.Flush();
            }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int dataRead;

            if (_lengthDataRead != _lengthData.Length)
            {
                var target = buffer.AsSpan(offset, count);

                dataRead = 0;
                while (_lengthDataRead < _lengthData.Length && dataRead < target.Length)
                {
                    target[dataRead++] = _lengthData[_lengthDataRead++];
                }
            }
            else
            {
                dataRead = _sourceInput.Read(buffer, offset, count);
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

    internal class Encryptor : IDisposable
    {
        readonly Aes256Cbc1 _cipher;
        readonly Stream _inputStream;
        readonly bool _isBinary;

        internal Encryptor(Aes256Cbc1 cipher, Stream inputStream, bool isBinary)
        {
            cipher.EnsureInitialized();

            _cipher = cipher;
            _inputStream = inputStream;
            _isBinary = isBinary;
        }

        internal int GetOutputLength()
        {
            return _cipher.GetEncryptedLength((int)_inputStream.Length);
        }

        internal void WriteToEnd(Stream outputStream)
        {
            outputStream.Write(TAUTENED_DATA);
            outputStream.Write(RESERVED_DATA);

            Debug.Assert(RANDOM_BYTES + KEY_TAG_BYTES == CIPHER_BLOCK_BYTES);

            var ivData = new byte[CIPHER_BLOCK_BYTES];
            var encryptTransform = _cipher.PrepareForEncryption(ivData);

            outputStream.Write(ivData);

            using var cryptoStream = new CryptoStream(
                _inputStream,
                encryptTransform,
                CryptoStreamMode.Read
            );

            var readBuf = new byte[CIPHER_BLOCK_BYTES];
            int readLen = 0;

            while ((readLen = cryptoStream.Read(readBuf)) > 0)
            {
                outputStream.Write(readBuf, 0, readLen);
            }
        }

        bool _isDisposed;

        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }
            _isDisposed = true;
        }
    }

    internal Encryptor CreateEncryptor(Stream inputStream, bool isBinary)
    {
        var encryptorInput = new EncryptorInputStream(inputStream);
        var encryptor = new Encryptor(this, encryptorInput, isBinary);

        return encryptor;
    }

    ICryptoTransform PrepareForDecryption(byte[] ivData)
    {
        if (ivData.Length != CIPHER_BLOCK_BYTES)
        {
            throw new ArgumentException($"Length must be {CIPHER_BLOCK_BYTES}", nameof(ivData));
        }

        var randomData = new ReadOnlySpan<byte>(ivData, 0, RANDOM_BYTES);
        var keyTagData = new ReadOnlySpan<byte>(ivData, RANDOM_BYTES, KEY_TAG_BYTES);

        var expectedKeyTagData = new byte[KEY_TAG_BYTES];
        HKDF.Expand(HashAlgorithmName.SHA256, _keyBase.HashedPass, expectedKeyTagData, randomData);

        if (keyTagData.SequenceEqual(expectedKeyTagData) == false)
        {
            throw new InvalidDataException($"Failed to verify key");
        }

        _aes.Key = _keyBase.GenerateCipherKey(randomData, CIPHER_KEY_BYTES, KEY_ITERATION_COUNT);
        _aes.IV = ivData;

        return _aes.CreateDecryptor();
    }

    internal class DecryptorInputStream : Stream
    {
        readonly Aes256Cbc1 _cipher;

        readonly Stream _sourceInput;

        [AllowNull]
        CryptoStream _cryptoStream;

        int _plainTextLength;

        bool _headerVerified;

        internal DecryptorInputStream(Aes256Cbc1 cipher, Stream sourceInput)
        {
            cipher.EnsureInitialized();

            if (sourceInput.Length <= PLAIN_TEXT_LENGTH_BYTES)
            {
                throw new ArgumentException($"Invalid input", nameof(sourceInput));
            }

            _cipher = cipher;
            _sourceInput = sourceInput;
        }

        public override bool CanRead => _sourceInput.CanRead;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length
        {
            get
            {
                VerifyHeader();

                return _plainTextLength;
            }
        }

        public override long Position
        {
            get
            {
                VerifyHeader();

                return _cryptoStream.Position - PLAIN_TEXT_LENGTH_BYTES;
            }
            set => throw new NotSupportedException();
        }

        public override void Flush()
        {
            VerifyHeader();

            _cryptoStream.Flush();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            VerifyHeader();

            return _cryptoStream.Read(buffer, offset, count);
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

        void VerifyHeader()
        {
            if (_headerVerified)
            {
                return;
            }

            _headerVerified = true;

            var tautenedData = new byte[TAUTENED_BYTES];
            var tautenedSize = _sourceInput.Read(tautenedData);
            if (tautenedSize != TAUTENED_BYTES)
            {
                throw new InvalidDataException($"Cannot read {nameof(TAUTENED_BYTES)}");
            }
            if (tautenedData.SequenceEqual(TAUTENED_DATA) == false)
            {
                throw new InvalidDataException($"Invalid {nameof(TAUTENED_DATA)}");
            }

            var reservedData = new byte[RESERVED_BYTES];
            var reservedSize = _sourceInput.Read(reservedData);
            if (reservedSize != RESERVED_BYTES)
            {
                throw new InvalidDataException($"Cannot read {nameof(RESERVED_BYTES)}");
            }

            var ivData = new byte[CIPHER_BLOCK_BYTES];

            var randomData = new Span<byte>(ivData, 0, RANDOM_BYTES);
            var randomSize = _sourceInput.Read(randomData);
            if (randomSize != RANDOM_BYTES)
            {
                throw new InvalidDataException($"Cannot read {nameof(RANDOM_BYTES)}");
            }

            var keyTagData = new Span<byte>(ivData, RANDOM_BYTES, KEY_TAG_BYTES);
            var keyTagSize = _sourceInput.Read(keyTagData);
            if (keyTagSize != KEY_TAG_BYTES)
            {
                throw new InvalidDataException($"Cannot read {nameof(KEY_TAG_BYTES)}");
            }

            var decryptTransform = _cipher.PrepareForDecryption(ivData);

            _cryptoStream = new CryptoStream(_sourceInput, decryptTransform, CryptoStreamMode.Read);

            var lengthData = new byte[PLAIN_TEXT_LENGTH_BYTES];

            var lengthDataSize = _cryptoStream.Read(lengthData);
            if (lengthDataSize != PLAIN_TEXT_LENGTH_BYTES)
            {
                throw new InvalidDataException($"Cannot read {nameof(PLAIN_TEXT_LENGTH_BYTES)}");
            }

            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(lengthData);
            }

            _plainTextLength = BitConverter.ToInt32(lengthData);
        }
    }

    internal class Decryptor
    {
        readonly Aes256Cbc1 _cipher;
        readonly Stream _inputStream;
        readonly bool _isBinary;

        internal Decryptor(Aes256Cbc1 cipher, Stream inputStream, bool isBinary)
        {
            cipher.EnsureInitialized();

            _cipher = cipher;
            _inputStream = inputStream;
            _isBinary = isBinary;
        }

        internal int GetOutputLength()
        {
            return (int)_inputStream.Length;
        }

        internal void WriteToEnd(Stream outputStream)
        {
            var readBuf = new byte[CIPHER_BLOCK_BYTES];
            int readLen;

            while ((readLen = _inputStream.Read(readBuf)) > 0)
            {
                outputStream.Write(readBuf, 0, readLen);
            }
        }
    }

    internal Decryptor CreateDecryptor(Stream inputStream, bool isBinary)
    {
        var decryptorInput = new DecryptorInputStream(this, inputStream);
        var decryptor = new Decryptor(this, decryptorInput, isBinary);

        return decryptor;
    }
}
