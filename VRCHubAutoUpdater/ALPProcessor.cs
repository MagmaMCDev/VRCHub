using System.Text.RegularExpressions;

namespace VRCHubAutoUpdater;
public enum AssetType
{
    Avatar,
    World,
    Dataswap
}
public delegate void AssetEventArgs(string ID, string CacheID);
public delegate void CommonAssetEventArgs(string ID, string CacheID, AssetType Type);
public partial class ALPProcessor
{
    private static long lastReadPosition = 0;
    private static string partialBuffer = "";
    internal static readonly string[] separator = ["\r\n", "\n"];
    private static readonly object ProcessLock = new();
    private string Log = "ALP.log";
    private Thread? Main = null;
    public event CommonAssetEventArgs? OnAssetProcessed;
    public event AssetEventArgs? OnAvatarProcessed;
    public event AssetEventArgs? OnWorldProcessed;
    public event AssetEventArgs? OnDataswapProcessed;
    public void Start(string LogFile)
    {
        if (Main != null)
            return;
        Log = LogFile;
        Main = new(() =>
        {
            while (Main != null)
            {
                ProcessContent();
                Thread.Sleep(1);
            }
        });
        Main.Start();
    }
    public void Wait(TimeSpan MaxSpan)
    {
        var startTime = DateTime.Now;
        while (Main != null)
        {
            if (DateTime.Now - startTime > MaxSpan)
                break;
            Thread.Sleep(10);
        }
    }

    public void Stop()
    {
        Main = null;
    }
    void ProcessContent()
    {
        lock (ProcessLock)
        {
            try
            {
                var fileInfo = new FileInfo(Log);

                if (fileInfo.Length < lastReadPosition)
                {
                    lastReadPosition = 0;
                    partialBuffer = "";
                }

                if (fileInfo.Length > lastReadPosition)
                {
                    using var fs = new FileStream(Log, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                    fs.Seek(lastReadPosition, SeekOrigin.Begin);
                    using var reader = new StreamReader(fs);
                    var newContent = reader.ReadToEnd();

                    lastReadPosition = fs.Position;
                    ProcessNewContent(newContent);
                }
            }
            catch { }
        }
    }

    void ProcessNewContent(string content)
    {
        var combinedContent = partialBuffer + content;
        var lines = combinedContent.Split(separator, StringSplitOptions.None);

        if (!combinedContent.EndsWith('\n'))
        {
            partialBuffer = lines[^1];
            for (var i = 0; i < lines.Length - 1; i++)
                ProcessLine(lines[i]);
        }
        else
        {
            partialBuffer = "";
            foreach (var line in lines)
                ProcessLine(line);
        }
    }

    void ProcessLine(string line)
    {
        if (string.IsNullOrWhiteSpace(line))
            return;

        var match = LogFileRegex().Match(line);
        if (!match.Success)
            return;

        var id = match.Groups["id"].Value;
        var cacheId = match.Groups["cacheid"].Value;
        var size = match.Groups["size"].Value;
        var type = match.Groups["type"].Value;

#if DEBUG
        Console.WriteLine($"ID: {id}, CacheID: {cacheId}, Size: {size}, Type: {type}");
#endif
        if (type.ToLower().Trim() == "avatar")
        {
            OnAssetProcessed?.Invoke(id, cacheId, AssetType.Avatar);
            OnAvatarProcessed?.Invoke(id, cacheId);
        }
        else if (type.ToLower().Trim() == "world")
        {
            OnAssetProcessed?.Invoke(id, cacheId, AssetType.World);
            OnWorldProcessed?.Invoke(id, cacheId);
        }
        else
        {
            OnAssetProcessed?.Invoke(id, cacheId, AssetType.Dataswap);
            OnDataswapProcessed?.Invoke(id, cacheId);
        }
    }

    [GeneratedRegex(@"ID: (?<id>[a-zA-Z0-9_-]+), CacheID: (?<cacheid>[a-zA-Z0-9]+), Size: (?<size>[\d\.]+MB), Type: (?<type>[a-zA-Z]+)", RegexOptions.Compiled)]
    public static partial Regex LogFileRegex();
}
