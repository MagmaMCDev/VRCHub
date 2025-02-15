using System;
using System.IO;
using System.IO.Compression;
using Spectre.Console;
using System.Threading;
using System.Linq;
using System.Text.Json;
namespace VRCDataMod
{
    internal class Program
    {
        public static string packPath = "";
        public static DataPack Pack;
        static void Main(string[] args)
        {
            try
            {
                packPath = args[0];
            }
            catch { }
            while (packPath == "")
            {
                var path = AnsiConsole.Ask<string>("[red]Enter DataPack Path[/]:").Replace("\"", "");
                try
                {
                    if (!path.ToUpper().EndsWith(".DP"))
                    {
                        AnsiConsole.MarkupLine("[red]Expected[/] '[OrangeRed1].DP[/]'");
                        throw new Exception();
                    }
                    else if (DataPack.ValidPack(File.ReadAllBytes(path)))
                        packPath = path;
                }
                catch
                {
                    AnsiConsole.MarkupLine("[RED]Invalid DataPack[/]!");
                    Thread.Sleep(2000);
                    Console.Clear();
                }
            }
            AnsiConsole.MarkupLine("[Lime]Valid DataPack Extracting[/]!");
            Thread.Sleep(1000);
            Pack = new DataPack(File.ReadAllBytes(packPath));
            DataPackage dataPackage = Pack.GetDataPackage();
            while (true)
            {
                Console.Clear();
                AnsiConsole.MarkupLine("[cyan]======================================[/]");
                AnsiConsole.MarkupLine($"[cyan]World[/]: {dataPackage.WorldName}");
                AnsiConsole.MarkupLine($"[cyan]WorldHash[/]: {dataPackage.WorldHash}");
                AnsiConsole.MarkupLine($"[cyan]Version[/]: {dataPackage.Version}");
                AnsiConsole.MarkupLine($"[cyan]Author[/]: {dataPackage.Author}");
                if (dataPackage.Discord != "")
                    AnsiConsole.MarkupLine($"[cyan]Discord[/]: {dataPackage.Discord}");
                if (dataPackage.Email != "")
                    AnsiConsole.MarkupLine($"[cyan]Email[/]: {dataPackage.Email}");
                AnsiConsole.MarkupLine("[cyan]======================================[/]");
                var Selection = AnsiConsole.Prompt(
                    new SelectionPrompt<string>()
                        .AddChoices(new[] {
                            "Install",
                            "Reset",
                            "Exit"
                        }));
                if (Selection == "Exit")
                    return;

                string contentpath = "";
                try
                {
                    contentpath = GetVRChatContentCachePath();
                }
                catch
                {
                    contentpath = Path.Combine(GetVRChatPath(), "Cache-WindowsPlayer\\");
                }
                if (Selection == "Reset")
                {
                    if (!Directory.Exists(Path.Combine(contentpath, dataPackage.WorldHash)))
                    {
                        AnsiConsole.MarkupLine($"[RED]Please Visit [/]`{dataPackage.WorldName}` [RED]Before Installing This Pack[/]!");
                        Thread.Sleep(2000);
                    }
                    else
                    {
                        DirectoryInfo dir = new DirectoryInfo(Path.Combine(contentpath, dataPackage.WorldHash));
                        var subDirectories = dir.GetDirectories();

                        if (subDirectories.Length == 0)
                        {
                            AnsiConsole.MarkupLine($"[RED]Please Visit [/]`{dataPackage.WorldName}` [RED]Before Installing This Pack[/]!");
                            Thread.Sleep(2000);
                        }
                        else
                        {
                            DirectoryInfo lastEditedDirectory = subDirectories
                                .OrderByDescending(d => d.LastWriteTime)
                                .FirstOrDefault();

                            if (lastEditedDirectory == null)
                            {
                                AnsiConsole.MarkupLine($"[RED]Please Visit [/]`{dataPackage.WorldName}` [RED]Before Installing This Pack[/]!");
                                Thread.Sleep(2000);
                            }
                            else
                            {
                                File.Delete(Path.Combine(lastEditedDirectory.FullName, "__data"));

                                AnsiConsole.MarkupLine($"[Lime]Reset World Data[/]!");
                                Thread.Sleep(2000);
                            }
                        }
                        Thread.Sleep(2000);
                        continue;
                    }
                }
                if (!Directory.Exists(Path.Combine(contentpath, dataPackage.WorldHash)))
                {
                    AnsiConsole.MarkupLine($"[RED]Please Visit [/]`{dataPackage.WorldName}` [RED]Before Installing This Pack[/]!");
                    Thread.Sleep(2000);
                }
                else
                {
                    DirectoryInfo dir = new DirectoryInfo(Path.Combine(contentpath, dataPackage.WorldHash));
                    var subDirectories = dir.GetDirectories();

                    if (subDirectories.Length == 0)
                    {
                        AnsiConsole.MarkupLine($"[RED]Please Visit [/]`{dataPackage.WorldName}` [RED]Before Installing This Pack[/]!");
                        Thread.Sleep(2000);
                    }
                    else
                    {
                        DirectoryInfo lastEditedDirectory = subDirectories
                            .OrderByDescending(d => d.LastWriteTime)
                            .FirstOrDefault();

                        if (lastEditedDirectory == null)
                        {
                            AnsiConsole.MarkupLine($"[RED]Please Visit [/]`{dataPackage.WorldName}` [RED]Before Installing This Pack[/]!");
                            Thread.Sleep(2000);
                        }
                        else
                        {

                            byte[] data = Pack.GetDataBytes();
                            string targetFilePath = Path.Combine(lastEditedDirectory.FullName, "__data");
                            File.WriteAllBytes(targetFilePath, data);

                            AnsiConsole.MarkupLine($"[GREEN]Pack installed successfully![/]");
                            Thread.Sleep(2000);
                        }
                    }
                }
            }
        }

