using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using Segment;
using VRCHub.Resources;
using static VRCHub.Common;

namespace VRCHub;

public partial class MainWindow
{
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
        Page_Select("SplashScreen");
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
}