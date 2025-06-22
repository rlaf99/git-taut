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

class InvalidTautenedDataException : Exception
{
    internal InvalidTautenedDataException(string? message)
        : base(message) { }

    internal bool HasTautenedBytes { get; set; }
    internal bool HasReservedBytes { get; set; }
}

partial class Aes256Cbc1(ILogger<Aes256Cbc1> logger)
{
    internal const int PLAIN_TEXT_MAX_BYTES = 100 * 1024 * 1024;
    internal const int PLAIN_TEXT_LENGTH_BYTES = 4;

    internal const int TAUTENED_BYTES = 4;
    internal const int RESERVED_BYTES = 4;
    internal const int RANDOM_BYTES = 12;
    internal const int KEY_TAG_BYTES = 4;

    internal const int CIPHER_TEXT_OFFSET =
        TAUTENED_BYTES + RESERVED_BYTES + RANDOM_BYTES + KEY_TAG_BYTES;
    internal const int CIPHER_BLOCK_BYTES = 16;
    internal const int CIPHER_KEY_BYTES = 32;

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

    internal int GetCipherTextLength(int intputSize) =>
        _aes.GetCiphertextLengthCbc(intputSize, _aes.Padding);
}

partial class Aes256Cbc1
{
    ICryptoTransform PrepareForEncryption(byte[] ivData)
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

    internal class EncryptorStream : Stream
    {
        readonly Aes256Cbc1 _cipher;
        readonly byte[] _ivData = new byte[CIPHER_BLOCK_BYTES];
        readonly ICryptoTransform _encryptTransform;
        readonly Stream _sourceStream;
        readonly bool _isBinary;
        readonly MemoryStream _headerStream;

        long _totalRead;

        internal EncryptorStream(
            Aes256Cbc1 cihper,
            Stream sourceInput,
            bool isBinary,
            ReadOnlySpan<byte> sourceExtraInfo
        )
        {
            _cipher = cihper;
            _isBinary = isBinary;
            _sourceStream = sourceInput;
            _headerStream = new();
            _encryptTransform = _cipher.PrepareForEncryption(_ivData);

            PrepareHeader(sourceExtraInfo);
        }

        void PrepareHeader(ReadOnlySpan<byte> sourceExtraInfo)
        {
            using var writer = new BinaryWriter(_headerStream, Encoding.UTF8, leaveOpen: true);

            writer.Write(_isBinary);

            ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(_sourceStream.Length, 0);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(
                _sourceStream.Length,
                PLAIN_TEXT_MAX_BYTES
            );

            var sourceLength = (int)_sourceStream.Length;
            var sourceLengthData = BitConverter.GetBytes(sourceLength);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(sourceLengthData);
            }

            writer.Write(sourceLengthData);

            ArgumentOutOfRangeException.ThrowIfGreaterThan(sourceExtraInfo.Length, byte.MaxValue);
            var extraInfoLength = (byte)sourceExtraInfo.Length;

            writer.Write(extraInfoLength);
            writer.Write(sourceExtraInfo);

            writer.Flush();

            _headerStream.Position = 0;
        }

        public override bool CanRead => _sourceStream.CanRead;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => _headerStream.Length + _sourceStream.Length;

        public override long Position
        {
            get => _totalRead;
            set => throw new NotSupportedException();
        }

        public override void Flush()
        {
            if (_totalRead > _headerStream.Length)
            {
                _sourceStream.Flush();
            }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int dataRead;

            if (_totalRead < _headerStream.Length)
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

        internal int GetOutputLength()
        {
            var cipherTextLength = _cipher.GetCipherTextLength((int)Length);
            var result = CIPHER_TEXT_OFFSET + cipherTextLength;

            return result;
        }

        internal void WriteToEnd(Stream outputStream)
        {
            outputStream.Write(TAUTENED_DATA);
            outputStream.Write(RESERVED_DATA);

            Debug.Assert(RANDOM_BYTES + KEY_TAG_BYTES == CIPHER_BLOCK_BYTES);

            outputStream.Write(_ivData);

            using var cryptoStream = new CryptoStream(
                this,
                _encryptTransform,
                CryptoStreamMode.Read
            );

            cryptoStream.CopyTo(outputStream);
        }

        internal void EncryptAdditional(Stream inputStream, Stream outputStream)
        {
            if (Position != Length)
            {
                throw new InvalidOperationException(
                    $"Should finish processing {nameof(EncryptorStream)} first"
                );
            }

            if (_encryptTransform.CanReuseTransform == false)
            {
                throw new InvalidOperationException(
                    $"{nameof(_encryptTransform)} cannot be reused"
                );
            }

            using var cryptoStream = new CryptoStream(
                inputStream,
                _encryptTransform,
                CryptoStreamMode.Read
            );

            cryptoStream.CopyTo(outputStream);
        }
    }

