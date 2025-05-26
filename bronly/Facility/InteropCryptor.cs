using System;
using System.Diagnostics;
using System.IO;
using System.IO.Hashing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using Okra.Larch.Interop;
using static Okra.Larch.Facility.YggStatics;
using static Okra.Larch.Interop.Ygg_ErrorCodeE;
using static Okra.Larch.Interop.Ygg_FilterStreamDirectionE;

namespace Okra.Larch.Facility;

internal class CryptorKeySalt
{
    const byte VERSION = 0xa1;
    const int DUMMY_BYTES_LENGTH = 15;
    const int CRC32_BYTES_LENGTH = 4;
    const int TOTAL_BYTES_LENGTH = 1 + DUMMY_BYTES_LENGTH + CRC32_BYTES_LENGTH;

    readonly byte[] m_bytes = new byte[TOTAL_BYTES_LENGTH];

    internal static CryptorKeySalt? GlobalDefault { get; set; } = null;

    internal ReadOnlySpan<byte> Data
    {
        get { return m_bytes; }
    }

    internal CryptorKeySalt()
    {
        m_bytes[0] = VERSION;
        RandomNumberGenerator.Fill(new Span<byte>(m_bytes, 1, DUMMY_BYTES_LENGTH));

        Crc32.Hash(
            new Span<byte>(m_bytes, 0, 1 + DUMMY_BYTES_LENGTH),
            new Span<byte>(m_bytes, 1 + DUMMY_BYTES_LENGTH, 4)
        );
    }

    CryptorKeySalt(byte[] bytes)
    {
        m_bytes = bytes;
    }

    internal static bool TryParse(string hex, out CryptorKeySalt? result)
    {
        result = null;

        try
        {
            var bytes = Convert.FromHexString(hex);
            if (bytes.Length != TOTAL_BYTES_LENGTH)
            {
                return false;
            }
            if (bytes[0] != VERSION)
            {
                return false;
            }

            var crcCheck = new byte[4];
            Crc32.Hash(new Span<byte>(bytes, 0, 1 + DUMMY_BYTES_LENGTH), crcCheck);

            var crcBytes = new Span<byte>(bytes, 1 + DUMMY_BYTES_LENGTH, 4);

            Debug.Assert(crcBytes.Length == crcCheck.Length);

            for (var i = 0; i < crcBytes.Length; i++)
            {
                if (crcBytes[i] != crcCheck[i])
                {
                    return false;
                }
            }

            result = new CryptorKeySalt(bytes);

            return true;
        }
        catch
        {
            // do nothing
        }

        return false;
    }
}

internal class CryptorKeyMaker : IDisposable
{
    const int ITERATION_COUNT = 123210;
    internal const int KEY_BYTES_LENGTH = 32;

    CryptorKeySalt? m_salt;

    byte[]? m_key = null;

    internal byte[] Key
    {
        get
        {
            if (m_key == null)
            {
                m_salt = CryptorKeySalt.GlobalDefault;

                if (m_salt == null)
                {
                    throw new InvalidOperationException("salt is null");
                }

                m_key = Rfc2898DeriveBytes.Pbkdf2(
                    "hello!",
                    m_salt.Data,
                    ITERATION_COUNT,
                    HashAlgorithmName.SHA256,
                    KEY_BYTES_LENGTH
                );
            }

            return m_key;
        }
    }

    void ClearKey()
    {
        if (m_key != null)
        {
            for (var i = 0; i < m_key.Length; i++)
            {
                m_key[i] = 0;
            }

            m_key = null;
        }
    }

    /* singleton */

    static CryptorKeyMaker? s_inst = null;

    internal static CryptorKeyMaker Inst
    {
        get
        {
            if (s_inst == null)
            {
                s_inst = new CryptorKeyMaker();
            }

            return s_inst;
        }
    }

    /* IDisposable */

    public void Dispose()
    {
        ClearKey();
    }
}

internal abstract unsafe class CryptorBase
{
    protected Ygg_FilterStreamDirectionE m_direction;

    protected CryptorBase(Ygg_FilterStreamDirectionE direction)
    {
        m_direction = direction;
    }
}

/* AesCryptor */

internal unsafe class AesCryptor : CryptorBase
{
    internal static readonly byte[] MAGIC_BYTES = new byte[4] { 1, 9, 9, 0xa1 };

    internal const int CIPHER_BLOCK_SIZE = 16; // in bytes
    internal const int HMAC_BYTES_LENGTH = 20;

    Aes m_aes;

    MemoryStream m_buffer = new();

    Ygg_FilterStreamPipelineS m_streamNext;

    internal SymmetricAlgorithm Algo
    {
        get => m_aes;
    }

