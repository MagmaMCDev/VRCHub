using System.Collections.Concurrent;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Pastel;

namespace AssetLoggerPlugin;

internal partial class AssetLogger
{
    private static bool ConsoleLog = false;
    private static bool FileLog = false;
    private static StreamWriter? writer;

    [DllImport("kernel32.dll")]
    private static extern bool AllocConsole();
    [DllImport("KERNEL32.DLL", CallingConvention = CallingConvention.StdCall, EntryPoint = "SetProcessWorkingSetSize", SetLastError = true)]
    internal static extern bool SetProcessWorkingSetSize64Bit(IntPtr pProcess, long dwMinimumWorkingSetSize, long dwMaximumWorkingSetSize);
    private static readonly SemaphoreSlim _semaphore = new(1, 1);
    private static readonly ConcurrentQueue<string> _messageQueue = new();

    public static string StripAnsiCodes(string input)
    {
        return AnsiCodes().Replace(input, string.Empty);
    }
    public static async void Output(string message)
    {
        message = message.Replace("\0", string.Empty);
        await _semaphore.WaitAsync();
        _messageQueue.Enqueue(message);
        await ProcessQueueAsync();
        _semaphore.Release();
    }
    private static async Task ProcessQueueAsync()
    {
        try
        {
            while (_messageQueue.TryDequeue(out var message))
            {
                if (ConsoleLog)
                    Console.WriteLine(message);
                if (FileLog)
                    await writer!.WriteLineAsync(StripAnsiCodes(message));
            }
        }
        catch { }
    }

