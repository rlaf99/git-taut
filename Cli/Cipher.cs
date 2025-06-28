using System.Buffers;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.IO;
using ZLogger;
using ZstdSharp;

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

partial class Aes256Cbc1(ILogger<Aes256Cbc1> logger, RecyclableMemoryStreamManager streamManager);

partial class Aes256Cbc1
{
    internal const int PLAIN_TEXT_MAX_BYTES = 100 * 1024 * 1024;

    internal const int TAUTENED_BYTES = 4;
    internal const int RESERVED_BYTES = 4;
    internal const int RANDOM_BYTES = 12;
    internal const int KEY_TAG_BYTES = 4;

    internal const int CIPHER_TEXT_OFFSET =
        TAUTENED_BYTES + RESERVED_BYTES + RANDOM_BYTES + KEY_TAG_BYTES;
    internal const int CIPHER_BLOCK_BYTES = 16;
    internal const int CIPHER_KEY_BYTES = 32;

    internal const int KEY_ITERATION_COUNT = 64007;

    internal const int PLAIN_TEXT_SCRAMBLE_BYTES = 4;

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

    internal void EnsureInitialized()
    {
        if (_initialized == false)
        {
            throw new InvalidOperationException($"{nameof(Aes256Cbc1)} not initailized");
        }
    }

    internal int GetCipherTextLength(int intputSize)
    {
        EnsureInitialized();

        var result = _aes.GetCiphertextLengthCbc(intputSize, _aes.Padding);

        return result;
    }
}

partial class Aes256Cbc1
{
    byte[] PrepareEncryptionKey(byte[] ivData)
    {
        if (ivData.Length != CIPHER_BLOCK_BYTES)
        {
            throw new ArgumentException($"Length must be {CIPHER_BLOCK_BYTES}", nameof(ivData));
        }

        var randomData = new Span<byte>(ivData, 0, RANDOM_BYTES);
        RandomNumberGenerator.Fill(randomData);

        var keyTagData = new Span<byte>(ivData, RANDOM_BYTES, KEY_TAG_BYTES);
        HKDF.Expand(HashAlgorithmName.SHA256, _keyBase.HashedPass, keyTagData, randomData);

        var key = _keyBase.GenerateCipherKey(randomData, CIPHER_KEY_BYTES, KEY_ITERATION_COUNT);

        return key;
    }

    ICryptoTransform GetEncryptionTransform(byte[] encKey, byte[] ivData)
    {
        _aes.Key = encKey;
        _aes.IV = ivData;

        return _aes.CreateEncryptor();
    }

    [Flags]
    internal enum ContentHeaderPrimaryFlags : byte
    {
        None = 0,
        ContentIsCompressed = 1 << 0,
        ExtraPayloadPresent = 1 << 1,
    }

    internal sealed class EncryptorStream : Stream
    {
        readonly Aes256Cbc1 _cipher;
        readonly byte[] _ivData = new byte[CIPHER_BLOCK_BYTES];
        readonly byte[] _encKey;

        readonly MemoryStream _headerStream = new();
        readonly Stream _sourceStream;
        readonly bool _leaveOpen;
        readonly bool _isCompressed;
        readonly int _sourceLength;
        readonly long _length;

        long _totalRead;

        internal EncryptorStream(
            Aes256Cbc1 cihper,
            Stream sourceInput,
            bool isCompressed,
            int sourceLength,
            ReadOnlySpan<byte> extraPayload,
            bool leaveOpen = false
        )
        {
            _cipher = cihper;
            _encKey = _cipher.PrepareEncryptionKey(_ivData);

            _sourceStream = sourceInput;
            _sourceLength = sourceLength;
            _isCompressed = isCompressed;

            _leaveOpen = leaveOpen;

            PrepareHeaderStream(extraPayload);
            _headerStream.Position = 0;
            _length = _headerStream.Length + _sourceStream.Length - _sourceStream.Position;
        }

        void PrepareHeaderStream(ReadOnlySpan<byte> extraPayload)
        {
            var primaryFlags = ContentHeaderPrimaryFlags.None;

            if (_isCompressed)
            {
                primaryFlags |= ContentHeaderPrimaryFlags.ContentIsCompressed;
            }

            if (extraPayload.Length > 0)
            {
                primaryFlags |= ContentHeaderPrimaryFlags.ExtraPayloadPresent;
            }

            var scrambleData = new byte[PLAIN_TEXT_SCRAMBLE_BYTES];
            RandomNumberGenerator.Fill(scrambleData);
            scrambleData[0] = (byte)primaryFlags;

            _headerStream.Write(scrambleData);

            ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(_sourceLength, 0);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(_sourceLength, PLAIN_TEXT_MAX_BYTES);

            var sourceLengthData = BitConverter.GetBytes(_sourceLength);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(sourceLengthData);
            }

