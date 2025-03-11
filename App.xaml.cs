using System.Configuration;
using System.Data;
using System.Windows;
using System.Windows.Threading;

namespace VRCHub;
/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    [STAThread]
    public static void Main()
    {
        var app = new App();
        app.InitializeComponent();
        app.Run();
    }
    public static string? hwid                  = null;
    public static string? cpuProduct            = null;
    public static string? cpuSerial             = null;
    public static string? biosVendor            = null;
    public static string? biosVersion           = null;
    public static string? baseboardManufacturer = null;
    public static string? baseboardProduct      = null;
    public static string? baseboardSerial       = null;
    public static string? windowsVersion        = null;
    public static string? machinename           = null;
    public App()
    {
        DispatcherUnhandledException += App_DispatcherUnhandledException;

        SentrySdk.Init(o =>
        {
            o.Dsn = EasyAnalytics.APIKey;
            o.TracesSampleRate = 1.0;
            o.ProfilesSampleRate = 1.0;
            o.ServerName = "VRCHub/" + Common.VERSION;
        });
    }

    void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        SentrySdk.CaptureException(e.Exception);
    }
}

