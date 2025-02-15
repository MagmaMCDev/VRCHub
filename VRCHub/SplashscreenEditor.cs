using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32;
using System.Windows.Media.Imaging;

namespace VRCHub;
public static class SplashscreenEditor
{
    public static BitmapImage? SelectImageFromExplorer()
    {
        OpenFileDialog openFileDialog = new OpenFileDialog();
        openFileDialog.Filter = "Image files (*.png;*.jpg;*.jpeg)|*.png;*.jpg;*.jpeg";
        if (openFileDialog.ShowDialog() == true)
        {
            BitmapImage bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = new Uri(openFileDialog.FileName);
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.EndInit();
            return bitmap;
        }
        return null;
    }
    public static void ScaleImage(BitmapImage image, out BitmapImage imageout)
    {
        int width = 800;
        int height = 450;

        BitmapSource bitmapSource = new TransformedBitmap(image, new System.Windows.Media.ScaleTransform((double)width / image.PixelWidth, (double)height / image.PixelHeight));
        PngBitmapEncoder encoder = new();
        encoder.Frames.Add(BitmapFrame.Create(bitmapSource));

        using MemoryStream memoryStream = new MemoryStream();
        encoder.Save(memoryStream);
        memoryStream.Seek(0, SeekOrigin.Begin);
        BitmapImage scaledImage = new BitmapImage();
        scaledImage.BeginInit();
        scaledImage.StreamSource = memoryStream;
        scaledImage.CacheOption = BitmapCacheOption.OnLoad;
        scaledImage.EndInit();
        imageout = scaledImage;
    }

    public static BitmapImage ScaleImage(BitmapImage image)
    {
        ScaleImage(image, out BitmapImage imageout);
        return imageout;
    }

    public static bool SaveImage(BitmapImage image)
    {
        try
        {
            PngBitmapEncoder encoder = new();
            encoder.Frames.Add(BitmapFrame.Create(image));

            using FileStream fileStream = new FileStream(SplashScreenPath, FileMode.Create);
            encoder.Save(fileStream);
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error saving image: " + ex.Message);
            return false;
        }
    }
    public static string SplashScreenPath => Path.Combine(new FileInfo(Config.VRChatInstallPath).Directory!.FullName, $"EasyAntiCheat\\SplashScreen.png");
}
