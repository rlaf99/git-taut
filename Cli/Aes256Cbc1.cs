using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using Microsoft.Extensions.Logging;
using Microsoft.IO;
using ZLogger;
using ZstdSharp;
using static Git.Taut.GitAttrConstants;

namespace Git.Taut;

class InvalidHallmarkException : FormatException
{
    internal InvalidHallmarkException(string? message)
        : base(message) { }

    internal bool HasHallmarkBytes { get; set; }
    internal bool HasReservedBytes { get; set; }
}

partial class Aes256Cbc1(ILogger<Aes256Cbc1> logger, RecyclableMemoryStreamManager streamManager);

partial class Aes256Cbc1
{
    internal const int PLAIN_TEXT_MAX_SIZE = 100 * 1024 * 1024;

    internal const int HALLMARK_SIZE = 4;
    internal const int RESERVED_SIZE = 4;
    internal const int RANDOM_FILL_SIZE = 20;
    internal const int RANDOM_FILL_SALT_OFFSET = 4;

    internal const int NAME_HASH_MIN_SIZE = 20;
    internal const int NAME_HASH_SALT_OFFSET = 4;
    internal const int NAME_HEAD_CRC_SIZE = 1;
    internal const int NAME_TAIL_CRC_SIZE = 1;
    internal const int NAME_CRC_SIZE = NAME_HEAD_CRC_SIZE + NAME_TAIL_CRC_SIZE;

    internal const int CIPHER_TEXT_OFFSET = HALLMARK_SIZE + RESERVED_SIZE + RANDOM_FILL_SIZE;
    internal const int CIPHER_BLOCK_SIZE = 16;
    internal const int PLAIN_TEXT_SCRAMBLE_SIZE = 4;

#pragma warning disable IDE0300 // Simplify collection initialization
    internal static readonly byte[] HALLMARK_DATA = new byte[HALLMARK_SIZE] { 0, 9, 9, 0xa1 };
    internal static readonly byte[] RESERVED_DATA = new byte[RESERVED_SIZE] { 0, 0, 0, 0 };
#pragma warning restore IDE0300 // Simplify collection initialization

    static readonly byte _hallmarkDataCrc = Crc8.Compute(HALLMARK_DATA);

    internal static CipherMode UsedCipherMode => CipherMode.CBC;
    internal static PaddingMode UsedPaddingMode => PaddingMode.PKCS7;

    [AllowNull]
    Aes _aes;

    [AllowNull]
    UserKeyHolder _keyHolder;

    internal UserKeyHolder KeyHolder => _keyHolder!;

    bool _initialized = false;

