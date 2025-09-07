using System.Diagnostics.CodeAnalysis;
using System.Text;
using Git.Taut;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Cli.Tests;

public sealed partial class Aes256Cbc1Tests : IDisposable
{
    HostBuilderFixture _hostBuilder;

    IHost _host;

    [AllowNull]
    Aes256Cbc1 _cihper;

    const string Sonnet_VIII_Part1 = """
Music to hear, why hear'st thou music sadly?
Sweets with sweets war not, joy delights in joy.
Why lovest thou that which thou receivest not gladly,
Or else receivest with pleasure thine annoy?
""";

    byte[] GetUserPasswordData()
    {
        return Encoding.ASCII.GetBytes("Hello!");
    }

    public Aes256Cbc1Tests(HostBuilderFixture hostBuilder)
    {
        _hostBuilder = hostBuilder;

        _host = _hostBuilder.BuildHost();

        _cihper = _host.Services.GetRequiredService<Aes256Cbc1>();

        UserKeyHolder keyHolder = new();

        keyHolder.DeriveCrudeKey(GetUserPasswordData(), []);

        _cihper.Init(keyHolder);
    }

    public void Dispose()
    {
        _cihper = null;
    }

    public class TestNotInitialized(HostBuilderFixture hostBuilder)
    {
        [Fact]
        public void NotInitialized()
        {
            var host = hostBuilder.BuildHost();
            var cipher = host.Services.GetRequiredService<Aes256Cbc1>();
            Assert.Throws<InvalidOperationException>(() => cipher.EnsureInitialized());
            Assert.Throws<InvalidOperationException>(() => cipher.GetCipherTextLength(100));
        }
    }

    void EncryptDecrypt(string? extraInfo = null)
    {
        var input = Encoding.ASCII.GetBytes(Sonnet_VIII_Part1);
        var inputStream = new MemoryStream(input, writable: false);

        var enc = _cihper.CreateEncryptor(inputStream, leaveOpen: true);

        Assert.False(enc.IsCompressed);

        var encOutputLength = enc.GetOutputLength();
        var encOutputStream = new MemoryStream();

        MemoryStream? encExtraOutput = null;
        if (extraInfo is null)
        {
            enc.ProduceOutput(encOutputStream);
        }
        else
        {
            var extraBytes = Encoding.UTF8.GetBytes(extraInfo);
            var extraInput = new MemoryStream(extraBytes, writable: false);
            encExtraOutput = new MemoryStream();
            enc.ProduceOutput(encOutputStream, extraInput, encExtraOutput);
        }

        Assert.True(inputStream.Length > 0);
        Assert.Equal(encOutputLength, encOutputStream.Length);

        encOutputStream.Position = 0;
        var dec = _cihper.CreateDecryptor(encOutputStream, leaveOpen: true);
        Assert.False(dec.IsCompressed);

        var decOutputLength = dec.GetOutputLength();
        var decExtraPayload = dec.GetExtraPayload();

        Assert.Equal(inputStream.Length, decOutputLength);
        Assert.Equal(0, decExtraPayload.Length);

        var decOutputStream = new MemoryStream();

        if (extraInfo is null)
        {
            dec.ProduceOutput(decOutputStream);
        }
        else
        {
            encExtraOutput!.Position = 0;
            var decExtraOutput = new MemoryStream();
            dec.ProduceOutput(decOutputStream, encExtraOutput, decExtraOutput);

            var decExtraInfo = Encoding.UTF8.GetString(
                decExtraOutput.GetBuffer(),
                0,
                (int)decExtraOutput.Length
            );

            Assert.Equal(extraInfo, decExtraInfo);
        }

        Assert.Equal(inputStream.Length, decOutputStream.Length);
        Assert.Equal(
            input.AsSpan(),
            decOutputStream.GetBuffer().AsSpan(0, (int)decOutputStream.Length)
        );
    }

    [Fact]
    public void NoCompression()
    {
        EncryptDecrypt();
    }

    [Fact]
    public void NoCompressionWithExtra()
    {
        var extraInfo = "Hello";
        EncryptDecrypt(extraInfo);
    }

