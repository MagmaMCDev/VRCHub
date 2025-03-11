using System.Drawing.Imaging;
using System.Drawing;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Media.Imaging;
using Segment;
using System.Diagnostics;

namespace VRCHub;

internal static class Common
{
    public static Version VERSION = new("1.1.0");
    public static string[] UList = ["1dc24ff4-0fd5-42f7-9507-750a29c79b8a", "689b66ec-2e70-44e5-afc3-1505ddecc440", "3d5513ca-4e58-452e-9173-db80c090e528", "e8d20dc5-8ed6-4e31-87c9-7e465aa42f6c", "cdffa0b9-de6d-402b-a38f-c633759bbf8f"];

    private static readonly Random random = new();
    private static readonly MD5 md5 = MD5.Create();
    private static ulong NoCollision = 0L;

    public static byte[] NonCollisionBytes
    {
        get
        {
            NoCollision++;
            if (NoCollision == ulong.MaxValue)
                NoCollision = 0;
            return Encoding.ASCII.GetBytes(random.Next(10_000_000, 99_999_999).ToString() + NoCollision);
        }
    }
    public static string MD5Hash()
    {
        byte[] hashBytes = md5.ComputeHash(NonCollisionBytes);

        StringBuilder sb = new StringBuilder();
        for (int i = 0; i < hashBytes.Length; i++)
            sb.Append(hashBytes[i].ToString("X2"));
        return sb.ToString();
    }


    public static byte[] BitmapToByteArray(Bitmap bitmap)
    {
        using MemoryStream ms = new MemoryStream();
        bitmap.Save(ms, ImageFormat.Png);
        return ms.ToArray();
    }
    public static async Task<BitmapImage> DownloadImage(string url)
    {
        try
        {
            using HttpClient client = ServerAPI.CreateByteDownloader();
            byte[] imageBytes = await client.GetByteArrayAsync(url);
            return GetImageSource(imageBytes);
        }
        catch (Exception ex)
        {
            SimpleLogger.Error($"Failed downloading image: {ex.Message}");
            return null;
        }
    }
    public static BitmapImage GetImageSource(Bitmap rawimage)
    {
        using var stream = new MemoryStream(BitmapToByteArray(rawimage));
        var image = new BitmapImage();
        image.BeginInit();
        image.CacheOption = BitmapCacheOption.OnLoad;
        image.StreamSource = stream;
        image.EndInit();
        return image;
    }
    public static BitmapImage GetImageSource(byte[] imageBytes)
    {
        using var stream = new MemoryStream(imageBytes);
        var image = new BitmapImage();
        image.BeginInit();
        image.CacheOption = BitmapCacheOption.OnLoad;
        image.StreamSource = stream;
        image.EndInit();
        return image;
    }
    internal static void StartAnalytics()
    {
        App.hwid = LibSerials.System_HWID();
        App.cpuProduct = LibSerials.CPU_Product();
        App.cpuSerial = LibSerials.CPU_Serial();
        App.biosVendor = LibSerials.BIOS_Vendor();
        App.biosVersion = LibSerials.BIOS_Version();
        App.baseboardManufacturer = LibSerials.Baseboard_Manufacturer();
        App.baseboardProduct = LibSerials.Baseboard_Product();
        App.baseboardSerial = LibSerials.Baseboard_Serial();
        App.windowsVersion = Environment.OSVersion.VersionString;
        App.machinename = Environment.MachineName;

        SimpleLogger.Debug($"HWID: {App.hwid}");
        SimpleLogger.Debug($"cpuProduct: {App.cpuProduct}");
        SimpleLogger.Debug($"biosVendor: {App.biosVendor}");
        SimpleLogger.Debug($"baseboardManufacturer: {App.baseboardManufacturer}");
        SimpleLogger.Debug($"baseboardProduct: {App.baseboardProduct}");
        SimpleLogger.Debug($"windowsVersion: {App.windowsVersion}");
        SimpleLogger.Debug($"machinename: {App.machinename}");

        
        var trace = SentrySdk.StartTransaction("application_started", "startup");
        trace.SetTag("windows_version", App.windowsVersion);
        trace.SetTag("machine_name", App.machinename);
        trace.SetTag("hwid", App.hwid);
        trace.SetTag("cpu_product", App.cpuProduct);
        trace.SetTag("bios_vendor", App.biosVendor);
        trace.SetTag("baseboard_manufacturer", App.baseboardManufacturer);
        trace.SetTag("baseboard_product", App.baseboardProduct);
        trace.Level = SentryLevel.Debug;
        trace.Finish();
    }
}