    public void Init(UserKeyHolder keyHolder)
    {
        if (keyHolder.CrudeKeyIsNull)
        {
            throw new ArgumentException($"{nameof(keyHolder.CrudeKeyIsNull)}", nameof(keyHolder));
        }

        ThrowHelper.InvalidOperationIfAlreadyInitalized(_initialized);

        _initialized = true;

        _keyHolder = keyHolder;

        _aes = Aes.Create();

        _aes.Mode = UsedCipherMode;
        _aes.Padding = UsedPaddingMode;

        logger.ZLogTrace($"Initialized {nameof(Aes256Cbc1)}");
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

    internal static byte GetFirstNonZero(ReadOnlySpan<byte> data)
    {
        foreach (var aByte in data)
        {
            if (aByte != 0)
            {
                return aByte;
            }
        }

        throw new InvalidOperationException($"All data are zero");
    }

    #region  Encryption

    internal MemoryStream EncryptName(byte[] nameData, ReadOnlySpan<byte> hash)
    {
        ArgumentOutOfRangeException.ThrowIfZero(nameData.Length, nameof(nameData));
        ArgumentOutOfRangeException.ThrowIfLessThan(hash.Length, NAME_HASH_MIN_SIZE, nameof(hash));

        var ivData = hash[0..CIPHER_BLOCK_SIZE].ToArray();

        int encKeyIterationCount = GetFirstNonZero(hash);
        var encKeySalt = hash[NAME_HASH_SALT_OFFSET..NAME_HASH_MIN_SIZE];
        var encKey = KeyHolder.DeriveCipherKey(encKeySalt, encKeyIterationCount);

        var encryptTransform = GetEncryptionTransform(encKey, ivData);
        var nameDataStream = new MemoryStream(nameData, writable: false);

        MemoryStream result = new();

        result.WriteByte(_hallmarkDataCrc);

        using (
            var cryptoStream = new CryptoStream(
                nameDataStream,
                encryptTransform,
                CryptoStreamMode.Read
            )
        )
        {
            cryptoStream.CopyTo(result);
        }

        var encData = result
            .GetBuffer()
            .AsSpan(NAME_HEAD_CRC_SIZE, (int)result.Length - NAME_HEAD_CRC_SIZE);

        var encDataCrc = Crc8.Compute(encData, _hallmarkDataCrc);

        result.WriteByte(encDataCrc);

        return result;
    }

    byte[] PrepareEncryptionKey(byte[] ivData, byte[] extraSalt)
    {
        ArgumentOutOfRangeException.ThrowIfNotEqual(
            ivData.Length,
            CIPHER_BLOCK_SIZE,
            nameof(ivData)
        );

        ArgumentOutOfRangeException.ThrowIfNotEqual(
            extraSalt.Length,
            RANDOM_FILL_SIZE - CIPHER_BLOCK_SIZE,
            nameof(extraSalt)
        );

        var randomFill = RandomNumberGenerator.GetBytes(20);

        randomFill.AsSpan(0, CIPHER_BLOCK_SIZE).CopyTo(ivData);
        randomFill.AsSpan(CIPHER_BLOCK_SIZE).CopyTo(extraSalt);

        int encKeyIterationCount = GetFirstNonZero(randomFill);
        var encKeySalt = randomFill.AsSpan(RANDOM_FILL_SALT_OFFSET);

        var result = KeyHolder.DeriveCipherKey(encKeySalt, encKeyIterationCount);

        return result;
    }

    ICryptoTransform GetEncryptionTransform(byte[] encKey, byte[] ivData) =>
        _aes.CreateEncryptor(encKey, ivData);

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
        readonly byte[] _ivData = new byte[CIPHER_BLOCK_SIZE];
        readonly byte[] _extraSalt = new byte[RANDOM_FILL_SIZE - CIPHER_BLOCK_SIZE];
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
            Stream inputStream,
            bool isCompressed,
            int sourceLength,
            ReadOnlySpan<byte> extraPayload,
            bool leaveOpen = false
        )
        {
            _cipher = cihper;
            _encKey = _cipher.PrepareEncryptionKey(_ivData, _extraSalt);

            _sourceStream = inputStream;
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

            var scrambleData = RandomNumberGenerator.GetBytes(PLAIN_TEXT_SCRAMBLE_SIZE);
            scrambleData[0] = (byte)primaryFlags;

            _headerStream.Write(scrambleData);

            ArgumentOutOfRangeException.ThrowIfLessThan(_sourceLength, 0);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(_sourceLength, PLAIN_TEXT_MAX_SIZE);

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

            var sourceBeginningData = new byte[HALLMARK_SIZE];
            var sourceBeginningSize = _sourceStream.Read(sourceBeginningData);
            if (
                sourceBeginningSize == HALLMARK_SIZE
                && sourceBeginningData.SequenceEqual(HALLMARK_DATA)
            )
            {
                throw new InvalidDataException($"{nameof(HALLMARK_DATA)} found in source");
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
            throw new NotSupportedException();
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
            outputStream.Write(HALLMARK_DATA);
            outputStream.Write(RESERVED_DATA);

            outputStream.Write(_ivData);
            outputStream.Write(_extraSalt);

            var encTransform = _cipher.GetEncryptionTransform(_encKey, _ivData);

            using var cryptoStream = new CryptoStream(
                this,
                encTransform,
                CryptoStreamMode.Read,
                leaveOpen: true
            );

            cryptoStream.CopyTo(outputStream);

            if (_leaveOpen == false)
            {
                _sourceStream.Dispose();
            }
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

        bool _disposed;

        protected override void Dispose(bool dispoing)
        {
            if (_disposed)
            {
                return;
            }
            _disposed = true;

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
            Stream inputStream,
            bool isCompressed,
            int sourceLength,
            ReadOnlySpan<byte> extraPayload,
            bool leaveOpen
        )
        {
            cipher.EnsureInitialized();

            _inputLength = inputStream.Length - inputStream.Position;

            _encStream = new EncryptorStream(
                cipher,
                inputStream,
                isCompressed,
                sourceLength,
                extraPayload,
                leaveOpen
            );
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

        bool _disposed;

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }
            _disposed = true;

            _encStream.Dispose();
        }
    }

