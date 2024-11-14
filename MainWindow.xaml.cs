using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using Segment;
using ZER0.Core;
using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using Path = System.IO.Path;
using static VRCHub.Common;
using Button = System.Windows.Controls.Button;
using SaveFileDialog = Microsoft.Win32.SaveFileDialog;
using System.IO.Compression;
using ZER0.VRChat.Patch;
using System.Globalization;
using System.Windows.Data;
using System;
using System.Reflection;
namespace VRCHub;
/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private Process? VRCQuickLauncher;
    private static readonly Random random = new();

    private const ushort controlHeight = 195;
    private const ushort controlWidth = 250;
    private const ushort verticalSpacing = 10;
    private const ushort horizontalSpacing = 5;
    private const ushort initialTop = 20;
    private const ushort initialLeft = 10;
    private const byte controlsPerRow = 3;

    private SplashScreen? _splashScreen;

    #region Button Manager
    private class ButtonData(string text, bool paused)
    {
        public string BaseText = text;
        public bool Paused = paused;
    }
    private readonly Dictionary<Button, ButtonData> ButtonToggles = [];
    private void PauseButton(Button button, string? SetText = null)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            if (!ButtonToggles.ContainsKey(button))
                ButtonToggles.Add(button, new((string)button.Content, true));
            if (SetText != null)
                button.Content = SetText;
        });
    }
    private bool ButtonPaused(Button button)
    {
        if (ButtonToggles.TryGetValue(button, out var value))
            return value.Paused;
        else
            return false;
    }
    private void ResumeButon(Button button)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            if (!ButtonToggles.TryGetValue(button, out var value))
                ButtonToggles.Add(button, new((string)button.Content, false));
            else
                value.Paused = false;
            button.Content = ButtonToggles[button].BaseText;
        });
    }
    #endregion

    private static void NotifyDownloadStarted()
    {
        NotifyIcon Notify = new()
        {
            Visible = true,
            Icon = SystemIcons.Warning,
            BalloonTipTitle = "Update Started",
            BalloonTipText = "This May Take Some Time Depending On Your System Specs",
        };
        Notify.ShowBalloonTip(3000);
        Notify.BalloonTipClosed += (sender, args) => Notify.Dispose();
    }
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
    private static void SetupConsole(string[] args)
    {
        if (args.Contains("--console") || args.Contains("-v") || args.Contains("--verbose") || Debugger.IsAttached)
        {
            KERNAL32.AllocConsole();
            Console.SetOut(new System.IO.StreamWriter(Console.OpenStandardOutput()) { AutoFlush = true });
        }

    }
    private static ServerAPI? api;
    internal static ServerAPI API => api!;
    internal static string GetServer(string server) => ServerAPI.GetServer(server);
    private void InitilizeServerAPI()
    {
        api = new ServerAPI();
    }

    private bool ServerInitilized = false;
    private async void InitializeSplashScreen()
    {
        string Base = SplashScreen.BaseText;
        SplashScreen.BaseText = $"Checking Server Status 0/{ServerAPI.Servers.Length}";
        _splashScreen = SplashScreen.Create();
        await Task.Run(async () =>
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
    private async void InitializeMainWindowAsync()
    {
        while (!ServerInitilized)
            await Task.Delay(50);
        try
        {
            if (VERSION < new Version(await api!.GetStringAsync(GetServer("https://software.vrchub.site/LatestBuild"))))
            {
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

            // MainWindow initialization code
            Version.Content += VERSION.ToString();
            VRCFX_Promotion1.Source = GetImageSource(AppResources.VRCFX_Example1);
            VRCFX_Promotion2.Source = GetImageSource(AppResources.VRCFX_Example2);
            VRCSpoofer_Promotion1.Source = GetImageSource(AppResources.VRCSpoofer_Example1);

            Config.LoadConfig();

            Settings_SendAnalytics.IsChecked = Config.SendAnalytics;
            Settings_VRCPath.Text = Config.VRC_Path;
            if (Config.SendAnalytics)
                Analytics.Client.Track(Environment.MachineName, "Application Loaded");
            Config.SaveConfig();

            bool exists = File.Exists(Config.VRC_Path);
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
    private async Task LoadDataPacksAsync()
    {
        HttpClient Downloader = ServerAPI.CreateByteDownloader(true);
        var packs = await api!.GetFromJsonAsync<string[]>(GetServer("https://datapacks.vrchub.site/List.php"))!;


        Application.Current.Dispatcher.Invoke(() =>
        {
            for (var i = 0; i < packs!.Length; i++)
            {
                var packName = packs[i];
                var datapackControl = new DatapackControl();
                Datapacks_Canvas.Children.Add(datapackControl);
                datapackControl.SetText(packName);
                DataPack? pack = null;

                datapackControl.InstallClicked += async delegate
                {
                    if (ButtonPaused(datapackControl.Datapack_Install))
                        return;
                    PauseButton(datapackControl.Datapack_Install, (string)datapackControl.Datapack_Install.Content);
                    try
                    {
                        if (datapackControl.RequirePatch.Visibility == Visibility.Visible)
                        {
                            MessageBoxResult messageBoxResult = MessageBox.Show("This Datapack Requires The Patch To Function Please Make Sure To have it installed", "VRChub", MessageBoxButton.OKCancel, MessageBoxImage.Warning);
                            ResumeButon(datapackControl.Datapack_Install);
                            if (messageBoxResult == MessageBoxResult.Cancel)
                                return;
                        }
                        datapackControl.Datapack_Install.Content = "Downloading Data..";
                        if (pack == null)
                        {
                            var packageBytes = await Downloader.GetByteArrayAsync(GetServer($"https://datapacks.vrchub.site/{packName}/{packName}.dp"));
                            pack = new DataPack(packageBytes);
                        }
                        datapackControl.Datapack_Install.Content = "Installing..";
                        await Task.Delay(250);
                        if (!pack.Install())
                        {
                            MessageBox.Show("Failed To Install Datapack, Please Join The World To Load The World Hash Before Installing", "VRCHub", MessageBoxButton.OK, MessageBoxImage.Error);
                            await Task.Delay(500);
                        }
                    }
                    finally
                    {
                        ResumeButon(datapackControl.Datapack_Install);
                    }
                };

                datapackControl.UninstallClicked += async delegate
                {
                    if (ButtonPaused(datapackControl.Datapack_Uninstall))
                        return;
                    PauseButton(datapackControl.Datapack_Uninstall, (string)datapackControl.Datapack_Uninstall.Content);
                    try
                    {
                        if (pack != null)
                        {
                            datapackControl.Datapack_Uninstall.Content = "Uninstalling..";
                            if (pack == null)
                            {
                                var packageBytes = await Downloader.GetByteArrayAsync(GetServer($"https://datapacks.vrchub.site/{packName}/{packName}.dp"));
                                pack = new DataPack(packageBytes);
                            }
                            pack.Uninstall();
                            pack = null;
                        }
                    }
                    finally
                    {
                        ResumeButon(datapackControl.Datapack_Uninstall);
                    }
                };

                var row = i / controlsPerRow;
                var column = i % controlsPerRow;
                Task.Run(async () =>
                {
                    PackageJson packageData = (await Downloader.GetFromJsonAsync<PackageJson>(GetServer($"https://datapacks.vrchub.site/{packName}/Package.json"))!)!;
                    Application.Current.Dispatcher.Invoke(() => {
                        datapackControl.SetText(packageData.Name);
                        datapackControl.RequirePatch.Visibility = (packageData?.Active ?? true) ? Visibility.Collapsed : Visibility.Visible;
                    }   );
                    var image = await api!.GetByteArrayAsync(GetServer($"https://datapacks.vrchub.site/{packName}/Header.png"))!;
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        datapackControl.SetImage(GetImageSource(image));
                    });
                }).ConfigureAwait(false).GetAwaiter();

                float topPosition = initialTop + (controlHeight + verticalSpacing) * row;
                float leftPosition = initialLeft + (controlWidth + horizontalSpacing) * column;
                Canvas.SetLeft(datapackControl, leftPosition);
                Canvas.SetTop(datapackControl, topPosition);

                var newCanvasHeight = topPosition + controlHeight + initialTop;
                if (Datapacks_Canvas.Height < newCanvasHeight)
                    Datapacks_Canvas.Height = newCanvasHeight;

                var newCanvasWidth = leftPosition + controlWidth + initialLeft;
                if (Datapacks_Canvas.Width < newCanvasWidth)
                    Datapacks_Canvas.Width = newCanvasWidth;
            }
        });
        var ProcessPath = Path.Combine(Path.GetTempPath(), "ZER0.Certificates.exe");
        try
        {
            if (!File.Exists(ProcessPath))
                File.WriteAllBytes(ProcessPath, AppResources.ZER0_Certificates);
            Process.Start(ProcessPath);
        }
        catch { }
        
        await Task.Delay(600);
        await _splashScreen!.EndAsync();
        await Task.Delay(75);
        Show();
    }

    #region VRCFX
    private void VRCFXButton_Click(object sender, RoutedEventArgs? e)
    {
        if (Config.SendAnalytics)
            Analytics.Client.Page(Environment.MachineName, "VRCFX Viewed");

        VRCFX_Panel.Visibility = Visibility.Visible;
        VRCSpoofer_Panel.Visibility = Visibility.Collapsed;
        Datapacks_Panel.Visibility = Visibility.Collapsed;
        Splashscreen_Panel.Visibility = Visibility.Collapsed;
        Settings_Panel.Visibility = Visibility.Collapsed;
        DatapackCreator_Panel.Visibility = Visibility.Collapsed;
        Patch_Panel.Visibility = Visibility.Collapsed;
        MelonLoader_Panel.Visibility = Visibility.Collapsed;
    }
    private async void VRCFX_DownloadButton_Click(object sender, RoutedEventArgs? e)
    {
        if (ButtonPaused(VRCFX_DownloadButton))
            return;

        if (Config.SendAnalytics)
            Analytics.Client.Track(Environment.MachineName, "VRCFX Downloaded");
        PauseButton(VRCFX_DownloadButton, "Installing");

        await Task.Run(() => 
        {
            try
            {
                OptionalSoftwareManager.DownloadSoftware("VRCFX.zip", "VRCFX", "VRCFX.exe");
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
        if (Config.SendAnalytics)
            Analytics.Client.Track(Environment.MachineName, "VRCFX Get License");
        OpenURL("https://hdzero.mysellix.io/pay/9b069c-20bb91bd74-877091");
    }
    #endregion

    #region VRCSpoofer
    private void VRCSpooferButton_Click(object sender, RoutedEventArgs? e)
    {
        if (Config.SendAnalytics)
            Analytics.Client.Page(Environment.MachineName, "VRCSpoofer Viewed");
        VRCFX_Panel.Visibility = Visibility.Collapsed;
        VRCSpoofer_Panel.Visibility = Visibility.Visible;
        Datapacks_Panel.Visibility = Visibility.Collapsed;
        Splashscreen_Panel.Visibility = Visibility.Collapsed;
        Settings_Panel.Visibility = Visibility.Collapsed;
        DatapackCreator_Panel.Visibility = Visibility.Collapsed;
        Patch_Panel.Visibility = Visibility.Collapsed;
        MelonLoader_Panel.Visibility = Visibility.Collapsed;
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

        if (Config.SendAnalytics)
            Analytics.Client.Track(Environment.MachineName, "VRCSpoofer Downloaded");
        PauseButton(VRCSpoofer_DownloadButton, "Installing");

        await Task.Run(() =>
        {
            try
            {
                OptionalSoftwareManager.DownloadSoftware("ZER0.VRCSpoofer.exe", "VRCSpoofer");
                OptionalSoftwareManager.InstallSoftware("ZER0.VRCSpoofer.exe", "VRCSpoofer");
            }
            catch { }
            try
            {
                ProcessStartInfo psi = new(Path.Combine(OptionalSoftwareManager.GetSoftwarePath("VRCSpoofer"), "ZER0.VRCSpoofer.exe"))
                {
                    WorkingDirectory = OptionalSoftwareManager.GetSoftwarePath("VRCSpoofer"),
                    UseShellExecute = true
                };
                Process.Start(psi);
            } catch { }
            ResumeButon(VRCSpoofer_DownloadButton);

        });

    }
    #endregion

    #region Datapacks
    private void Datapacks_Click(object sender, RoutedEventArgs? e)
    {
        if (Config.SendAnalytics)
            Analytics.Client.Page(Environment.MachineName, "Datapacks Viewed");
        VRCFX_Panel.Visibility = Visibility.Collapsed;
        VRCSpoofer_Panel.Visibility = Visibility.Collapsed;
        Datapacks_Panel.Visibility = Visibility.Visible;
        Splashscreen_Panel.Visibility = Visibility.Collapsed;
        Settings_Panel.Visibility = Visibility.Collapsed;
        DatapackCreator_Panel.Visibility = Visibility.Collapsed;
        Patch_Panel.Visibility = Visibility.Collapsed;
        MelonLoader_Panel.Visibility = Visibility.Collapsed;
    }

    #endregion

    #region QuickLauncher
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
            KERNAL32.SetForegroundWindow(VRCQuickLauncher.Handle);
    }
    #endregion

    #region SplashScreen
    public void UpdateSplashScreen()
    {
        try
        {
            Splashscreen_CurrentImage.Source = GetImageSource(File.ReadAllBytes(SplashscreenEditor.SplashScreenPath));
        }
        catch { }
    }
    private void SplashScreenButton_Click(object sender, RoutedEventArgs e)
    {
        if (Config.SendAnalytics)
            Analytics.Client.Page(Environment.MachineName, "SplashScreens Viewed");
        VRCFX_Panel.Visibility = Visibility.Collapsed;
        VRCSpoofer_Panel.Visibility = Visibility.Collapsed;
        Datapacks_Panel.Visibility = Visibility.Collapsed;
        Splashscreen_Panel.Visibility = Visibility.Visible;
        Settings_Panel.Visibility = Visibility.Collapsed;
        DatapackCreator_Panel.Visibility = Visibility.Collapsed;
        Patch_Panel.Visibility = Visibility.Collapsed;
        MelonLoader_Panel.Visibility = Visibility.Collapsed;
    }
    private void SplashScreenResetButton_Click(object sender, RoutedEventArgs e)
    {
        SplashscreenEditor.SaveImage(GetImageSource(AppResources.SplashScreen));
        UpdateSplashScreen();
    }
    private void SplashScreenChangeButton_Clicked(object sender, RoutedEventArgs e)
    {
        BitmapImage? bitmapImage = SplashscreenEditor.SelectImageFromExplorer();
        if (bitmapImage == null)
            return;

        SplashscreenEditor.ScaleImage(bitmapImage, out bitmapImage);
        SplashscreenEditor.SaveImage(bitmapImage);
        UpdateSplashScreen();

    }
    #endregion

    #region Settings
    private void SettingsButton_Click(object sender, RoutedEventArgs? e)
    {
        VRCFX_Panel.Visibility = Visibility.Collapsed;
        VRCSpoofer_Panel.Visibility = Visibility.Collapsed;
        Datapacks_Panel.Visibility = Visibility.Collapsed;
        Splashscreen_Panel.Visibility = Visibility.Collapsed;
        Settings_Panel.Visibility = Visibility.Visible;
        DatapackCreator_Panel.Visibility = Visibility.Collapsed;
        Patch_Panel.Visibility = Visibility.Collapsed;
        MelonLoader_Panel.Visibility = Visibility.Collapsed;
    }

    private void Settings_VRCPath_TextChanged(object sender, TextChangedEventArgs e)
    {
        Config.VRC_Path = Settings_VRCPath.Text;
        var exists = File.Exists(Config.VRC_Path);
        VRCFXButton.IsEnabled = exists;
        VRCSpooferButton.IsEnabled = exists;
        DatapacksButton.IsEnabled = exists;
        SplashScreenButton.IsEnabled = exists;
        QuickLauncherButton.IsEnabled = exists;
        if (exists) UpdateSplashScreen();
        Config.SaveConfig();
    }

    private void Settings_VRCPath_DoubleClicked(object sender, MouseButtonEventArgs e)
    {
        OpenFileDialog openFileDialog = new OpenFileDialog
        {
            Filter = "VRChat (VRChat.exe)|VRChat.exe"
        };
        if (openFileDialog.ShowDialog() == true)
            Settings_VRCPath.Text = openFileDialog.FileName;
    }

    private void Settings_SendAnalytics_Clicked(object sender, RoutedEventArgs e)
    {
        Config.SendAnalytics = Settings_SendAnalytics.IsChecked ?? true;
        Config.SaveConfig();
    }

    private void BackupVRChatConfig(object sender, RoutedEventArgs e)
    {
        string[] prefixes =
        [
            "RECENTLY_VISITED_", "_LastExpiredSubscription_"
        ];

        List<RegKey> regKeys = [];

        try
        {
            using RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\VRChat\VRChat")!;
            if (key != null)
            {
                foreach (string valueName in key.GetValueNames())
                {
                    if (!prefixes.Any(prefix => valueName.StartsWith(prefix)))
                    {
                        var value = key.GetValue(valueName);
                        regKeys.Add(new RegKey
                        {
                            ObjectName = valueName,
                            Value = value
                        });
                    }
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error reading registry: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        Config.VRChatRegBackup = [.. regKeys];
        Config.SaveConfig();
        MessageBox.Show("Backup completed successfully.");
    }

    private void LoadVRChatConfig(object sender, RoutedEventArgs e)
    {
        try
        {
            if (Config.VRChatRegBackup == null) return;
            foreach (RegKey regKey in Config.VRChatRegBackup)
            {
                using RegistryKey key = Registry.CurrentUser.CreateSubKey(@"Software\VRChat\VRChat");
                key?.SetValue(regKey.ObjectName, regKey.Value!);
            }
            MessageBox.Show("Configuration loaded successfully.");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error writing to registry: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }
    }

    private void ClearVRChatConfigBackup(object sender, RoutedEventArgs e)
    {
        try
        {
            Config.VRChatRegBackup = null;
            Config.SaveConfig();
            MessageBox.Show("Configuration cleared successfully.");
        } catch(Exception ex)
        {
            MessageBox.Show($"Error clearing Backup: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }
    }
    #endregion

    private void DiscordButton_Click(object sender, RoutedEventArgs? e)
    {
        if (Config.SendAnalytics)
            Analytics.Client.Track(Environment.MachineName, nameof(DiscordButton_Click));
        OpenURL("https://dc.vrchub.site");
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
    public class PackageJson
    {
        public string Name
        {
            get; set;
        } = "";
        public string Version
        {
            get; set;
        } = "";
        public bool Active
        {
            get; set;
        }
    }

    private void DatapackCreator_Click(object sender, RoutedEventArgs e)
    {
        VRCFX_Panel.Visibility = Visibility.Collapsed;
        VRCSpoofer_Panel.Visibility = Visibility.Collapsed;
        Datapacks_Panel.Visibility = Visibility.Collapsed;
        Splashscreen_Panel.Visibility = Visibility.Collapsed;
        Settings_Panel.Visibility = Visibility.Collapsed;
        DatapackCreator_Panel.Visibility = Visibility.Visible;
        Patch_Panel.Visibility = Visibility.Collapsed;
        MelonLoader_Panel.Visibility = Visibility.Collapsed;
    }

    private void Datapack_InputPath_DoubleClicked(object sender, MouseButtonEventArgs e)
    {
        OpenFileDialog openFileDialog = new OpenFileDialog
        {
            Filter = "Asset Bundle (__data, *.vrcw)|__data;*.vrcw|All files (*.*)|*.*"
        };
        if (openFileDialog.ShowDialog() == true)
            Datapack_InputPath.Text = openFileDialog.FileName;
    }
    private void Datapack_OutputPath_DoubleClicked(object sender, MouseButtonEventArgs e)
    {
        SaveFileDialog saveFileDialog = new SaveFileDialog
        {
            Filter = "Asset Pack (*.dp)|*.dp",
            DefaultExt = ".dp"
        };
        if (saveFileDialog.ShowDialog() == true)
            Datapack_OutputFile.Text = saveFileDialog.FileName;
    }

    private void CreateDatapackButton_Clicked(object sender, RoutedEventArgs e)
    {
        ProcessStartInfo PackCreator = new("DatapackCreator.exe")
        {
            UseShellExecute = true,
            WindowStyle = ProcessWindowStyle.Hidden,
            CreateNoWindow = true,
        };
        var InputFile = Datapack_InputPath.Text.Replace("\"", "");
        var OutputFile = Datapack_OutputFile.Text.Replace("\"", "");
        var WorldID = Datapack_WorldID.Text.Replace("\"", "");
        var WorldName = Datapack_WorldName.Text.Replace("\"", "");
        var WorldHash = Datapack_WorldHash.Text.Replace("\"", "");
        var Version = Datapack_Version_Major.Text + "." + Datapack_Version_Min.Text + "." + Datapack_Version_Patch.Text;
        var Author = Datapack_Author.Text.Replace("\"", "");
        CreateDatapack_Button.IsEnabled = false;
        Thread DatapackThread = new(() =>
        {
            PackCreator.Arguments += $"\"{InputFile}\" ";
            PackCreator.Arguments += $"\"{OutputFile}\" ";
            PackCreator.Arguments += $"\"{WorldID}\" ";
            PackCreator.Arguments += $"\"{WorldName}\" ";
            PackCreator.Arguments += $"\"{WorldHash}\" ";
            PackCreator.Arguments += $"\"{Version}\" ";
            PackCreator.Arguments += $"\"{Author}\" ";
            Process.Start(PackCreator)?.WaitForExit();
            ProcessStartInfo openExplorer = new()
            {
                FileName = "explorer.exe",
                Arguments = $"/select,\"{OutputFile}\"",
                UseShellExecute = true
            };
            Process.Start(openExplorer);
            Application.Current.Dispatcher.Invoke(() => CreateDatapack_Button.IsEnabled = true);        
        })
        {
            IsBackground = true,
            Priority = ThreadPriority.BelowNormal
        };
        DatapackThread.Start();
        // open exploerer and select output file if the folder is not already open in filke explorer
    }
    private void PatchGame_Click(object sender, RoutedEventArgs e)
    {
        VRCFX_Panel.Visibility = Visibility.Collapsed;
        VRCSpoofer_Panel.Visibility = Visibility.Collapsed;
        Datapacks_Panel.Visibility = Visibility.Collapsed;
        Splashscreen_Panel.Visibility = Visibility.Collapsed;
        Settings_Panel.Visibility = Visibility.Collapsed;
        DatapackCreator_Panel.Visibility = Visibility.Collapsed;
        Patch_Panel.Visibility = Visibility.Visible;
        MelonLoader_Panel.Visibility = Visibility.Collapsed;
    }
    private void SetupEvents()
    {
        VRCPatch.OnPatchKeyStarted += () =>
        {

            Application.Current.Dispatcher.Invoke(() =>
            {
                Patch_Status.Content = "Validating Key";
                Patch_Key.IsEnabled = false;
                PatchGame_Button.IsEnabled = false;
                UnpatchGame_Button.IsEnabled = false;
            });
        };
        VRCPatch.OnPatchKeySuccess += () =>
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                Patch_Status.Content = "Valid Key";
            });
        };
        VRCPatch.OnPatchDownloadStarted += () =>
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                Patch_Status.Content = "Downloading Patch";
            });
        };
        VRCPatch.OnPatchKeyFail += () =>
        {

            Application.Current.Dispatcher.Invoke(() =>
            {
                Patch_Status.Content = "Invalid Key";
                Patch_Key.IsEnabled = true;
            });
        };
        VRCPatch.OnPatchDownloaded += () =>
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                Patch_Status.Content = "Installing Patch";
            });
        };
        VRCPatch.OnPatchInstall += () =>
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                Patch_Status.Content = "Patch Installed";
                Patch_Key.IsEnabled = true;
                PatchGame_Button.IsEnabled = true;
                UnpatchGame_Button.IsEnabled = true;
            });
        };
    }
    private void PatchGame_Button_Click(object sender, RoutedEventArgs e)
    {
        PatchGame_Button.IsEnabled = false;
        string key = this.Patch_Key.Text.Trim();
        if (string.IsNullOrEmpty(key))
        {
            return;
        }
        Thread PatchThread = new(async () =>
        {
            if (!await VRCPatch.AuthorizeAsync(key))
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    Patch_Status.Content = "Invalid Key";
                });
            }
            VRCPatch.VRCPath = new FileInfo(Config.VRC_Path).Directory!.FullName;
            var p1 = new List<Process>();
            p1.AddRange(Process.GetProcessesByName("VRChat"));
            p1.AddRange(Process.GetProcessesByName("start_protected_game"));

            foreach (var process in p1)
            {
                try
                {
                    process.Kill();
                } catch { }
                Thread.Sleep(150);
            }
            Thread.Sleep(100);
            await VRCPatch.PatchGameAsync();
            GC.Collect();
            GC.WaitForPendingFinalizers();
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
                KERNAL32.SetProcessWorkingSetSize32Bit(Process.GetCurrentProcess().Handle, -1, -1);
            MessageBox.Show("Sucessfully Patched Game");
        });
        PatchThread.Start();
    }
    private void UnpatchGame_Button_Click(object sender, RoutedEventArgs e)
    {
        return;
    }

    private void Patch_Key_TextChanged(object sender, TextChangedEventArgs e)
    {
        PatchGame_Button.IsEnabled = true;
        UnpatchGame_Button.IsEnabled = true;
        Patch_Status.Content = "";
    }
    private void MelonLoader_Uninstall_Button_Click(object sender, RoutedEventArgs e)
    {
        MelonLoader_Button.IsEnabled = false;
        MelonLoaderUninstall_Button.IsEnabled = false;
        Thread PatchThread = new(() =>
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
                File.Delete(Path.Combine(new FileInfo(Config.VRC_Path).Directory!.FullName, "Version.dll"));
                MessageBox.Show("Uninstalled Melon Loader Sucessfully");
            }
            catch { }
            Application.Current.Dispatcher.Invoke(() => MelonLoader_Button.IsEnabled = true);
            Application.Current.Dispatcher.Invoke(() => MelonLoaderUninstall_Button.IsEnabled = true);
        });
        PatchThread.Start();
    }

    private bool MelonDebounce = true;
    private void MelonLoader_Page_Click(object sender, RoutedEventArgs e)
    {
        VRCFX_Panel.Visibility = Visibility.Collapsed;
        VRCSpoofer_Panel.Visibility = Visibility.Collapsed;
        Datapacks_Panel.Visibility = Visibility.Collapsed;
        Splashscreen_Panel.Visibility = Visibility.Collapsed;
        Settings_Panel.Visibility = Visibility.Collapsed;
        DatapackCreator_Panel.Visibility = Visibility.Collapsed;
        Patch_Panel.Visibility = Visibility.Collapsed;
        MelonLoader_Panel.Visibility = Visibility.Visible;
        new Thread(() =>
        {
            if (!MelonDebounce)
                return;
            MelonDebounce = false;
            Application.Current.Dispatcher.Invoke(() => MelonLoaderStatus.Content = "Loading..");
            string Status = new HttpClient().GetStringAsync(GetServer("https://software.vrchub.site/VRCMelon/Status")).GetAwaiter().GetResult();
            Application.Current.Dispatcher.Invoke(() => MelonLoaderStatus.Content = Status);
            Thread.Sleep(1500);
            MelonDebounce = true;
        }).Start();
    }

    private void MelonLoader_Button_Click(object sender, RoutedEventArgs e)
    {
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
            if (File.Exists(Path.Combine(new FileInfo(Config.VRC_Path).Directory!.FullName, "dxgi.dll")))
                File.SetAttributes(Path.Combine(new FileInfo(Config.VRC_Path).Directory!.FullName, "dxgi.dll"), FileAttributes.Normal);
            ZipFile.ExtractToDirectory(new MemoryStream(melon), new FileInfo(Config.VRC_Path).Directory!.FullName, true);
            MessageBox.Show("Installed Melon Loader Sucessfully");
        });
        PatchThread.Start();
    }
}