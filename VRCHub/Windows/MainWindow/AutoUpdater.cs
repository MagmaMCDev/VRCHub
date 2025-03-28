using System.Diagnostics;
using System.IO;
using System.Security.Principal;

namespace VRCHub;

public partial class MainWindow
{
    private static readonly string TaskFile = Path.Combine(Environment.CurrentDirectory, "Plugins", "VRCHubTaskScheduler.exe");
    public static void SetupAutoUpdater()
    {
        string CreateTask = $"/create /tn VRCHub /tr \"'{TaskFile}'\" /sc onlogon /rl highest /f";
        string RunTask = $"/run /tn VRCHub";

        if (IsTaskInstalled("VRCHub"))
        {
            if (!IsTaskRunning("VRCHub"))
                Process.Start("schtasks", RunTask).WaitForExit();
            return;
        }
        if (!IsRunAsAdmin())
        {
            RelaunchAsAdmin();
            return;
        }

        ProcessStartInfo psi = new ProcessStartInfo("schtasks", CreateTask)
        {
            UseShellExecute = false,
            CreateNoWindow = true
        };

        Process process = Process.Start(psi)!;
        process.WaitForExit();
        Process.Start("schtasks", RunTask).WaitForExit();
    }

    /// <summary>
    /// Checks if the current process is running with administrative privileges.
    /// </summary>
    private static bool IsRunAsAdmin()
    {
        WindowsIdentity id = WindowsIdentity.GetCurrent();
        WindowsPrincipal principal = new WindowsPrincipal(id);
        return principal.IsInRole(WindowsBuiltInRole.Administrator);
    }

    /// <summary>
    /// Relaunches the current application with administrative privileges.
    /// </summary>
    private static void RelaunchAsAdmin()
    {
        ProcessStartInfo proc = new ProcessStartInfo
        {
            UseShellExecute = true,
            WorkingDirectory = Environment.CurrentDirectory,
            FileName = Environment.ProcessPath,
            Verb = "runas" 
        };

        Process.Start(proc);
        Environment.Exit(0);
    }

    /// <summary>
    /// Checks if a scheduled task with the given name exists.
    /// </summary>
    private static bool IsTaskInstalled(string taskName)
    {
        // This method uses schtasks to query for an existing task.
        // You might improve this by parsing the output for a more robust check.
        ProcessStartInfo psi = new ProcessStartInfo("schtasks", "/query /tn \"" + taskName + "\"")
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            CreateNoWindow = true
        };

        try
        {
            using (Process process = Process.Start(psi))
            {
                string output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();

                // If the output contains the task name, then it's already installed.
                return output.IndexOf(taskName, StringComparison.OrdinalIgnoreCase) >= 0;
            }
        }
        catch
        {
            // If querying fails (for instance, if the task doesn’t exist), assume it is not installed.
            return false;
        }
    }
    /// <summary>
    /// Checks if a scheduled task with the given name is currently running.
    /// </summary>
    private static bool IsTaskRunning(string taskName)
    {
        ProcessStartInfo psi = new ProcessStartInfo("schtasks", "/query /tn \"" + taskName + "\"")
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            CreateNoWindow = true
        };

        try
        {
            using Process process = Process.Start(psi)!;
            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();
            return output.Contains("Running");
        }
        catch
        {
            return false;
        }
    }

}