    internal Encryptor CreateEncryptor(
        Stream sourceInput,
        double compressionTargetRatio = COMPRESSION_TARGET_RATIO_DISABLED_VALUE,
        bool leaveOpen = false
    )
    {
        return CreateEncryptor(sourceInput, compressionTargetRatio, [], leaveOpen);
    }

    internal Encryptor CreateEncryptor(
        Stream sourceInput,
        double compressionTargetRatio,
        ReadOnlySpan<byte> extraPayload,
        bool leaveOpen = false
    )
    {
        var sourceLength = sourceInput.Length - sourceInput.Position;

        ArgumentOutOfRangeException.ThrowIfGreaterThan(sourceLength, PLAIN_TEXT_MAX_SIZE);

        var isCompressed = false;
        var inputStream = sourceInput;

        if (compressionTargetRatio != COMPRESSION_TARGET_RATIO_DISABLED_VALUE)
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(
                compressionTargetRatio,
                COMPRESSION_TARGET_RATIO_LOWER_BOUND
            );
            ArgumentOutOfRangeException.ThrowIfGreaterThan(
                compressionTargetRatio,
                COMPRESSION_TARGET_RATIO_UPPER_BOUND
            );

            var compressedLengthLimit = (int)(sourceInput.Length * compressionTargetRatio);
            var compressedStream = streamManager.GetStream(
                null,
                requiredSize: compressedLengthLimit
            );

            using (
                var encompressionStream = new CompressionStream(
                    compressedStream,
                    bufferSize: 1024,
                    leaveOpen: true
                )
            )
            {
                sourceInput.CopyTo(encompressionStream);

                if (leaveOpen == false)
                {
                    sourceInput.Dispose();
                }
                leaveOpen = false;
            }

            logger.ZLogTrace(
                $"Compressed encryptor input from {sourceLength} to {compressedStream.Length}"
            );

            compressedStream.Position = 0;

            if (compressedStream.Length > compressedLengthLimit)
            {
                inputStream = new WrappedDecompressionStream(
                    compressedStream,
                    sourceLength,
                    leaveOpen: false
                );

                logger.ZLogTrace(
                    $"Compression not applied as the compressed length ({compressedStream.Length}) is above the limit ({compressedLengthLimit})"
                );
            }
            else
            {
                isCompressed = true;
                inputStream = compressedStream;
            }
        }

        Encryptor encryptor = new(
            this,
            inputStream,
            isCompressed,
            (int)sourceLength,
            extraPayload,
            leaveOpen: leaveOpen
        );

