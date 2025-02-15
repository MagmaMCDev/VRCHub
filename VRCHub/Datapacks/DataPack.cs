using System.IO.Compression;
using System.IO;
using Newtonsoft.Json;
using VRCHub.Models;
using System.Net.Http;
using System.Windows;

namespace VRCHub;

public class DataPack
{
    private readonly byte[] _packData;

    public DataPack(byte[] packData)
    {
        _packData = packData;
    }

    public static bool ValidPack(byte[] Pack)
    {
        try
        {
            using var memoryStream = new MemoryStream(Pack);
            using var archive = new ZipArchive(memoryStream, ZipArchiveMode.Read);
            var hasPackageJson = false;
            var hasData = false;
            
            foreach (var entry in archive.Entries)
            {
                if (entry.FullName == "package.json")
                    hasPackageJson = true;
                if (entry.FullName == "__data")
                    hasData = true;
            }
            
            return hasPackageJson && hasData;
            
        }
        catch
        {
            return false;
        }
    }

    public byte[] GetDataBytes()
    {
        using var memoryStream = new MemoryStream(_packData);
        using var archive = new ZipArchive(memoryStream, ZipArchiveMode.Read);
        var dataEntry = archive.GetEntry("__data");
        if (dataEntry != null)
        {
            using var dataStream = dataEntry.Open();
            using var dataMemoryStream = new MemoryStream();
            dataStream.CopyTo(dataMemoryStream);
            return dataMemoryStream.ToArray();
            
        }
        else
        {
            throw new Exception("__data entry not found in the ZIP archive.");
        }
        
    }

    public DataPackage GetDataPackage()
    {
        using var memoryStream = new MemoryStream(_packData);
        using var archive = new ZipArchive(memoryStream, ZipArchiveMode.Read);
        var packageEntry = archive.GetEntry("package.json");
        if (packageEntry != null)
        {
            using var packageStream = packageEntry.Open();
            using var reader = new StreamReader(packageStream);
            var packageJson = reader.ReadToEnd();
            return JsonConvert.DeserializeObject<DataPackage>(packageJson)!;
        }
        else
        {
            throw new Exception("package.json entry not found in the ZIP archive.");
        }
        
    }

    public bool Install()
    {
        try
        {
            var contentpath = VRCConfig.GetContentCachePath();
            using HttpClient client = new();
            client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:91.0) Gecko/20100101 Firefox/91.0");
            DataPackage Package = GetDataPackage();
            DirectoryInfo dir = new(Path.Combine(contentpath, Package.WorldHash));
            var subDirectories = dir.GetDirectories();
            DirectoryInfo lastEditedDirectory = subDirectories
                .OrderByDescending(d => d.LastWriteTime)
                .FirstOrDefault()!;

            var data = GetDataBytes();
            var targetFilePath = Path.Combine(lastEditedDirectory.FullName, "__data");
            File.WriteAllBytes(targetFilePath, data);
            return true;
        }
        catch (Exception ex)
        {
            SimpleLogger.Error(ex.ToString());
        }
        return false;
    }
    public bool Uninstall()
    {
        try
        {
            var contentpath = VRCConfig.GetContentCachePath();                              
            DataPackage Package = GetDataPackage();
            Directory.Delete(Path.Combine(contentpath, Package.WorldHash), true);
            return true;
        }
        catch(Exception ex)
        {
            MessageBox.Show("Uninstallation Error", ex.Message);
        }
        return false;
    }
}