using System.Text;
using Git.Taut;

namespace Cli.Tests;

public sealed partial class Base32Tests : IDisposable
{
    public void Dispose() { }

    [Fact]
    public void EncodeDecode()
    {
        string[] inputs = ["H", "He", "Hel", "Hell", "Hello", "Hello!"];
        string[] outputs =
        [
            "90======",
            "91IG====",
            "91IMO===",
            "91IMOR0=",
            "91IMOR3F",
            "91IMOR3F44======",
        ];

        for (int i = 0; i < inputs.Length; i++)
        {
            var input = inputs[i];
            var output = outputs[i];

            var inputData = Encoding.ASCII.GetBytes(input);
            var encodedString = Base32Hex.GetString(inputData);
            Assert.Equal(output, encodedString);

            var decodedData = Base32Hex.GetBytes(encodedString);
            var decodedString = Encoding.ASCII.GetString(decodedData);
            Assert.Equal(input, decodedString);
        }
    }

    [Fact]
    public void EmptyOutputs()
    {
        {
            var input = string.Empty;
            var output = Base32Hex.GetBytes(input);
            Assert.Equal([], output);
        }
    }

    [Fact]
    public void InvalidInputs()
    {
        {
            var input = "012345678";
            var ex = Assert.Throws<FormatException>(() => Base32Hex.GetBytes(input));
            Assert.Equal("The Length of input is not a multiple of 8", ex.Message);
        }

        {
            var input = "0123456W";
            var ex = Assert.Throws<FormatException>(() => Base32Hex.GetBytes(input));
            Assert.Equal("'W' is not in the alphabet", ex.Message);
        }
    }

    [Fact]
    public void InvalidPaddings()
    {
        {
            var input = "========";
            var ex = Assert.Throws<FormatException>(() => Base32Hex.GetBytes(input));
            Assert.Equal("Invalid padding: count of '=' is greater than 6", ex.Message);
        }

        {
            var input = "0=======";
            var ex = Assert.Throws<FormatException>(() => Base32Hex.GetBytes(input));
            Assert.Equal("Invalid padding: count of '=' is greater than 6", ex.Message);
        }

        {
            var input = "012=====";
            var ex = Assert.Throws<FormatException>(() => Base32Hex.GetBytes(input));
            Assert.Equal("Invalid padding: count of '=' is 5", ex.Message);
        }

        {
            var input = "012345==";
            var ex = Assert.Throws<FormatException>(() => Base32Hex.GetBytes(input));
            Assert.Equal("Invalid padding: count of '=' is 2", ex.Message);
        }
    }
}
