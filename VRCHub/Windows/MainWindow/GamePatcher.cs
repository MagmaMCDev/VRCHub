using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Windows.Forms;
using static VRCHub.Common;
using Application = System.Windows.Application;
namespace VRCHub;

public partial class MainWindow
{
    private async Task<bool> CheckGameVersion()
    {
        if (!File.Exists(Config.VRChatInstallPath))
            return true;
        if (File.Exists(Path.Combine(new FileInfo(Config.VRChatInstallPath).Directory!.FullName, "VRChat_Data", "1606")))
            return true;
        
        NotifyIcon Notify = new()
        {
            Visible = true,
            Icon = SystemIcons.Warning,
            BalloonTipTitle = "Update Started",
            BalloonTipText = "This May Take Some Time Depending On Your System Specs",
        };
        Notify.ShowBalloonTip(3000);
        Notify.BalloonTipClosed += (sender, args) => Notify.Dispose();
        byte[] bytes = await api!.GetByteArrayAsync(GetServer("https://software.vrchub.site/GamePatch/VersionNumbers/1606/Game.zip"));

        if (MessageBox.Show("Game Patch Downloaded Automatically Install?\nPlease Close your game before continuing", "VRCHub", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning) == System.Windows.Forms.DialogResult.Cancel)
        {
            if (MessageBox.Show("Would you like to be asked later?", "VRCHub", MessageBoxButtons.YesNo, MessageBoxIcon.Information) == System.Windows.Forms.DialogResult.Yes)
                return true;
            File.WriteAllText(Path.Combine(new FileInfo(Config.VRChatInstallPath).Directory!.FullName, "VRChat_Data", "1606"), MD5Hash());
            return false;
        }

        ZipArchive archive = new(new MemoryStream(bytes));
        archive.ExtractToDirectory(new FileInfo(Config.VRChatInstallPath).Directory!.FullName, true);
        File.WriteAllText(Path.Combine(new FileInfo(Config.VRChatInstallPath).Directory!.FullName, "VRChat_Data", "1606"), MD5Hash());
        return true;
    }
}