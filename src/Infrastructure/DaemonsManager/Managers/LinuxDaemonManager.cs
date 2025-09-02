using System.Diagnostics;
using FmuApiDomain.DaemonsManager;

namespace ServicesAndDaemonsManager.Managers;

public class LinuxDaemonManager : IDaemonManager
{
    public bool Restart(string daemonName)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "systemctl",
            Arguments = $"restart {daemonName}",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        try
        {
            using var process = Process.Start(startInfo);

            if (process is null)
                return false;

            process.WaitForExit();

            return process.ExitCode == 0;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public bool IsRunning(string daemonName)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "systemctl",
            Arguments = $"is-active {daemonName}",
            UseShellExecute = false,
            RedirectStandardOutput = true
        };

        try
        {
            using var process = Process.Start(startInfo);
            
            if (process is null)
                return false;
            
            process.WaitForExit();
            return process.ExitCode == 0;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public string Status(string daemonName)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "systemctl",
            Arguments = $"is-active {daemonName}",
            UseShellExecute = false,
            RedirectStandardOutput = true
        };

        try
        {
            using var process = Process.Start(startInfo);

            if (process is null)
                return "Не найдена";
            
            var output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();
            return output.Trim();
        }
        catch (Exception)
        {
            return "Не найдена";
        }
    }
}