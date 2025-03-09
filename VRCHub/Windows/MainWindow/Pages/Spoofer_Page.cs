using Segment;
using System.Diagnostics;
using System.IO;
using System.Windows;
using static VRCHub.ButtonManager;

namespace VRCHub;

public partial class MainWindow
{
    private void VRCSpooferButton_Click(object sender, RoutedEventArgs? e)
    {
        if (Config.SendAnalytics)
            Analytics.Client.Page(Environment.MachineName, "VRCSpoofer Viewed");
        Page_Select("VRCSpoofer");
    }
    private void VRCSpoofer_GetLicenseButton_Click(object sender, RoutedEventArgs? e)
    {
        if (Config.SendAnalytics)
            Analytics.Client.Track(Environment.MachineName, "VRCSpoofer Get License");
        OpenURL("https://hdzero.mysellix.io/pay/005854-950d2e0567-b6a173");

    }
    private async void VRCSpoofer_DownloadButton_Click(object sender, RoutedEventArgs? e)
    {
        if (ButtonPaused(VRCSpoofer_DownloadButton))
            return;
        ShowNotification("Started Download For ZER0's Spoofer!");

        if (Config.SendAnalytics)
            Analytics.Client.Track(Environment.MachineName, "Spoofer Downloaded");
        PauseButton(VRCSpoofer_DownloadButton, "Installing");

        await Task.Run(() =>
        {
            try
            {
                OptionalSoftwareManager.DownloadSoftware("ZER0Spoofer.exe", "HWIDSpoofer");
                ShowNotification("Installing ZER0's Spoofer!");
                OptionalSoftwareManager.InstallSoftware("ZER0Spoofer.exe", "HWIDSpoofer");
            }
            catch { }
            try
            {
                ProcessStartInfo psi = new(Path.Combine(OptionalSoftwareManager.GetSoftwarePath("HWIDSpoofer"), "ZER0Spoofer.exe"))
                {
                    WorkingDirectory = OptionalSoftwareManager.GetSoftwarePath("HWIDSpoofer"),
                    UseShellExecute = true
                };
                Process.Start(psi);
            }
            catch { }
            ResumeButon(VRCSpoofer_DownloadButton);

        });

    }
}