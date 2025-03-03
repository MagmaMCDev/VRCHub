using System.Drawing.Imaging;
using System.Drawing;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Media.Imaging;
using Segment;

namespace VRCHub;

internal static class Common
{
    public static Version VERSION = new("1.0.4");
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
        Analytics.VERSION = VERSION.ToString();
        Analytics.Initialize(EasyAnalytics.APIKey);
    }
}