    private static readonly string LogFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "VRCHub", "ALP.log");
    private static readonly string VRChatCacheDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            @"..\LocalLow\VRChat\VRChat\Cache-WindowsPlayer"
        );
    private static bool LogAvatars = false;
    private static bool LogWorlds = false;
    private static short threads = -1;
    static async Task Main(string[] args)
    {
        #region Setup
        Directory.CreateDirectory(Path.GetDirectoryName(LogFile)!);

        foreach (var arg in args)
        {
            if (arg.StartsWith("--threads"))
            {
                var parts = arg.Split('=');
                if (parts.Length > 1 && short.TryParse(parts[1], out var parsedThreads))
                    threads = parsedThreads;
            }
            else if (arg == "--avatars")
                LogAvatars = true;
            else if (arg == "--worlds")
                LogWorlds = true;
        }



        if (args.Contains("--console"))
        {
            ConsoleLog = true;
            AllocConsole();
        }

        if (args.Contains("--log"))
        {
            FileLog = true;
            try
            {
                File.WriteAllBytes(LogFile, []);
            }
            catch { }
            FileStream fileStream = new(LogFile, FileMode.Append, FileAccess.Write, FileShare.Read);
            writer = new StreamWriter(fileStream)
            {
                AutoFlush = true
            };
        }

        if (!ConsoleLog && !FileLog)
        {
            AllocConsole();
            LogAvatars = true;
            LogWorlds = true;
            Console.Title = "APL - Menu";
            Console.WriteLine("No arguments provided.");
            Console.WriteLine("\t--console [Console Window]");
            Console.WriteLine("\t--log [Logs Data To Prebuilt File]");
            Console.WriteLine("\t--avatars [Logs Avatars]");
            Console.WriteLine("\t--worlds [Logs Worlds]");
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
                try
                {
                    File.WriteAllBytes(LogFile, []);
                }
                catch { }
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
        if (ConsoleLog)
            Console.Title = "APL - Catching Up";
        ProcessExistingBundles();
        if (ConsoleLog)
            Console.Title = "APL - Logging";

        await Task.Yield();
        await ProcessQueueAsync();
        if (FileLog)
            writer!.Flush();
        await Task.Delay(10);

        watcher.Created += OnFileCreated;
        while (true)
            Thread.Sleep(250);
    }
    private static readonly Queue<string> pendingBundleFiles = new();
    private static readonly object queueLock = new();
    private static async void ProcessExistingBundles()
    {
        var existingFiles = Directory.GetFiles(VRChatCacheDirectory, "__data", SearchOption.AllDirectories);
        var SafeProcessorCount = Environment.ProcessorCount >= 4 ? Environment.ProcessorCount : 8; // ensure at least a few threads
        if (threads != -1)
            SafeProcessorCount = threads * 2;
        ParallelOptions options = new()
        {
            MaxDegreeOfParallelism = SafeProcessorCount / 2
        };
        Parallel.ForEach(existingFiles, options, file =>
        {
            try
            {
                ProcessBundleFile(file, false).Wait();
            }
            catch { }
        });

        await Task.Yield();
        GC.Collect(GC.MaxGeneration, GCCollectionMode.Aggressive, true);
        GC.WaitForPendingFinalizers();
        SetProcessWorkingSetSize64Bit(Process.GetCurrentProcess().Handle, -1, -1);
    }
    private static async Task ProcessBundleFile(string bundleFile, bool ForceGC)
    {
        string? CacheID;
        try
        {
            var fileInfo = new DirectoryInfo(bundleFile);
            CacheID = fileInfo.Parent!.Parent!.Name;
        }
        catch
        {
            return;
        }


        // avatars are "normally" smaller than worlds even though avatars can still be like 200MB 💀
        if (AsyncFileReader.IsAvatar(bundleFile, out var avatarId))
        {
            if (LogAvatars)
            {
                var fileSizeBytes = new FileInfo(bundleFile).Length;
                Output($@"ID: {avatarId.Pastel("#ffb3b3")}, CacheID: {CacheID.Pastel("#b3e0ff")}, Size: {(fileSizeBytes / (1024.0 * 1024.0)).ToString("F1").Pastel("#ffcc99")}" + "MB".Pastel(Color.Orange) +$" Type: ".Pastel("#ffffff") + "Avatar".Pastel("#ff99cc"));
            }
            if (ForceGC)
            {
                await Task.Yield();
                GC.Collect(1, GCCollectionMode.Optimized, false, false);
            }
            return;
        }

        if (AsyncFileReader.IsWorld(bundleFile, out var worldId))
        {
            if (LogWorlds)
            {
                var Bundle = new FileInfo(bundleFile);
                var IsDataswap = Bundle.Directory!.EnumerateFiles().Any(file => file.Name == "WorldData.csv");
                Output($@"ID: {worldId.Pastel("#ffb3b3")}, CacheID: {CacheID.Pastel("#b3e0ff")}, Size: {(Bundle.Length / (1024.0 * 1024.0)).ToString("F1").Pastel("#ffcc99")}" + "MB".Pastel(Color.Orange) +$", Type: {(IsDataswap ? "Dataswap" : "World").Pastel("#ff99cc")}".Pastel("#ffffff"));
            }
            if (ForceGC)
            {
                await Task.Yield();
                GC.Collect(GC.MaxGeneration, GCCollectionMode.Aggressive, true);
                GC.WaitForPendingFinalizers();
                SetProcessWorkingSetSize64Bit(Process.GetCurrentProcess().Handle, -1, -1);
            }
            return;
        }
    }

    private static async void OnFileCreated(object sender, FileSystemEventArgs e)
    {
        if (!Path.GetFileName(e.FullPath).Equals("__lock", StringComparison.OrdinalIgnoreCase))
            return;

        var BundleFile = Path.Combine(Path.GetDirectoryName(e.FullPath)!, "__data");

        lock (queueLock)
            pendingBundleFiles.Enqueue(BundleFile);

        await ProcessBundleFile(BundleFile, true);
    }

    [GeneratedRegex(@"\x1b\[[0-9;]*[mK]", RegexOptions.Compiled)]
    public static partial Regex AnsiCodes();
}
