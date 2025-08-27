using System.Diagnostics.CodeAnalysis;
using System.Text;
using Microsoft.Extensions.Logging;
using ZLogger;

namespace Git.Taut;

sealed class GitCredential : IDisposable
{
    GitCli _cli;
    string _url;

    StringBuilder _buf = new();

    [AllowNull]
    byte[] _passwordData;

    internal byte[] PasswordData => _passwordData!;

    internal string FilledInfo => _buf.ToString();

    internal GitCredential(GitCli cli, string url)
    {
        _cli = cli;
        _url = url;
    }

    void FillDataReceiver(string line)
    {
        if (_buf.Length > 0)
        {
            _buf.Append('\n');
            _buf.Append(line);
        }
        else
        {
            _buf.Append(line);
        }

        if (_passwordData is null)
        {
            _passwordData = ParseForPasswordData(line);
        }
    }

    void ErrorDataReceiver(string line)
    {
        Console.Error.WriteLine(line);
    }

    static byte[]? ParseForPasswordData(string line)
    {
        const string prefix = "password=";

        if (line.StartsWith(prefix))
        {
            var password = line[prefix.Length..];

            var passwordData = Encoding.ASCII.GetBytes(password);

            return passwordData;
        }

        return null;
    }

    void ClearLastFill()
    {
        for (int i = 0; i < _buf.Length; i++)
        {
            _buf[i] = '\0';
        }

        _buf.Clear();

        if (_passwordData is not null)
        {
            Array.Fill<byte>(_passwordData, 0);
            _passwordData = null;
        }
    }

    internal void Fill()
    {
        ClearLastFill();

        void InputProvider(StreamWriter writer)
        {
            var urlLine = $"url={_url}";

            writer.WriteLine(urlLine);
            writer.WriteLine();
        }

        _cli.Execute(InputProvider, FillDataReceiver, ErrorDataReceiver, "credential", "fill");
    }

    void BufInputProvider(StreamWriter writer)
    {
        if (_buf.Length == 0)
        {
            throw new InvalidOperationException($"There is not filled data");
        }

        for (int i = 0; i < _buf.Length; i++)
        {
            writer.Write(_buf[i]);
        }
    }

    internal void Approve()
    {
        _cli.Execute(BufInputProvider, null, ErrorDataReceiver, "credential", "approve");

        ClearLastFill();
    }

    internal void Reject()
    {
        _cli.Execute(BufInputProvider, null, ErrorDataReceiver, "credential", "reject");

        ClearLastFill();
    }

    public void Dispose()
    {
        ClearLastFill();
    }
}