    internal AesCryptor(Ygg_FilterStreamDirectionE direction, Ygg_FilterStreamPipelineS streamNext)
        : base(direction)
    {
        m_streamNext = streamNext;

        if (
            m_streamNext.p_strm == null
            || m_streamNext.fp_output == null
            || m_streamNext.fp_finish == null
        )
        {
            throw new ArgumentException($"invalid {streamNext}");
        }

        m_aes = Aes.Create();

        m_aes.Mode = CipherMode.CBC; // making it explicit
        m_aes.Padding = PaddingMode.PKCS7; // making it explicit

        m_aes.Key = CryptorKeyMaker.Inst.Key;

        Array.Clear(m_aes.IV);

        Debug.Assert(m_aes.BlockSize == CIPHER_BLOCK_SIZE * 8);
        Debug.Assert(m_aes.IV.Length == CIPHER_BLOCK_SIZE);
        Debug.Assert(HMACSHA1.HashSizeInBytes == HMAC_BYTES_LENGTH);
        Debug.Assert(HMAC_BYTES_LENGTH > CIPHER_BLOCK_SIZE);
    }

    internal void Collect(sbyte* buf_p, int len)
    {
        m_buffer.Write(new ReadOnlySpan<byte>(buf_p, len));
    }

    bool HasMagicBytes()
    {
        var len = MAGIC_BYTES.Length;
        if (m_buffer.Length >= len)
        {
            var magicBytes = new ReadOnlySpan<byte>(m_buffer.GetBuffer(), 0, len);
            if (magicBytes.SequenceEqual(MAGIC_BYTES))
            {
                return true;
            }
        }

        return false;
    }

    void PipelineRawContent()
    {
        if (m_buffer.Length > 0)
        {
            m_buffer.Position = 0;

            var buf = m_buffer.GetBuffer();
            var len = m_buffer.Length;

            fixed (byte* buf_p = buf)
            {
                YggCheckErrorCode(
                    m_streamNext.fp_output(m_streamNext.p_strm, (sbyte*)buf_p, (int)len)
                );
            }
        }

        YggCheckErrorCode(m_streamNext.fp_finish(m_streamNext.p_strm));
    }

    void FinishForStore()
    {
        if (m_buffer.Length == 0 || HasMagicBytes())
        {
            PipelineRawContent();
            return;
        }

        fixed (byte* p = MAGIC_BYTES)
        {
            YggCheckErrorCode(
                m_streamNext.fp_output(m_streamNext.p_strm, (sbyte*)p, MAGIC_BYTES.Length)
            );
        }

        m_buffer.Position = 0;
        var hmacVal = HMACSHA1.HashData(m_aes.Key, m_buffer);
        m_aes.IV = hmacVal[0..CIPHER_BLOCK_SIZE];

        fixed (byte* hmacVal_p = hmacVal)
        {
            YggCheckErrorCode(
                m_streamNext.fp_output(m_streamNext.p_strm, (sbyte*)hmacVal_p, hmacVal.Length)
            );
        }

        m_buffer.Position = 0;
        using var cryptoStream = new CryptoStream(
            m_buffer,
            m_aes.CreateEncryptor(),
            CryptoStreamMode.Read
        );

        var readBuf = new byte[CIPHER_BLOCK_SIZE];
        var readLen = 0;

        while ((readLen = cryptoStream.Read(readBuf)) > 0)
        {
            fixed (byte* buf_p = readBuf)
            {
                YggCheckErrorCode(
                    m_streamNext.fp_output(m_streamNext.p_strm, (sbyte*)buf_p, readLen)
                );
            }
        }

        YggCheckErrorCode(m_streamNext.fp_finish(m_streamNext.p_strm));
    }

    void FinishForFetch()
    {
        if (!HasMagicBytes())
        {
            PipelineRawContent();
            return;
        }

        var ciphertextOffset = MAGIC_BYTES.Length + HMAC_BYTES_LENGTH;

        if (m_buffer.Length <= ciphertextOffset)
        {
            throw new InvalidDataException("incorrect hmac length");
        }

        if ((m_buffer.Length - ciphertextOffset) % CIPHER_BLOCK_SIZE != 0)
        {
            throw new InvalidDataException("incorrect ciphertext length");
        }

        var headHmacVal = new byte[HMAC_BYTES_LENGTH];
        m_buffer.Position = MAGIC_BYTES.Length;
        m_buffer.ReadExactly(headHmacVal);

        m_aes.IV = headHmacVal[0..CIPHER_BLOCK_SIZE];

        m_buffer.Position = ciphertextOffset;
        using var cryptoStream = new CryptoStream(
            m_buffer,
            m_aes.CreateDecryptor(),
            CryptoStreamMode.Read
        );

        using var hmac = IncrementalHash.CreateHMAC(HashAlgorithmName.SHA1, m_aes.Key);

        var readBuf = new byte[CIPHER_BLOCK_SIZE];
        var readLen = 0;

        while ((readLen = cryptoStream.Read(readBuf)) > 0)
        {
            hmac.AppendData(readBuf, 0, readLen);

            fixed (byte* buf_p = readBuf)
            {
                YggCheckErrorCode(
                    m_streamNext.fp_output(m_streamNext.p_strm, (sbyte*)buf_p, readLen)
                );
            }
        }

        var realHmacVal = hmac.GetCurrentHash();
        if (!realHmacVal.SequenceEqual(headHmacVal))
        {
            throw new InvalidDataException("validation with hmac failed");
        }

        YggCheckErrorCode(m_streamNext.fp_finish(m_streamNext.p_strm));
    }