        return encryptor;
    }

    #endregion Encryption

    #region  Decryption

    internal MemoryStream DecryptName(byte[] nameData, ReadOnlySpan<byte> hash)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(hash.Length, NAME_HASH_MIN_SIZE, nameof(hash));

        if (nameData.Length <= NAME_CRC_SIZE)
        {
            throw new FormatException($"Data must be more than {NAME_CRC_SIZE} bytes");
        }

        if (
            nameData.Length < (NAME_CRC_SIZE + CIPHER_BLOCK_SIZE)
            || (nameData.Length - NAME_CRC_SIZE) % CIPHER_BLOCK_SIZE != 0
        )
        {
            throw new FormatException($"Data not aligned on {CIPHER_BLOCK_SIZE} bytes boundary");
        }

        if (nameData[0] != _hallmarkDataCrc)
        {
            throw new FormatException($"Data does not seem to have correct head CRC");
        }

        var crc = Crc8.Compute(nameData.AsSpan(1), _hallmarkDataCrc);
        if (crc != 0)
        {
            throw new FormatException($"Data does not seem to have correct tail CRC");
        }

        var ivData = hash[0..CIPHER_BLOCK_SIZE].ToArray();

        int decKeyIterationCount = GetFirstNonZero(hash);
        var decKeySalt = hash[NAME_HASH_SALT_OFFSET..NAME_HASH_MIN_SIZE];
        var decKey = KeyHolder.DeriveCipherKey(decKeySalt, decKeyIterationCount);

        var decryptTransform = GetDecryptionTransform(decKey, ivData);

        var nameStream = new MemoryStream(
            nameData,
            NAME_HEAD_CRC_SIZE,
            nameData.Length - NAME_CRC_SIZE,
            writable: false
        );

        MemoryStream result = new();

        using (
            var cryptoStream = new CryptoStream(nameStream, decryptTransform, CryptoStreamMode.Read)
        )
        {
            cryptoStream.CopyTo(result);
        }

        return result;
    }

    byte[] PrepareDecryptionKey(
        byte[] ivData,
        ReadOnlySpan<byte> decKeySalt,
        int decKeyIterationCount
    )
    {
        ArgumentOutOfRangeException.ThrowIfNotEqual(
            ivData.Length,
            CIPHER_BLOCK_SIZE,
            nameof(ivData)
        );

        ArgumentOutOfRangeException.ThrowIfNotEqual(
            decKeySalt.Length,
            CIPHER_BLOCK_SIZE,
            nameof(decKeySalt)
        );

        var result = KeyHolder.DeriveCipherKey(decKeySalt, decKeyIterationCount);

        return result;
    }

    ICryptoTransform GetDecryptionTransform(byte[] decKey, byte[] ivData) =>
        _aes.CreateDecryptor(decKey, ivData);

    internal class DecryptorStream : Stream
    {
        readonly Aes256Cbc1 _cipher;
        readonly byte[] _ivData = new byte[CIPHER_BLOCK_SIZE];

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
            throw new NotSupportedException();
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

            var decTransform = _cipher.GetDecryptionTransform(_decKey!, _ivData);

            using var cryptoStream = new CryptoStream(
                input,
                decTransform,
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

            var hallmarkData = new byte[HALLMARK_SIZE];
            var hallmarkSize = _sourceStream.Read(hallmarkData);
            if (hallmarkSize != HALLMARK_SIZE)
            {
                throw new InvalidHallmarkException($"Cannot read {nameof(HALLMARK_SIZE)}");
            }
            if (hallmarkData.SequenceEqual(HALLMARK_DATA) == false)
            {
                throw new InvalidHallmarkException($"Invalid {nameof(HALLMARK_DATA)}");
            }

            var reservedData = new byte[RESERVED_SIZE];
            var reservedSize = _sourceStream.Read(reservedData);
            if (reservedSize != RESERVED_SIZE)
            {
                throw new InvalidDataException($"Cannot read {nameof(RESERVED_SIZE)}");
            }

            var randomFill = new byte[RANDOM_FILL_SIZE];
            var randomFillSize = _sourceStream.Read(randomFill);
            if (randomFillSize != RANDOM_FILL_SIZE)
            {
                throw new InvalidDataException($"Cannot read {nameof(RANDOM_FILL_SIZE)}");
            }

            randomFill.AsSpan(0, CIPHER_BLOCK_SIZE).CopyTo(_ivData);

            int decKeyIterationCount = GetFirstNonZero(randomFill);
            var decKeySalt = randomFill.AsSpan(RANDOM_FILL_SALT_OFFSET);

            _decKey = _cipher.PrepareDecryptionKey(_ivData, decKeySalt, decKeyIterationCount);
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

            var scrambleData = new byte[PLAIN_TEXT_SCRAMBLE_SIZE];
            _cryptoStream.ReadExactly(scrambleData);
            _outputOffset += PLAIN_TEXT_SCRAMBLE_SIZE;
            var primaryFlags = (ContentHeaderPrimaryFlags)scrambleData[0];

            if (primaryFlags.HasFlag(ContentHeaderPrimaryFlags.ContentIsCompressed))
            {
                _isCompressed = true;
            }

            const int lengthSize = sizeof(int);
            var lengthData = new byte[lengthSize];
            _cryptoStream.ReadExactly(lengthData, 0, lengthSize);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(lengthData);
            }
            _outputLength = BitConverter.ToInt32(lengthData);
            _outputOffset += lengthSize;

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

        bool _disposed;

        protected override void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }
            _disposed = true;

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

        bool _disposed;

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }
            _disposed = true;

            _decStream.Dispose();
        }
    }

    internal Decryptor CreateDecryptor(Stream inputStream, bool leaveOpen = false)
    {
        var decryptor = new Decryptor(this, inputStream, leaveOpen);

        return decryptor;
    }

    #endregion  Decryption
}
