using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Windows;
using static VRCHub.Common;
namespace VRCHub;

public partial class MainWindow
{
    private async Task<bool> CheckApplicationVersion()
    {
        bool success = true;
        if (VERSION < new Version(await api!.GetStringAsync(GetServer("https://software.vrchub.site/LatestBuild"))))
        {
            success = false;
            var tempExe = Path.GetTempFileName() + ".exe";
            NotifyDownloadStarted();
            await Task.Run(async () =>
            {
                HttpClient downloader = ServerAPI.CreateByteDownloader(false);
                File.WriteAllBytes(tempExe, await downloader.GetByteArrayAsync(GetServer("https://software.vrchub.site/VRCHub%20Setup.exe")));
                downloader.Dispose();

                await Application.Current.Dispatcher.Invoke(async () =>
                {
                    await _splashScreen!.EndAsync();
                    ProcessStartInfo psi = new ProcessStartInfo(tempExe)
                    {
                        UseShellExecute = true,
                        Verb = "runas",
                        WorkingDirectory = new FileInfo(tempExe).DirectoryName
                    };
                    Process.Start(psi);
                    Environment.Exit(0);
                    return;
                });
            }).WaitAsync(new CancellationToken());
        }
        return success;
    }
}