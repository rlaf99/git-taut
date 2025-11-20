// The code is adapted from https://github.com/dotnet/aspnetcore/blob/c821481c641a08274e8bf9743eed10d1475d2c6b/src/Identity/Extensions.Core/src/Base32.cs
using System.Text;

namespace Git.Taut;

/// <summary>
/// Implements (7.Base 32 Encoding with Extended Hex Alphabet)[https://datatracker.ietf.org/doc/html/rfc4648#autoid-12]
/// </summary>
class Base32Hex
{
    private const string _base32Chars = "0123456789ABCDEFGHIJKLMNOPQRSTUV";

    public static string GetString(byte[] input) => GetString(input.AsSpan());

    public static string GetString(ReadOnlySpan<byte> input)
    {
        if (input.Length == 0)
        {
            return string.Empty;
        }

        var roughMaxCapacity = (input.Length * 8 / 5) + 7;

        StringBuilder sb = new(roughMaxCapacity);

        for (int offset = 0; offset < input.Length; )
        {
            int numCharsToOutput = GetNextGroup(
                input,
                ref offset,
                out var a,
                out var b,
                out var c,
                out var d,
                out var e,
                out var f,
                out var g,
                out var h
            );

            sb.Append((numCharsToOutput >= 1) ? _base32Chars[a] : '=');
            sb.Append((numCharsToOutput >= 2) ? _base32Chars[b] : '=');
            sb.Append((numCharsToOutput >= 3) ? _base32Chars[c] : '=');
            sb.Append((numCharsToOutput >= 4) ? _base32Chars[d] : '=');
            sb.Append((numCharsToOutput >= 5) ? _base32Chars[e] : '=');
            sb.Append((numCharsToOutput >= 6) ? _base32Chars[f] : '=');
            sb.Append((numCharsToOutput >= 7) ? _base32Chars[g] : '=');
            sb.Append((numCharsToOutput >= 8) ? _base32Chars[h] : '=');
        }

        return sb.ToString();
    }

    public static byte[] GetBytes(string input)
    {
        if (input.Length % 8 != 0)
        {
            throw new FormatException($"The Length of {nameof(input)} is not a multiple of 8");
        }

        CheckPadding(input);

        var trimmedInput = input.AsSpan().TrimEnd('=');
        if (trimmedInput.Length == 0)
        {
            return [];
        }

        var output = new byte[trimmedInput.Length * 5 / 8];
        var bitIndex = 0;
        var inputIndex = 0;
        var outputBits = 0;
        var outputIndex = 0;
        while (outputIndex < output.Length)
        {
            var byteIndex = _base32Chars.IndexOf(char.ToUpperInvariant(trimmedInput[inputIndex]));
            if (byteIndex < 0)
            {
                throw new FormatException($"'{trimmedInput[inputIndex]}' is not in the alphabet");
            }

            var bits = Math.Min(5 - bitIndex, 8 - outputBits);
            output[outputIndex] <<= bits;
            output[outputIndex] |= (byte)(byteIndex >> (5 - (bitIndex + bits)));

            bitIndex += bits;
            if (bitIndex >= 5)
            {
                inputIndex++;
                bitIndex = 0;
            }

            outputBits += bits;
            if (outputBits >= 8)
            {
                outputIndex++;
                outputBits = 0;
            }
        }
        return output;
    }

    static void CheckPadding(string input)
    {
        int count = 0;
        int index = input.Length - 1;
        for (; index >= 0; index--)
        {
            if (input[index] == '=')
            {
                count++;
            }
            else
            {
                break;
            }
            if (count > 6)
            {
                throw new FormatException($"Invalid padding: count of '=' is greater than 6");
            }
        }

        switch (count)
        {
            case 2:
                throw new FormatException($"Invalid padding: count of '=' is 2");
            case 5:
                throw new FormatException($"Invalid padding: count of '=' is 5");
        }
    }

    private static int GetNextGroup(
        ReadOnlySpan<byte> input,
        ref int offset,
        out byte a,
        out byte b,
        out byte c,
        out byte d,
        out byte e,
        out byte f,
        out byte g,
        out byte h
    )
    {
        var retVal = (input.Length - offset) switch
        {
            1 => 2,
            2 => 4,
            3 => 5,
            4 => 7,
            _ => 8,
        };

        uint b1 = (offset < input.Length) ? input[offset++] : 0U;
        uint b2 = (offset < input.Length) ? input[offset++] : 0U;
        uint b3 = (offset < input.Length) ? input[offset++] : 0U;
        uint b4 = (offset < input.Length) ? input[offset++] : 0U;
        uint b5 = (offset < input.Length) ? input[offset++] : 0U;

        a = (byte)(b1 >> 3);
        b = (byte)(((b1 & 0x07) << 2) | (b2 >> 6));
        c = (byte)((b2 >> 1) & 0x1f);
        d = (byte)(((b2 & 0x01) << 4) | (b3 >> 4));
        e = (byte)(((b3 & 0x0f) << 1) | (b4 >> 7));
        f = (byte)((b4 >> 2) & 0x1f);
        g = (byte)(((b4 & 0x3) << 3) | (b5 >> 5));
        h = (byte)(b5 & 0x1f);

        return retVal;
    }
}