            _headerStream.Write(sourceLengthData);

            ArgumentOutOfRangeException.ThrowIfGreaterThan(extraPayload.Length, byte.MaxValue);
            var extraPayloadLength = (byte)extraPayload.Length;

            if (extraPayload.Length > 0)
            {
                _headerStream.WriteByte(extraPayloadLength);
                _headerStream.Write(extraPayload);
            }

            var sourceBeginningData = new byte[TAUTENED_BYTES];
            var sourceBeginningSize = _sourceStream.Read(sourceBeginningData);
            if (
                sourceBeginningSize == TAUTENED_BYTES
                && sourceBeginningData.SequenceEqual(TAUTENED_DATA)
            )
            {
                throw new InvalidDataException($"{nameof(TAUTENED_DATA)} found in source");
            }

            _headerStream.Write(sourceBeginningData, 0, sourceBeginningSize);
        }

        public override bool CanRead => _sourceStream.CanRead;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => _length;

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

        internal bool IsCompressed => _isCompressed;

        internal int GetOutputLength()
        {
            var cipherTextLength = _cipher.GetCipherTextLength((int)_length);
            var result = CIPHER_TEXT_OFFSET + cipherTextLength;

            return result;
        }

        internal void WriteToEnd(Stream outputStream)
        {
            outputStream.Write(TAUTENED_DATA);
            outputStream.Write(RESERVED_DATA);

            outputStream.Write(_ivData);

            var encryptTransform = _cipher.GetEncryptionTransform(_encKey, _ivData);

            using var cryptoStream = new CryptoStream(
                this,
                encryptTransform,
                CryptoStreamMode.Read,
                leaveOpen: true
            );

            cryptoStream.CopyTo(outputStream);
        }

        internal void EncryptExternal(Stream input, Stream output)
        {
            var encryptTransform = _cipher.GetEncryptionTransform(_encKey, _ivData);

            using var cryptoStream = new CryptoStream(
                input,
                encryptTransform,
                CryptoStreamMode.Read,
                leaveOpen: true
            );

            cryptoStream.CopyTo(output);
        }

        bool _isDisposed;

        protected override void Dispose(bool dispoing)
        {
            if (_isDisposed)
            {
                return;
            }
            _isDisposed = true;

            if (dispoing)
            {
                Array.Fill<byte>(_encKey, 0);

                _headerStream.Dispose();

                if (_leaveOpen == false)
                {
                    _sourceStream.Dispose();
                }
            }

            base.Dispose(dispoing);
        }
    }

    internal sealed class Encryptor : IDisposable
    {
        readonly EncryptorStream _encStream;

        readonly long _inputLength;

        internal Encryptor(
            Aes256Cbc1 cipher,
            Stream sourceInput,
            bool isCompressed,
            int sourceLength,
            ReadOnlySpan<byte> extraPayload,
            bool leaveOpen
        )
        {
            cipher.EnsureInitialized();

            _encStream = new EncryptorStream(
                cipher,
                sourceInput,
                isCompressed,
                sourceLength,
                extraPayload,
                leaveOpen
            );

            _inputLength = sourceInput.Length - sourceInput.Position;
        }

        internal bool IsCompressed => _encStream.IsCompressed;

        internal int GetOutputLength() => _encStream.GetOutputLength();

        internal int GetInputLength() => (int)_inputLength;

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
                _encStream.EncryptExternal(extraInput, extraOutput);
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

            _encStream.Dispose();
        }
    }

    internal Encryptor CreateEncryptor(
        Stream sourceInput,
        double compressionMaxRatio = 0.0,
        bool leaveOpen = false
    )
    {
        return CreateEncryptor(sourceInput, compressionMaxRatio, [], leaveOpen);
    }

    internal Encryptor CreateEncryptor(
        Stream sourceInput,
        double compressionMaxRatio,
        ReadOnlySpan<byte> extraPayload,
        bool leaveOpen = false
    )
    {
        var sourceLength = sourceInput.Length - sourceInput.Position;

        ArgumentOutOfRangeException.ThrowIfGreaterThan(sourceLength, PLAIN_TEXT_MAX_BYTES);

        var isCompressed = false;
        var inputStream = sourceInput;

        if (compressionMaxRatio > 0.0)
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(compressionMaxRatio, 0.1);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(compressionMaxRatio, 0.9);

            var compressedBufferSize = (int)(sourceInput.Length * compressionMaxRatio);
            var compressedStream = streamManager.GetStream(
                null,
                requiredSize: compressedBufferSize
            );

            bool SizedCompress()
            {
                using var encompressionStream = new CompressionStream(
                    compressedStream,
                    bufferSize: 512,
                    leaveOpen: true
                );

                var buffer = ArrayPool<byte>.Shared.Rent(1024);
                try
                {
                    var startPosition = compressedStream.Position;
                    for (; ; )
                    {
                        var dataRead = sourceInput.Read(buffer, 0, buffer.Length);
                        if (dataRead == 0)
                        {
                            break;
                        }

                        encompressionStream.Write(buffer, 0, dataRead);

                        if (compressedStream.Position - startPosition > compressedBufferSize)
                        {
                            return false;
                        }
                    }

                    encompressionStream.Flush();

                    if (compressedStream.Position - startPosition > compressedBufferSize)
                    {
                        return false;
                    }
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(buffer);
                }

                return true;
            }

            isCompressed = SizedCompress();

            if (isCompressed)
            {
                var compressedLength = compressedStream.Position;

                compressedStream.Position = 0;
                compressedStream.SetLength(compressedLength);

                if (leaveOpen == false)
                {
                    inputStream.Dispose();
                }
                leaveOpen = false;

                inputStream = compressedStream;

                logger.ZLogTrace(
                    $"Compress encryptor input from {sourceInput.Length} to {compressedLength}"
                );
            }
        }

        var encryptor = new Encryptor(
            this,
            inputStream,
            isCompressed,
            (int)sourceLength,
            extraPayload,
            leaveOpen
        );

        return encryptor;
    }
}

