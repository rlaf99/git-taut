using System.Text;

namespace Git.Taut;

sealed class GitCredential : IDisposable
{
    GitCli _cli;
    string _url;

    StringBuilder _buf = new();

    string _userName = string.Empty;

    internal string UserName => _userName;

    byte[] _passwordData = [];

    internal byte[] PasswordData => _passwordData;

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

        if (string.IsNullOrEmpty(_userName))
        {
            _userName = ParseForUserName(line);
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

    static string ParseForUserName(string line)
    {
        const string prefix = "username=";

        var username = string.Empty;

        if (line.StartsWith(prefix))
        {
            var value = line[prefix.Length..];
            username = value.Trim();
        }

        return username;
    }

    static byte[] ParseForPasswordData(string line)
    {
        const string prefix = "password=";

        if (line.StartsWith(prefix))
        {
            var password = line[prefix.Length..];

            var passwordData = Encoding.ASCII.GetBytes(password);

            return passwordData;
        }

        return [];
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

            _passwordData = [];
            _userName = string.Empty;
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

    void InputProvider(StreamWriter writer)
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
        _cli.Execute(InputProvider, null, ErrorDataReceiver, "credential", "approve");

        ClearLastFill();
    }

    internal void Reject()
    {
        _cli.Execute(InputProvider, null, ErrorDataReceiver, "credential", "reject");

        ClearLastFill();
    }

    public void Dispose()
    {
        ClearLastFill();
    }
}
