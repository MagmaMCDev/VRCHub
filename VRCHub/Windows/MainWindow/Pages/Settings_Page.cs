using Microsoft.Win32;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace VRCHub;

public partial class MainWindow
{
    private void SettingsButton_Click(object sender, RoutedEventArgs? e)
    {
        Page_Select("Settings");
    }

    private void Settings_VRCPath_TextChanged(object sender, TextChangedEventArgs e)
    {
        Config.VRChatInstallPath = Settings_VRCPath.Text;
        var exists = File.Exists(Config.VRChatInstallPath);
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
    private static bool ShowDeletedPacks = false;
    private void Settings_ShowDeleted_Click(object sender, RoutedEventArgs e)
    {
        ShowDeletedPacks = Settings_ShowDeleted.IsChecked ?? false;
    }

}