partial class Aes256Cbc1
{
    byte[] GetDecryptionKey(byte[] ivData)
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

        var key = _keyBase.GenerateCipherKey(randomData, CIPHER_KEY_BYTES, KEY_ITERATION_COUNT);

        return key;
    }

    ICryptoTransform GetDecryptionTransform(byte[] decKey, byte[] ivData)
    {
        _aes.Key = decKey;
        _aes.IV = ivData;

        return _aes.CreateDecryptor();
    }

    internal class DecryptorStream : Stream
    {
        readonly Aes256Cbc1 _cipher;
        readonly byte[] _ivData = new byte[CIPHER_BLOCK_BYTES];

        readonly Stream _sourceStream;
        readonly bool _leaveOpen;

        [AllowNull]
        byte[] _decKey;

        [AllowNull]
        CryptoStream _cryptoStream;

        [AllowNull]
        byte[] _extraPayload;

        bool _isCompressed;
        int _outputLength;
        int _outputOffset;

        bool _overallHeaderExamined;
        bool _contentHeaderExamined;

        bool HeaderExamined => _overallHeaderExamined && _contentHeaderExamined;

        bool _outputProduced;

        internal DecryptorStream(Aes256Cbc1 cipher, Stream sourceInput, bool leaveOpen = false)
        {
            cipher.EnsureInitialized();

            _cipher = cipher;
            _sourceStream = sourceInput;
            _leaveOpen = leaveOpen;
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
            if (HeaderExamined)
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
            if (_outputProduced)
            {
                throw new InvalidOperationException($"The output has already been produced");
            }
            _outputProduced = true;

            ExamineHeader();

            if (_isCompressed)
            {
                using var decompressionStream = new DecompressionStream(
                    _cryptoStream,
                    leaveOpen: true
                );

                decompressionStream.CopyTo(outputStream);
            }
            else
            {
                _cryptoStream.CopyTo(outputStream);
            }

            _cryptoStream.Dispose();
            _cryptoStream = null;
        }

        internal void EncryptExternal(Stream input, Stream output)
        {
            ExamineOverallHeader();

            var encryptTransform = _cipher.GetEncryptionTransform(_decKey!, _ivData);

            using var cryptoStream = new CryptoStream(
                input,
                encryptTransform,
                CryptoStreamMode.Read,
                leaveOpen: true
            );

            cryptoStream.CopyTo(output);
        }

        internal void DecryptExternal(Stream input, Stream output)
        {
            ExamineOverallHeader();

            var decryptTransform = _cipher.GetDecryptionTransform(_decKey!, _ivData);

            using var cryptoStream = new CryptoStream(
                input,
                decryptTransform,
                CryptoStreamMode.Read,
                leaveOpen: true
            );

            cryptoStream.CopyTo(output);
        }

        internal ReadOnlySpan<byte> ExtraPayload
        {
            get
            {
                ExamineHeader();

                return _extraPayload;
            }
        }

        internal bool IsCompressed
        {
            get
            {
                ExamineHeader();

                return _isCompressed;
            }
        }

        void ExamineHeader()
        {
            if (HeaderExamined)
            {
                return;
            }

            ExamineOverallHeader();
            ExamineContentHeader();
        }

        internal void ExamineOverallHeader()
        {
            if (_overallHeaderExamined)
            {
                return;
            }
            _overallHeaderExamined = true;

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

            _decKey = _cipher.GetDecryptionKey(_ivData);
        }

        internal void ExamineContentHeader()
        {
            if (_contentHeaderExamined)
            {
                return;
            }
            _contentHeaderExamined = true;

            var decryptTransform = _cipher.GetDecryptionTransform(_decKey!, _ivData);

            _cryptoStream = new CryptoStream(
                _sourceStream,
                decryptTransform,
                CryptoStreamMode.Read
            );

            var scrambleData = new byte[PLAIN_TEXT_SCRAMBLE_BYTES];
            _cryptoStream.ReadExactly(scrambleData);
            _outputOffset += PLAIN_TEXT_SCRAMBLE_BYTES;
            var primaryFlags = (ContentHeaderPrimaryFlags)scrambleData[0];

            if (primaryFlags.HasFlag(ContentHeaderPrimaryFlags.ContentIsCompressed))
            {
                _isCompressed = true;
            }

            const int lengthBytes = sizeof(int);
            var lengthData = new byte[lengthBytes];
            _cryptoStream.ReadExactly(lengthData, 0, lengthBytes);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(lengthData);
            }
            _outputLength = BitConverter.ToInt32(lengthData);
            _outputOffset += lengthBytes;

            if (primaryFlags.HasFlag(ContentHeaderPrimaryFlags.ExtraPayloadPresent))
            {
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

        bool _isDisposed;

        protected override void Dispose(bool disposing)
        {
            if (_isDisposed)
            {
                return;
            }
            _isDisposed = true;

            if (disposing)
            {
                if (_decKey is not null)
                {
                    Array.Fill<byte>(_decKey, 0);
                }
                if (_leaveOpen == false)
                {
                    _sourceStream.Dispose();
                }
                _cryptoStream?.Dispose();
                _cryptoStream = null;
            }

            base.Dispose(disposing);
        }
    }

    internal sealed class Decryptor : IDisposable
    {
        readonly DecryptorStream _decStream;

        internal Decryptor(Aes256Cbc1 cipher, Stream inputStream, bool leaveOpen)
        {
            cipher.EnsureInitialized();

            _decStream = new DecryptorStream(cipher, inputStream, leaveOpen);
        }

        internal int GetOutputLength() => (int)_decStream.Length;

        internal ReadOnlySpan<byte> GetExtraPayload() => _decStream.ExtraPayload;

        internal bool IsCompressed => _decStream.IsCompressed;

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
                _decStream.DecryptExternal(extraInput, extraOutput);
            }
        }

        bool _isDiposed;

        public void Dispose()
        {
            if (_isDiposed)
            {
                return;
            }
            _isDiposed = true;

            _decStream.Dispose();
        }
    }

    internal Decryptor CreateDecryptor(Stream inputStream, bool leaveOpen = false)
    {
        var decryptor = new Decryptor(this, inputStream, leaveOpen);

        return decryptor;
    }

    internal sealed class Recryptor : IDisposable
    {
        readonly DecryptorStream _decStream;

        internal Recryptor(Aes256Cbc1 cipher, Stream inputStream)
        {
            cipher.EnsureInitialized();

            _decStream = new DecryptorStream(cipher, inputStream, leaveOpen: true);
            _decStream.ExamineOverallHeader();
        }

        internal void Encrypt(Stream input, Stream output)
        {
            _decStream.EncryptExternal(input, output);
        }

        internal void Decrypt(Stream input, Stream output)
        {
            _decStream.DecryptExternal(input, output);
        }

        bool _isDiposed;

        public void Dispose()
        {
            if (_isDiposed)
            {
                return;
            }
            _isDiposed = true;

            _decStream.Dispose();
        }
    }

    internal Recryptor CreateRecryptor(Stream inputStream)
    {
        var recryptor = new Recryptor(this, inputStream);

        return recryptor;
    }
}
