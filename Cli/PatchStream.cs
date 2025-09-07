using System.Text;

namespace Git.Taut;

class PatchRegainStream : Stream
{
    readonly MemoryStream _targetStream;

    static readonly byte[] s_diffGitDummyLine = Encoding.ASCII.GetBytes(
        $"diff --git a/dummy b/dummy\n"
    );

    internal PatchRegainStream()
    {
        _targetStream = new();

        _targetStream.Write(s_diffGitDummyLine);
    }

    public override bool CanRead => false;

    public override bool CanSeek => false;

    public override bool CanWrite => true;

    public override long Length => _targetStream.Length;

    public override long Position
    {
        get => _targetStream.Position;
        set => throw new NotSupportedException();
    }

    public override void Flush()
    {
        throw new NotSupportedException();
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        throw new NotSupportedException();
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
        _targetStream.Write(buffer, offset, count);
    }

    internal byte[] GetBuffer()
    {
        return _targetStream.GetBuffer();
    }
}

class PatchTautenStream : Stream
{
    readonly Stream _sourceStream;
    readonly MemoryStream _headerStream;

    readonly long _length;
    readonly long _sourceHeaderOffset;

    long _totalRead;

    static readonly byte[] s_diffGit = Encoding.ASCII.GetBytes("diff --git");
    static readonly byte[] s_tripleMinus = Encoding.ASCII.GetBytes("---");
    static readonly byte[] s_tripleMinusDummyLine = Encoding.ASCII.GetBytes($"--- a/dummy\n");
    static readonly byte[] s_triplePlus = Encoding.ASCII.GetBytes("+++");
    static readonly byte[] s_triplePlusDummyLine = Encoding.ASCII.GetBytes($"+++ b/dummy\n");
    static readonly byte[] s_doubleAt = Encoding.ASCII.GetBytes("@@");

    internal PatchTautenStream(Stream patchInput)
    {
        _sourceStream = patchInput;
        _headerStream = new();

        PrepareHeaderStream();

        _headerStream.Position = 0;
        _sourceHeaderOffset = _sourceStream.Position;
        _length = _headerStream.Length + _sourceStream.Length - _sourceHeaderOffset;
    }

    int ReadLine()
    {
        int dataRead = 0;

        for (; ; )
        {
            var val = _sourceStream.ReadByte();
            if (val == -1)
            {
                break;
            }

            _headerStream.WriteByte((byte)val);
            dataRead++;

            if (val == '\n')
            {
                break;
            }
        }

        return dataRead;
    }

    void PrepareHeaderStream()
    {
        for (int totalRead = 0; ; )
        {
            var dataRead = ReadLine();
            if (dataRead == 0)
            {
                break;
            }

            var buffer = _headerStream.GetBuffer();
            var lineData = buffer.AsSpan(totalRead, dataRead);
            if (lineData.StartsWith(s_diffGit))
            {
                _headerStream.SetLength(totalRead); // rewind

                continue;
            }
            if (lineData.StartsWith(s_tripleMinus))
            {
                _headerStream.SetLength(totalRead); // rewind
                _headerStream.Write(s_tripleMinusDummyLine);
                totalRead = (int)_headerStream.Length;

                continue;
            }
            if (lineData.StartsWith(s_triplePlus))
            {
                _headerStream.SetLength(totalRead); // rewind
                _headerStream.Write(s_triplePlusDummyLine);
                totalRead = (int)_headerStream.Length;

                continue;
            }
            if (lineData.StartsWith(s_doubleAt))
            {
                break; // we have passed the diff header, time to break
            }

            totalRead = (int)_headerStream.Length;
        }
    }

    public override bool CanRead => _sourceStream.CanRead;

    public override bool CanSeek => false;

    public override bool CanWrite => false;

    public override long Length => _length;

    public override long Position
    {
        get => _totalRead;
        set
        {
            ArgumentOutOfRangeException.ThrowIfNotEqual(value, 0);

            _totalRead = 0;
            _headerStream.Position = 0;
            _sourceStream.Position = _sourceHeaderOffset;
        }
    }

    public override void Flush()
    {
        throw new NotSupportedException();
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        int dataRead;

        if (_headerStream.Position != _headerStream.Length)
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
}
