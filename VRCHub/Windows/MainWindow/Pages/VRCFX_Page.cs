using System.Diagnostics;
using System.Windows;
using static VRCHub.ButtonManager;
using Segment;
using System.IO;

namespace VRCHub;

public partial class MainWindow
{
    private void VRCFXButton_Click(object sender, RoutedEventArgs? e)
    {
        Page_Select("VRCFX");
    }
    private async void VRCFX_DownloadButton_Click(object sender, RoutedEventArgs? e)
    {
        if (ButtonPaused(VRCFX_DownloadButton))
            return;

        PauseButton(VRCFX_DownloadButton, "Installing");
        ShowNotification("Started Download For VRCFX!");
        await Task.Run(() =>
        {
            try
            {
                OptionalSoftwareManager.DownloadSoftware("VRCFX.zip", "VRCFX", "VRCFX.exe");
                ShowNotification("Installing VRCFX!");
                OptionalSoftwareManager.InstallSoftware("VRCFX.exe", "VRCFX");
            }
            catch { }

            try
            {
                ProcessStartInfo psi = new(Path.Combine(OptionalSoftwareManager.GetSoftwarePath("VRCFX"), "VRCFX.exe"))
                {
                    WorkingDirectory = OptionalSoftwareManager.GetSoftwarePath("VRCFX"),
                    UseShellExecute = true
                };
                Process.Start(psi);
            }
            catch { }
            ResumeButon(VRCFX_DownloadButton);
        });
    }

    private void VRCFX_GetLicenseButton_Click(object sender, RoutedEventArgs? e)
    {
        MessageBox.Show("You can Purchase a key by creating a ticket in the discord.");
    }
}