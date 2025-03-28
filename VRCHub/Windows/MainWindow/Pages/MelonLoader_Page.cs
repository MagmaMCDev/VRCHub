using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Windows;

namespace VRCHub;

public partial class MainWindow
{
    private void MelonLoader_Uninstall_Button_Click(object sender, RoutedEventArgs e)
    {
        MelonLoader_Button.IsEnabled = false;
        MelonLoaderUninstall_Button.IsEnabled = false;
        new Thread(() =>
        {
            var p1 = new List<Process>();
            p1.AddRange(Process.GetProcessesByName("VRChat"));
            p1.AddRange(Process.GetProcessesByName("start_protected_game"));

            foreach (var process in p1)
            {
                try
                {
                    process.Kill();
                }
                catch { }
                Thread.Sleep(150);
            }
            Thread.Sleep(100);
            try
            {
                File.Delete(Path.Combine(new FileInfo(Config.VRChatInstallPath).Directory!.FullName, "Version.dll"));
                File.Delete(Path.Combine(new FileInfo(Config.VRChatInstallPath).Directory!.FullName, "dxgi.dll"));
                MessageBox.Show("Uninstalled Melon Loader Sucessfully");
            }
            catch { }
            Application.Current.Dispatcher.Invoke(() => MelonLoader_Button.IsEnabled = true);
            Application.Current.Dispatcher.Invoke(() => MelonLoaderUninstall_Button.IsEnabled = true);
        }).Start();
    }

    private bool MelonDebounce = true;
    private void MelonLoader_Page_Click(object sender, RoutedEventArgs e)
    {
        Page_Select("MelonLoader");
        new Thread(() =>
        {
            if (!MelonDebounce)
                return;
            MelonDebounce = false;
            Application.Current.Dispatcher.Invoke(() => MelonLoaderStatus.Content = "Loading..");
            string Status = new HttpClient().GetStringAsync(GetServer("https://software.vrchub.site/VRCMelon/Status")).GetAwaiter().GetResult();
            Application.Current.Dispatcher.Invoke(() => MelonLoaderStatus.Content = Status);
            Thread.Sleep(5000);
            MelonDebounce = true;
        }).Start();
    }

    private void MelonLoader_Button_Click(object sender, RoutedEventArgs e)
    {
        MessageBoxResult res = MessageBox.Show("It is highly not recommened to not install melonloader on windows\nFeel free to create a ticket in the discord if you have any questions", "MelonLoader", MessageBoxButton.OKCancel);
        if (res != MessageBoxResult.OK)
            return;
        Thread PatchThread = new(async () =>
        {

            var p1 = new List<Process>();
            p1.AddRange(Process.GetProcessesByName("VRChat"));
            p1.AddRange(Process.GetProcessesByName("start_protected_game"));

            foreach (var process in p1)
            {
                try
                {
                    process.Kill();
                }
                catch { }
                Thread.Sleep(150);
            }
            Thread.Sleep(100);
            byte[] melon = await new HttpClient().GetByteArrayAsync(GetServer("https://software.vrchub.site/VRCMelon/Release.zip"));
            if (File.Exists(Path.Combine(new FileInfo(Config.VRChatInstallPath).Directory!.FullName, "dxgi.dll")))
                File.SetAttributes(Path.Combine(new FileInfo(Config.VRChatInstallPath).Directory!.FullName, "dxgi.dll"), FileAttributes.Normal);
            ZipFile.ExtractToDirectory(new MemoryStream(melon), new FileInfo(Config.VRChatInstallPath).Directory!.FullName, true);
            MessageBox.Show("Installed Melon Loader Sucessfully");
        });
        PatchThread.Start();
    }
}