    void EncryptDecryptCompression(string? extraInfo = null)
    {
        var input = Encoding.ASCII.GetBytes(Sonnet_VIII_Part1);
        var inputStream = new MemoryStream(input, writable: false);

        const double compressionMaxRatio = 0.8;
        var enc = _cihper.CreateEncryptor(inputStream, compressionMaxRatio, leaveOpen: true);

        Assert.True(enc.IsCompressed);

        var encOutputLength = enc.GetOutputLength();
        var encOutputStream = new MemoryStream();

        MemoryStream encExtraOutput = new();
        if (extraInfo is null)
        {
            enc.ProduceOutput(encOutputStream);
        }
        else
        {
            var extraBytes = Encoding.UTF8.GetBytes(extraInfo);
            var extraInput = new MemoryStream(extraBytes, writable: false);
            enc.ProduceOutput(encOutputStream, extraInput, encExtraOutput);
        }

        Assert.Equal(encOutputLength, encOutputStream.Length);

        var encContentLength = enc.GetInputLength();
        Assert.True(encContentLength < inputStream.Length);

        encOutputStream.Position = 0;
        var dec = _cihper.CreateDecryptor(encOutputStream);

        Assert.True(dec.IsCompressed);

        var decOutputLength = dec.GetOutputLength();
        var decExtraPayload = dec.GetExtraPayload();

        Assert.Equal(inputStream.Length, decOutputLength);
        Assert.Equal(0, decExtraPayload.Length);

        var decOutputStream = new MemoryStream();

        if (extraInfo is null)
        {
            dec.ProduceOutput(decOutputStream);
        }
        else
        {
            encExtraOutput.Position = 0;
            var decExtraOutput = new MemoryStream();
            dec.ProduceOutput(decOutputStream, encExtraOutput, decExtraOutput);

            var decExtraInfo = Encoding.UTF8.GetString(
                decExtraOutput.GetBuffer(),
                0,
                (int)decExtraOutput.Length
            );

            Assert.Equal(extraInfo, decExtraInfo);
        }

        Assert.Equal(inputStream.Length, decOutputStream.Length);
        Assert.Equal(
            input.AsSpan(),
            decOutputStream.GetBuffer().AsSpan(0, (int)decOutputStream.Length)
        );
    }

    [Fact]
    public void UseCompression()
    {
        EncryptDecryptCompression();
    }

    [Fact]
    public void UseCompressionWithExtra()
    {
        var extraInfo = "Hello";
        EncryptDecryptCompression(extraInfo);
    }

    [Fact]
    public void TryCompressionButMaxRatioTooSmall()
    {
        var input = Encoding.ASCII.GetBytes(Sonnet_VIII_Part1);
        var inputStream = new MemoryStream(input, writable: false);

        const double compressionMaxRatio = 0.1;
        var enc = _cihper.CreateEncryptor(inputStream, compressionMaxRatio);

        Assert.False(enc.IsCompressed);

        Assert.Equal(input.Length, enc.GetInputLength());

        var outputLength = enc.GetOutputLength();

        var output = new MemoryStream();
        enc.ProduceOutput(output);

        Assert.Equal(outputLength, output.Length);
    }

    [Fact]
    public void TryCompressionButSourceTooSmall()
    {
        var input = Encoding.ASCII.GetBytes("abcde");
        var inputStream = new MemoryStream(input, writable: false);

        const double compressionMaxRatio = 0.9;
        var enc = _cihper.CreateEncryptor(inputStream, compressionMaxRatio);

        Assert.False(enc.IsCompressed);

        var inputLength = enc.GetInputLength();
        Assert.Equal(input.Length, inputLength);

        var outputLength = enc.GetOutputLength();

        var output = new MemoryStream();
        enc.ProduceOutput(output);

        Assert.Equal(outputLength, output.Length);
    }

    [Fact]
    public void TryEncryptAlreadyTautened()
    {
        var input = Encoding.ASCII.GetBytes(Sonnet_VIII_Part1);
        var inputStream = new MemoryStream(input, writable: false);

        var enc = _cihper.CreateEncryptor(inputStream);

        var encOutputLength = enc.GetOutputLength();
        var encOutputStream = new MemoryStream();

        enc.ProduceOutput(encOutputStream);
        Assert.Equal(encOutputLength, encOutputStream.Length);

        encOutputStream.Position = 0;
        Assert.Throws<InvalidDataException>(() => _cihper.CreateEncryptor(encOutputStream));
    }
}
