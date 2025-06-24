using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Text;
using Git.Taut;
using Microsoft.Extensions.Logging;
using ZLogger;
using ZstdSharp;

namespace Cli.Tests;

public sealed partial class Aes256Cbc1Tests : IDisposable
{
    LoggerFactoryFixture _loggerFactory;

    [AllowNull]
    Aes256Cbc1 _cihper;

    public Aes256Cbc1Tests(LoggerFactoryFixture loggerFactory)
    {
        _loggerFactory = loggerFactory;
        _cihper = new Aes256Cbc1(_loggerFactory.CreateLogger<Aes256Cbc1>());
        _cihper.Init();
    }

    public void Dispose()
    {
        _cihper = null;
    }

    [Fact]
    public void Bad_NotInitialized()
    {
        var cipher = new Aes256Cbc1(_loggerFactory.CreateLogger<Aes256Cbc1>());

        Assert.Throws<InvalidOperationException>(() => cipher.EnsureInitialized());

        Assert.Throws<InvalidOperationException>(() => cipher.GetCipherTextLength(100));
    }

    const string Sonnet_VIII_Part1 = """
Music to hear, why hear'st thou music sadly?
Sweets with sweets war not, joy delights in joy.
Why lovest thou that which thou receivest not gladly,
Or else receivest with pleasure thine annoy?
""";

    [Fact]
    public void Good_EncryptDecrypt()
    {
        var input = Encoding.ASCII.GetBytes(Sonnet_VIII_Part1);
        var inputStream = new MemoryStream(input, writable: false);

        var enc = _cihper.CreateEncryptor(inputStream);

        Assert.False(enc.IsCompressed);

        var encOutputLength = enc.GetOutputLength();

        var encOutputStream = new MemoryStream();
        enc.ProduceOutput(encOutputStream);

        Assert.Equal(encOutputLength, encOutputStream.Length);

        encOutputStream.Position = 0;
        var dec = _cihper.CreateDecryptor(encOutputStream);
        Assert.False(dec.IsCompressed);

        var decOutputLength = dec.GetOutputLength();
        var decExtraPayload = dec.GetExtraPayload();

        Assert.Equal(inputStream.Length, decOutputLength);
        Assert.Equal(0, decExtraPayload.Length);

        var decOutputStream = new MemoryStream();
        dec.ProduceOutput(decOutputStream);
        Assert.Equal(inputStream.Length, decOutputStream.Length);
        Assert.Equal(
            input.AsSpan(),
            decOutputStream.GetBuffer().AsSpan(0, (int)decOutputStream.Length)
        );
    }

    [Fact]
    public void Good_EncryptDecryptWithCompression()
    {
        var input = Encoding.ASCII.GetBytes(Sonnet_VIII_Part1);
        var inputStream = new MemoryStream(input, writable: false);

        const double compressionMaxRatio = 0.8;
        var enc = _cihper.CreateEncryptor(inputStream, compressionMaxRatio);

        Assert.True(enc.IsCompressed);

        var encOutputLength = enc.GetOutputLength();
        var encOutputStream = new MemoryStream();
        enc.ProduceOutput(encOutputStream);

        Assert.Equal(encOutputLength, encOutputStream.Length);

        var encContentLength = enc.GetContentLength();
        Assert.True(encContentLength < inputStream.Length);

        encOutputStream.Position = 0;
        var dec = _cihper.CreateDecryptor(encOutputStream);

        Assert.True(dec.IsCompressed);

        var decOutputLength = dec.GetOutputLength();
        var decExtraPayload = dec.GetExtraPayload();

        Assert.Equal(inputStream.Length, decOutputLength);
        Assert.Equal(0, decExtraPayload.Length);

        var decOutputStream = new MemoryStream();
        dec.ProduceOutput(decOutputStream);

        Assert.Equal(inputStream.Length, decOutputStream.Length);
        Assert.Equal(
            input.AsSpan(),
            decOutputStream.GetBuffer().AsSpan(0, (int)decOutputStream.Length)
        );
    }
}