    internal class Encryptor
    {
        readonly EncryptorStream _encStream;

        internal Encryptor(
            Aes256Cbc1 cipher,
            Stream sourceInput,
            bool isBinary,
            ReadOnlySpan<byte> deltaBashHash
        )
        {
            cipher.EnsureInitialized();

            _encStream = new EncryptorStream(cipher, sourceInput, isBinary, deltaBashHash);
        }

        internal int GetOutputLength() => _encStream.GetOutputLength();

        internal void ProduceOutput(
            Stream outputStream,
            Stream? extraInput = null,
            Stream? extraOutput = null
        )
        {
            if (extraInput is null ^ extraOutput is null)
            {
                throw new ArgumentException(
                    $"{nameof(extraInput)} and {nameof(extraOutput)} must both be null or not null"
                );
            }

            _encStream.WriteToEnd(outputStream);

            if (extraInput is not null && extraOutput is not null)
            {
                _encStream.EncryptAdditional(extraInput, extraOutput);
            }
        }
    }

    internal Encryptor CreateEncryptor(Stream sourceInput, bool isBinary)
    {
        return CreateEncryptor(sourceInput, isBinary, []);
    }

    internal Encryptor CreateEncryptor(
        Stream sourceInput,
        bool isBinary,
        ReadOnlySpan<byte> soruceExtraInfo
    )
    {
        var encryptor = new Encryptor(this, sourceInput, isBinary, soruceExtraInfo);

        return encryptor;
    }
}

partial class Aes256Cbc1
{
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

    internal class DecryptorStream : Stream
    {
        readonly Aes256Cbc1 _cipher;
        readonly byte[] _ivData = new byte[CIPHER_BLOCK_BYTES];

        readonly Stream _sourceStream;

        [AllowNull]
        ICryptoTransform _decryptTransform;

        [AllowNull]
        CryptoStream _cryptoStream;

        [AllowNull]
        byte[] _extraPayload;

        bool _isBinary;

        int _outputLength;

        int _outputOffset;

        bool _headerExamined;
        bool _outputProduced;

        internal DecryptorStream(Aes256Cbc1 cipher, Stream sourceInput)
        {
            cipher.EnsureInitialized();

            _cipher = cipher;
            _sourceStream = sourceInput;
        }

        public override bool CanRead => _sourceStream.CanRead;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length
        {
            get
            {
                ExamineHeader();

                return _outputLength;
            }
        }

        public override long Position
        {
            get
            {
                ExamineHeader();

                return _cryptoStream.Position - _outputOffset;
            }
            set => throw new NotSupportedException();
        }

