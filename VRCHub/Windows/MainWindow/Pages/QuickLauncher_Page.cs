using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using VRCHub.Resources;

namespace VRCHub;

public partial class MainWindow
{
    private Process? VRCQuickLauncher;
    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
    private void QuickLauncherButton_Click(object sender, RoutedEventArgs e)
    {
        if (VRCQuickLauncher == null || VRCQuickLauncher.HasExited)
        {
            ProcessStartInfo PSI = new(Path.Combine(Path.GetTempPath(), "VRC Quick Launcher.exe"));
            if (!File.Exists(Path.Combine(Path.GetTempPath(), "VRC Quick Launcher.exe")))
                File.WriteAllBytes(Path.Combine(Path.GetTempPath(), "VRC Quick Launcher.exe"), AppResources.VRC_Quick_Launcher);
            VRCQuickLauncher = Process.Start(PSI);
        }
        else
        {
            ShowWindow(VRCQuickLauncher.MainWindowHandle, 9);
            KERNAL32.SetForegroundWindow(VRCQuickLauncher.MainWindowHandle);
        }
    }
}