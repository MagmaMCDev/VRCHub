using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;

namespace AssetLoggerPlugin;

internal class AssetLogger
{
    private static bool ConsoleLog = false;
    private static bool FileLog = false;
    private static StreamWriter? writer;

    [DllImport("kernel32.dll")]
    private static extern bool AllocConsole();
    [DllImport("KERNEL32.DLL", CallingConvention = CallingConvention.StdCall, EntryPoint = "SetProcessWorkingSetSize", SetLastError = true)]
    internal static extern bool SetProcessWorkingSetSize64Bit(IntPtr pProcess, long dwMinimumWorkingSetSize, long dwMaximumWorkingSetSize);

    public static void Output(string message)
    {
        if (ConsoleLog)
            Console.WriteLine(message);

        if (FileLog && writer != null)
            writer.WriteLine(message);
    }

    private static readonly string LogFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "VRCHub", "ALP.log");
    private static readonly string VRChatCacheDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            @"..\LocalLow\VRChat\VRChat\Cache-WindowsPlayer"
        );
    static void Main(string[] args)
    {
        #region Setup
        Directory.CreateDirectory(Path.GetDirectoryName(LogFile)!);

        if (args.Contains("--console"))
        {
            ConsoleLog = true;
            AllocConsole();
        }

        if (args.Contains("--log"))
        {
            FileLog = true;
            FileStream fileStream = new(LogFile, FileMode.Append, FileAccess.Write, FileShare.Read);
            writer = new StreamWriter(fileStream)
            {
                AutoFlush = true
            };
        }

        if (!ConsoleLog && !FileLog)
        {
            AllocConsole();
            Console.Title = "APL - Menu";
            Console.WriteLine("No arguments provided.");
            Console.WriteLine("[1] Console only");
            Console.WriteLine("[2] File only");
            Console.WriteLine("[3] Both");

            Console.Write("Mode: ");
            byte input;
            if (!byte.TryParse(Console.ReadKey().KeyChar.ToString(), out input) || input < 1 || input > 3)
            {
                Console.WriteLine();
                Console.WriteLine("Invalid input, please enter a number between 1 and 3.");
                return;
            }
            Console.WriteLine();
            if (input >= 2)
            {
                input -= 2;
                FileLog = true;
                FileStream fileStream = new(LogFile, FileMode.Append, FileAccess.Write, FileShare.Read);
                writer = new StreamWriter(fileStream)
                {
                    AutoFlush = true
                };
            }  
            if (input >= 1)
                ConsoleLog = true;
        }
        #endregion

        FileSystemWatcher watcher = new()
        {
            Path = VRChatCacheDirectory,
            Filter = "*",
            IncludeSubdirectories = true,
            EnableRaisingEvents = true
        };
        watcher.Created += OnFileCreated;
        Console.Title = "APL - Logging";
        while (true)
        {
            Thread.Sleep(100);
        }
    }
    private static readonly Queue<string> pendingBundleFiles = new();
    private static readonly object queueLock = new();
    private static async void OnFileCreated(object sender, FileSystemEventArgs e)
    {
        if (!Path.GetFileName(e.FullPath).Equals("__lock", StringComparison.OrdinalIgnoreCase))
            return;

        var BundleFile = Path.Combine(Path.GetDirectoryName(e.FullPath)!, "__data");

        lock (queueLock)
            pendingBundleFiles.Enqueue(BundleFile);

        string? CacheID;
        try
        {
            var fileInfo = new DirectoryInfo(BundleFile);
            CacheID = fileInfo.Parent!.Parent!.Name;
        }
        catch { return; }

        // avatars are "normally" smaller than worlds even though avatars can still be like 200MB 💀
        if (AsyncFileReader.IsAvatar(BundleFile, out var avatarId))
        {
            var fileSizeBytes = new FileInfo(BundleFile).Length;
            Output($@"ID: {avatarId}, CacheID: {CacheID}, Size: {fileSizeBytes / (1024.0 * 1024.0):F1}MB, Type: Avatar");
            await Task.Yield();
            GC.Collect(1, GCCollectionMode.Optimized, false, false);
            return;
        }
        if (AsyncFileReader.IsWorld(BundleFile, out var worldId))
        {
            var Bundle = new FileInfo(BundleFile);
            var IsDataswap = Bundle.Directory!.EnumerateFiles().Any(file => file.Name == "WorldData.csv");
            Output($@"ID: {worldId}, CacheID: {CacheID}, Size: {Bundle.Length / (1024.0 * 1024.0):F1}MB, Type: {(IsDataswap ? "Dataswap" : "World")}");
            await Task.Yield();
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Aggressive, true);
            GC.WaitForPendingFinalizers();
            SetProcessWorkingSetSize64Bit(Process.GetCurrentProcess().Handle, -1, -1);
            return;
        }
    }

}