        public override void Flush()
        {
            if (_headerExamined)
            {
                _cryptoStream.Flush();
            }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            ExamineHeader();

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

        internal void WriteToEnd(Stream outputStream)
        {
            ExamineHeader();

            _cryptoStream.CopyTo(outputStream);

            _cryptoStream.Dispose();
            _cryptoStream = null;

            _outputProduced = true;
        }

        internal void DecryptAdditional(Stream inputStream, Stream outputStream)
        {
            if (_outputProduced == false)
            {
                throw new InvalidOperationException(
                    $"Should finish processing {nameof(DecryptorStream)} first"
                );
            }

            if (_decryptTransform.CanReuseTransform == false)
            {
                throw new InvalidOperationException(
                    $"{nameof(_decryptTransform)} cannot be reused"
                );
            }

            using var cryptoStream = new CryptoStream(
                inputStream,
                _decryptTransform,
                CryptoStreamMode.Read
            );

            cryptoStream.CopyTo(outputStream);
        }

        internal ReadOnlySpan<byte> ExtraPayload
        {
            get
            {
                ExamineHeader();

                return _extraPayload;
            }
        }

        internal bool IsBinary
        {
            get
            {
                ExamineHeader();

                return _isBinary;
            }
        }

        void ExamineHeader()
        {
            if (_headerExamined)
            {
                return;
            }

            _headerExamined = true;

            ExamineOverallHeader();
            ExamineContentHeader();
        }

        void ExamineOverallHeader()
        {
            var tautenedData = new byte[TAUTENED_BYTES];
            var tautenedSize = _sourceStream.Read(tautenedData);
            if (tautenedSize != TAUTENED_BYTES)
            {
                throw new InvalidTautenedDataException($"Cannot read {nameof(TAUTENED_BYTES)}");
            }
            if (tautenedData.SequenceEqual(TAUTENED_DATA) == false)
            {
                throw new InvalidTautenedDataException($"Invalid {nameof(TAUTENED_DATA)}");
            }

            var reservedData = new byte[RESERVED_BYTES];
            var reservedSize = _sourceStream.Read(reservedData);
            if (reservedSize != RESERVED_BYTES)
            {
                throw new InvalidDataException($"Cannot read {nameof(RESERVED_BYTES)}");
            }

            var randomData = new Span<byte>(_ivData, 0, RANDOM_BYTES);
            var randomSize = _sourceStream.Read(randomData);
            if (randomSize != RANDOM_BYTES)
            {
                throw new InvalidDataException($"Cannot read {nameof(RANDOM_BYTES)}");
            }

            var keyTagData = new Span<byte>(_ivData, RANDOM_BYTES, KEY_TAG_BYTES);
            var keyTagSize = _sourceStream.Read(keyTagData);
            if (keyTagSize != KEY_TAG_BYTES)
            {
                throw new InvalidDataException($"Cannot read {nameof(KEY_TAG_BYTES)}");
            }
        }

        void ExamineContentHeader()
        {
            _decryptTransform = _cipher.PrepareForDecryption(_ivData);

            _cryptoStream = new CryptoStream(
                _sourceStream,
                _decryptTransform,
                CryptoStreamMode.Read
            );

            _isBinary = _cryptoStream.ReadByte() != 0;
            _outputOffset += 1;

            const int lengthBytes = sizeof(int);
            var lengthData = new byte[lengthBytes];
            _cryptoStream.ReadExactly(lengthData, 0, lengthBytes);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(lengthData);
            }
            _outputLength = BitConverter.ToInt32(lengthData);
            _outputOffset += lengthBytes;

            var extraInfoLength = _cryptoStream.ReadByte();
            _outputOffset += 1;

            if (extraInfoLength > 0)
            {
                _extraPayload = new byte[extraInfoLength];
                _cryptoStream.ReadExactly(_extraPayload, 0, extraInfoLength);
                _outputOffset += extraInfoLength;
            }
        }
    }

    internal class Decryptor
    {
        readonly DecryptorStream _decStream;

        internal Decryptor(Aes256Cbc1 cipher, Stream inputStream)
        {
            cipher.EnsureInitialized();

            _decStream = new DecryptorStream(cipher, inputStream);
        }

        internal int GetOutputLength() => (int)_decStream.Length;

        internal ReadOnlySpan<byte> GetExtraPayload() => _decStream.ExtraPayload;

        internal bool IsContentBinary() => _decStream.IsBinary;

        internal void ProduceOutput(
            Stream outputStream,
            Stream? extraInput = null,
            Stream? extraOutput = null
        )
        {
            if (extraInput is null ^ extraOutput is null)
            {
                throw new ArgumentException(
                    $"{nameof(extraInput)} and {nameof(extraOutput)} must both be null or not null"
                );
            }

            _decStream.WriteToEnd(outputStream);

            if (extraInput is not null && extraOutput is not null)
            {
                _decStream.DecryptAdditional(extraInput, extraOutput);
            }
        }
    }

    internal Decryptor CreateDecryptor(Stream inputStream)
    {
        var decryptor = new Decryptor(this, inputStream);

        return decryptor;
    }
}
