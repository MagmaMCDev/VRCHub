using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Net.Http.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Media.Animation;
using ZER0.VRChat.Patch;
using Segment;
using static VRCHub.Common;
using static VRCHub.ButtonManager;

using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using Path = System.IO.Path;
using SaveFileDialog = Microsoft.Win32.SaveFileDialog;
using Control = System.Windows.Controls.Control;
using System.Windows.Threading;
using VRCHub.Resources;
using VRCHub.Windows;
using System.Runtime.InteropServices;
namespace VRCHub;
/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    #region ServerAPI
    private bool ServerInitilized = false;
    private static ServerAPI? api;
    internal static ServerAPI API => api!;
    internal static string GetServer(string server) => ServerAPI.GetServer(server);
    private void InitilizeServerAPI()
    {
        api = new ServerAPI();
    }
    private void CheckServer(string server, byte index)
    {
        if (!API.CheckServer(server))
        {
            Application.Current.Dispatcher.Invoke(_splashScreen!.StartFadeOut);
            MessageBoxResult result = MessageBox.Show(
                $"Failed To Check Server Status On Server {index}/{ServerAPI.Servers.Length}" +
                $"\r\nDo You Want To Use Our Server Proxy?" +
                $"\r\nIf This Continues To Happen Please Try And Use A VPN!", "ServerAPI - Status",
                MessageBoxButton.OKCancel, MessageBoxImage.Warning);
            if (result == MessageBoxResult.OK)
                CheckProxyServer();
            else
            {
                Environment.Exit(2);
                return;
            }
        }
    }
    private void CheckProxyServer()
    {
        ServerAPI.usingProxy = true;
        if (!API.CheckServer("https://magmamc.dev/ServerProxy/vrchub/api/2/Status"))
        {
            DialogResult proxyresult = System.Windows.Forms.MessageBox.Show(
                $"Failed To Check Server Status On Server Proxy" +
                $"\r\nIf This Continues To Happen Please Try And Use A VPN!", "ServerAPI - Proxy Status",
                MessageBoxButtons.RetryCancel, MessageBoxIcon.Warning);
            if (proxyresult == System.Windows.Forms.DialogResult.Retry)
            {
                System.Windows.Forms.Application.Restart();
                Environment.Exit(3);
                return;
            }
            else
            {
                Environment.Exit(2);
                return;
            }
        }
        Application.Current.Dispatcher.Invoke(_splashScreen!.StartFadeIn);
    }
    #endregion

    private SplashScreen? _splashScreen;
    public MainWindow()
    {
        SetupConsole(Environment.GetCommandLineArgs());
        StartAnalytics();
        Hide();
        InitilizeServerAPI();
        InitializeSplashScreen();
        InitializeMainWindowAsync();
        SetupEvents();
    }

    private async void InitializeSplashScreen()
    {
        string Base = SplashScreen.BaseText;
        SplashScreen.BaseText = $"Checking Server Status 0/{ServerAPI.Servers.Length}";
        _splashScreen = SplashScreen.Create();
        await Task.Run(() =>
        {
            byte index = 0;
            foreach (var server in ServerAPI.Servers)
            {
                index++;
                CheckServer(server, index);
                _splashScreen.SetText($"Checking Server Status {index}/{ServerAPI.Servers.Length}");
            }
            SplashScreen.BaseText = Base;
            _splashScreen.StartTextAnimation();
            ServerInitilized = true;
        }).WaitAsync(new CancellationToken());
    }
    private async void InitializeMainWindowAsync()
    {
        while (!ServerInitilized)
            await Task.Delay(50);
        try
        {
            await CheckApplicationVersion();

            Version.Content += VERSION.ToString();
            VRCFX_Promotion1.Source = GetImageSource(AppResources.VRCFX_Example1);
            VRCFX_Promotion2.Source = GetImageSource(AppResources.VRCFX_Example2);
            VRCSpoofer_Promotion1.Source = GetImageSource(AppResources.Spoofer_Example1);

            Config.LoadConfig();

            Settings_SendAnalytics.IsChecked = Config.SendAnalytics;
            Settings_VRCPath.Text = Config.VRChatInstallPath;
            if (Config.SendAnalytics)
                Analytics.Client.Track(Environment.MachineName, "Application Loaded");
            Config.SaveConfig();

            bool exists = File.Exists(Config.VRChatInstallPath);
            VRCFXButton.IsEnabled = exists;
            VRCSpooferButton.IsEnabled = exists;
            DatapacksButton.IsEnabled = exists;
            SplashScreenButton.IsEnabled = exists;
            QuickLauncherButton.IsEnabled = exists;

            if (!exists)
            {
                SettingsButton_Click(this, null);
                MessageBox.Show("VRChat Location Failed To Auto Locate Please Set The VRChat Location To Continue", "VRCHub", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            else
                Datapacks_Click(this, null);

            UpdateSplashScreen();

            // Data packs loading
            await LoadDataPacksAsync();
        }
        finally
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
                KERNAL32.SetProcessWorkingSetSize32Bit(Process.GetCurrentProcess().Handle, -1, -1);
        }
    }

    private void SetupEvents()
    {
        AccountProfile.NotificationEvent += ShowNotification;
    }
    public static void OpenURL(string URL)
    {
        ProcessStartInfo PSI = new()
        {
            FileName = URL,
            UseShellExecute = true
        };
        Process.Start(PSI);
    }

    private void OSCTools_Button_Click(object sender, RoutedEventArgs e)
    {

    }
    private void DiscordButton_Click(object sender, RoutedEventArgs? e)
    {
        if (Config.SendAnalytics)
            Analytics.Client.Track(Environment.MachineName, nameof(DiscordButton_Click));
        OpenURL("https://dc.vrchub.site");
    }
}