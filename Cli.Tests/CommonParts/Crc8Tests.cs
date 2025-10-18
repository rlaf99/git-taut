using System.Text;
using Git.Taut;

namespace Cli.Tests.CommonParts;

public sealed partial class Crc8Tests : IDisposable
{
    public void Dispose() { }

    [Fact]
    public unsafe void ByteWiseVsTable()
    {
        Span<byte> data = stackalloc byte[1];

        for (int i = 0; i < 256; i++)
        {
            data[0] = (byte)i;

            var result1 = Crc8.ComputeByteWise(data);
            var result2 = Crc8.ComputeFromTable(data);

            Assert.Equal(result1, result2);
        }
    }

    [Fact]
    public unsafe void ByteWiseVsTable2()
    {
        Span<byte> data = stackalloc byte[1];

        byte initialValue = 1;

        for (int i = 0; i < 256; i++)
        {
            data[0] = (byte)i;

            var result1 = Crc8.ComputeByteWise(data, initialValue);
            var result2 = Crc8.ComputeFromTable(data, initialValue);

            Assert.Equal(result1, result2);
        }
    }

    [Fact]
    public void ComputeThenVerify()
    {
        var data = Encoding.ASCII.GetBytes("foo");

        var result1 = Crc8.ComputeByteWise(data);
        var result2 = Crc8.ComputeFromTable(data);

        Assert.Equal(result1, result2);

        var data2 = new byte[data.Length + 1];
        data.CopyTo(data2, 0);
        data2[^1] = result1;

        result1 = Crc8.ComputeByteWise(data2);
        result2 = Crc8.ComputeFromTable(data2);

        Assert.Equal(0, result1);
        Assert.Equal(0, result2);
    }

    [Fact]
    public void ComputeThenVerify2()
    {
        var preamble = Encoding.ASCII.GetBytes("foo");
        var data = Encoding.ASCII.GetBytes("bar");

        var result1 = Crc8.ComputeByteWise(preamble);
        var result2 = Crc8.ComputeFromTable(preamble);

        Assert.Equal(result1, result2);

        result1 = Crc8.ComputeByteWise(data, result1);
        result2 = Crc8.ComputeFromTable(data, result2);

        Assert.Equal(result1, result2);

        var data2 = new byte[data.Length + 1];
        data.CopyTo(data2, 0);
        data2[^1] = result1;

        result1 = Crc8.ComputeByteWise(preamble);
        result2 = Crc8.ComputeFromTable(preamble);

        result1 = Crc8.ComputeByteWise(data2, result1);
        result2 = Crc8.ComputeFromTable(data2, result2);

        Assert.Equal(0, result1);
        Assert.Equal(0, result2);
    }
}