        public static string GetVRChatPath() =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),"..\\LocalLow\\VRChat\\VRChat\\");
        public static VRCConfig GetVRChatConfig()
        {
            string ConfigData = File.ReadAllText(GetVRChatPath() + "config.json");
            return JsonSerializer.Deserialize<VRCConfig>(ConfigData);
        }
        public static string GetVRChatContentCachePath()
        {
            VRCConfig Conf = GetVRChatConfig();
            if (string.IsNullOrWhiteSpace(Conf.cache_directory))
                throw new ArgumentNullException();
            return Path.Combine(Conf.cache_directory, "Cache-WindowsPlayer");
        }
    }
    public class VRCConfig
    {
        public int cache_expire_delay
        {
            get; set;
        } = 0;
        public int cache_size
        {
            get; set;
        } = 0;
        public string cache_directory
        {
            get; set;
        } = "";
        public int fpv_steadycam_fov
        {
            get; set;
        } = 0;
        public int camera_res_height
        {
            get; set;
        } = 0;
        public int camera_res_width
        {
            get; set;
        } = 0;
    }

    public class DataPack
    {
        private byte[] _packData;

        public DataPack(byte[] packData)
        {
            _packData = packData;
        }

        public static bool ValidPack(byte[] Pack)
        {
            try
            {
                using (var memoryStream = new MemoryStream(Pack))
                using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Read))
                {
                    bool hasPackageJson = false;
                    bool hasData = false;

                    foreach (var entry in archive.Entries)
                    {
                        if (entry.FullName == "package.json")
                            hasPackageJson = true;
                        if (entry.FullName == "__data")
                            hasData = true;
                    }

                    return hasPackageJson && hasData;
                }
            }
            catch
            {
                return false;
            }
        }

        public byte[] GetDataBytes()
        {
            using (var memoryStream = new MemoryStream(_packData))
            using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Read))
            {
                var dataEntry = archive.GetEntry("__data");
                if (dataEntry != null)
                {
                    using (var dataStream = dataEntry.Open())
                    using (var dataMemoryStream = new MemoryStream())
                    {
                        dataStream.CopyTo(dataMemoryStream);
                        return dataMemoryStream.ToArray();
                    }
                }
                else
                {
                    throw new Exception("__data entry not found in the ZIP archive.");
                }
            }
        }

        public DataPackage GetDataPackage()
        {
            using (var memoryStream = new MemoryStream(_packData))
            using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Read))
            {
                var packageEntry = archive.GetEntry("package.json");
                if (packageEntry != null)
                {
                    using (var packageStream = packageEntry.Open())
                    using (var reader = new StreamReader(packageStream))
                    {
                        var packageJson = reader.ReadToEnd();
                        return JsonSerializer.Deserialize<DataPackage>(packageJson);
                    }
                }
                else
                {
                    throw new Exception("package.json entry not found in the ZIP archive.");
                }
            }
        }
    }

    public class DataPackage
    {
        public string WorldName { get; set; } = "";
        public string WorldHash { get; set; } = "";
        public string Version { get; set; } = "";
        public string Author { get; set; } = "";
        public string Discord { get; set; } = "";
        public string Email { get; set; } = "";
    }

}
