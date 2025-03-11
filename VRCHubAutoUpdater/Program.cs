using System.Collections.Concurrent;
using System.IO.Compression;
using System.Net;
using System.Net.Http.Headers;
using System.Reflection.PortableExecutable;
using System.Text.RegularExpressions;

namespace VRCHubAutoUpdater;
public partial class Program
{
    private static bool Exit = false;
    private static void CurrentDomain_ProcessExit(object? sender, EventArgs e) =>
        Exit = true;

    private static readonly string LogFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "VRCHub", "ALP.log");
    private static readonly string VRChatCacheDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            @"..\LocalLow\VRChat\VRChat\Cache-WindowsPlayer"
        );
    private static DirectoryInfo GetWorldCacheDirectory(string CacheID)
    {
        var subdirectories = new DirectoryInfo(Path.Combine(VRChatCacheDirectory, CacheID)).GetDirectories();
        return subdirectories
            .OrderByDescending(d => d.LastWriteTime)
            .First();
    }
    public static (string name, string version) ParseWorldData(string filePath)
    {
        var records = ("", "");
        var dict = new Dictionary<string, string>();
        try
        {
            var lines = File.ReadAllLines(filePath);

            if (lines.Length < 2)
                return ("", "");

            var keys = lines[0].Split(',');
            var values = lines[1].Split(',');

            if (keys.Length != values.Length)
                return ("", "");

            for (var i = 0; i < keys.Length; i++)
                dict[keys[i].Trim().ToLower()] = values[i].Trim();
            records.Item1 = dict["name"];
            records.Item2 = dict["version"];
        }
        catch { }

        return records;
    }


    public static void Main()
    {
        AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExit;

        ALPProcessor processor = new();
        processor.OnDataswapProcessed += Processor_OnDataswapProcessed;
        processor.Start(LogFile);
        processor.Wait(TimeSpan.MaxValue);
    }

    private static readonly ConcurrentBag<string> WatchedDataswaps = new();

    private static void Processor_OnDataswapProcessed(string ID, string CacheID)
    {
        if (WatchedDataswaps.Contains(ID))
            return;
        WatchedDataswaps.Add(ID);

        var WorldData = ParseWorldData(Path.Combine(GetWorldCacheDirectory(CacheID).FullName, "WorldData.csv"));

        new Thread(() => WatcherThread(WorldData.name, WorldData.version, ID, CacheID)).Start();
    }
    private static async void WatcherThread(string Name, string Version, string ID, string CacheID)
    {
        Console.WriteLine($"Watching World: {Name}, Ver: {Version}, CacheID: {CacheID}");

        using var httpClient = new HttpClient(
            new HttpClientHandler()
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            });
        httpClient.DefaultRequestHeaders.CacheControl = new()
        {
            NoCache = true,
            NoStore = true,
        };
        httpClient.DefaultRequestHeaders.Pragma.Add(new("no-cache"));

        while (!Exit)
        {
            Thread.Sleep(5 * 1000);
            try
            {
                Console.WriteLine($"Checking World Version: {Name}");
                string cacheDirectory = GetWorldCacheDirectory(CacheID).FullName;

                string ver = PackageVersion().Match(
                    await httpClient.GetStringAsync($"https://datapacks.vrchub.site/{Name}/Package.json")
                ).Groups[1].Value;

                if (Version == ver)
                    continue;

                Console.WriteLine($"Found New Version For World: {Name}, Ver: {ver}");
                var content = await httpClient.GetByteArrayAsync($"https://datapacks.vrchub.site/{Name}/{Name}.dp");
                string tempZipFilePath = Path.Combine(cacheDirectory, $"{Name}.zip");
                await File.WriteAllBytesAsync(tempZipFilePath, content);
                ZipFile.ExtractToDirectory(tempZipFilePath, cacheDirectory, true);
                File.Delete(tempZipFilePath);

                Console.WriteLine($"Downloaded and extracted {Name}.dp to {cacheDirectory}");
                if (File.Exists(Path.Combine(cacheDirectory, "Package.json")))
                    File.Delete(Path.Combine(cacheDirectory, "Package.json"));

                Version = ver;
                
            }
            catch { }
        }
    }
    [GeneratedRegex(@"""Version"": ""(\d+\.\d+\.\d+)""", RegexOptions.Compiled)]
    public static partial Regex PackageVersion();
}
