using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using Segment;

namespace VRCHub;

internal static class Common
{
    public static Version VERSION = new("0.5.1");

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
