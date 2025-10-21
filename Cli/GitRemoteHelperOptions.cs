using Microsoft.Extensions.Logging;
using ZLogger;

namespace Git.Taut;

class GitRemoteHelperOptions(ILogger<GitRemoteHelper> logger)
{
    const string optVerbosity = "verbosity";
    internal int Verbosity = 1;

    const string optProgress = "progress";
    internal bool ShowProgress = false;

    const string optCloning = "cloning";
    internal bool IsCloning = false;

    const string optCheckConnectivity = "check-connectivity";
    internal bool CheckConnectivity = false;

    const string optForce = "force";
    internal bool ForceUpdate = false;

    const string optDryRun = "dry-run";
    internal bool DryRun = false;

    internal void HandleNameValue(string nameValue)
    {
        bool GetBooleanValue(string opt)
        {
            return nameValue[(opt.Length + 1)..] == "true";
        }

        void TraceOptionUpdate(string opt, string value)
        {
            logger.ZLogTrace($"Set option '{opt}' to {value}");
        }

        if (nameValue.StartsWith(optVerbosity))
        {
            if (int.TryParse(nameValue[(optVerbosity.Length + 1)..], out var value))
            {
                Verbosity = value;

                logger.SendLineToGit("ok");

                TraceOptionUpdate(optVerbosity, value.ToString());
            }
            else
            {
                Console.WriteLine("error falied to parse the value");
            }
        }
        else if (nameValue.StartsWith(optProgress))
        {
            ShowProgress = GetBooleanValue(optProgress);

            logger.SendLineToGit("ok");

            TraceOptionUpdate(optProgress, ShowProgress.ToString());
        }
        else if (nameValue.StartsWith(optCloning))
        {
            IsCloning = GetBooleanValue(optCloning);

            logger.SendLineToGit("ok");

            TraceOptionUpdate(optCloning, IsCloning.ToString());
        }
        else if (nameValue.StartsWith(optCheckConnectivity))
        {
            CheckConnectivity = GetBooleanValue(optCheckConnectivity);

            logger.SendLineToGit("ok");

            TraceOptionUpdate(optCheckConnectivity, CheckConnectivity.ToString());
        }
        else if (nameValue.StartsWith(optForce))
        {
            ForceUpdate = GetBooleanValue(optForce);

            logger.SendLineToGit("ok");

            TraceOptionUpdate(optForce, ForceUpdate.ToString());
        }
        else if (nameValue.StartsWith(optDryRun))
        {
            DryRun = GetBooleanValue(optDryRun);

            logger.SendLineToGit("ok");

            TraceOptionUpdate(optDryRun, DryRun.ToString());
        }
        else
        {
            logger.SendLineToGit("unsupported");
        }
    }
}