    internal void Finish()
    {
        if (m_direction == YGG_FILTER_STREAM_STORE)
        {
            FinishForStore();
        }
        else
        {
            FinishForFetch();
        }
    }

    internal void Clear()
    {
        m_aes.Clear();
        m_aes.Dispose();
        m_buffer.Dispose();
    }
}

internal static unsafe class AesCryptorStatics
{
    internal static AesCryptor GetAesCryptorFromHandle(void* p)
    {
        var cryptor = GCHandle.FromIntPtr((nint)p).Target as AesCryptor;
        if (cryptor == null)
        {
            throw new InvalidOperationException("cryptor is null");
        }

        return cryptor;
    }

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static void* AesCryptorCreate(
        Ygg_FilterStreamDirectionE direction,
        Ygg_FilterStreamPipelineS streamNext
    )
    {
        try
        {
            var cryptor = new AesCryptor(direction, streamNext);

            return (void*)GCHandle.ToIntPtr(GCHandle.Alloc(cryptor));
        }
        catch (Exception)
        {
            return null;
        }
    }

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static void AesCryptorDestroy(void* self_p)
    {
        try
        {
            GCHandle.FromIntPtr((nint)self_p).Free();
        }
        catch (Exception)
        {
            // do nothing
        }
    }

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static Ygg_ErrorCodeE AesCryptorOutput(void* self_p, sbyte* buf_p, int len)
    {
        try
        {
            var cryptor = GCHandle.FromIntPtr((nint)self_p).Target as AesCryptor;
            if (cryptor == null)
            {
                return YGG_ERROR;
            }

            cryptor.Collect(buf_p, len);

            return YGG_OK;
        }
        catch (Exception)
        {
            return YGG_ERROR;
        }
    }

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static Ygg_ErrorCodeE AesCryptorFinish(void* self_p)
    {
        try
        {
            var cryptor = GCHandle.FromIntPtr((nint)self_p).Target as AesCryptor;
            if (cryptor == null)
            {
                return YGG_ERROR;
            }

            try
            {
                cryptor.Finish();
            }
            finally
            {
                cryptor.Clear();
            }

            return YGG_OK;
        }
        catch (Exception)
        {
            return YGG_ERROR;
        }
    }

    internal static Ygg_FilterStreamMethodGroupS GetAesCryptorMethods()
    {
        return new Ygg_FilterStreamMethodGroupS()
        {
            fp_create = &AesCryptorCreate,
            fp_destroy = &AesCryptorDestroy,
            fp_output = &AesCryptorOutput,
            fp_finish = &AesCryptorFinish,
        };
    }
}

/* PipelineSink */

internal unsafe class PipelinkSink
{
    internal MemoryStream m_buffer = new();

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static Ygg_ErrorCodeE Output(void* self_p, sbyte* buf_p, int len)
    {
        var self = GCHandle.FromIntPtr((nint)self_p).Target as PipelinkSink;
        if (self == null)
        {
            return YGG_ERROR;
        }

        try
        {
            self.m_buffer.Write(new ReadOnlySpan<byte>(buf_p, len));

            return YGG_OK;
        }
        catch
        {
            return YGG_ERROR;
        }
    }

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static Ygg_ErrorCodeE Finish(void* self_p)
    {
        try
        {
            GCHandle.FromIntPtr((nint)self_p).Free();

            return YGG_OK;
        }
        catch
        {
            return YGG_ERROR;
        }
    }

    internal Ygg_FilterStreamPipelineS GetPipeline()
    {
        var handle = GCHandle.ToIntPtr(GCHandle.Alloc(this));
        var result = new Ygg_FilterStreamPipelineS()
        {
            p_strm = (void*)handle,
            fp_output = &Output,
            fp_finish = &Finish,
        };

        return result;
    }
}
