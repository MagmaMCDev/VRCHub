using System.IO.MemoryMappedFiles;
using System.Text;
using System.Text.RegularExpressions;

namespace AssetLoggerPlugin;
public struct AsyncFileReader
{
    public static bool IsWorld(string filePath, out string? worldId)
    {
        return FileContainsPattern(filePath, @"wrld_[a-fA-F0-9]{8}-[a-fA-F0-9]{4}-[a-fA-F0-9]{4}-[a-fA-F0-9]{4}-[a-fA-F0-9]{12}", out worldId);
    }
    public static bool IsAvatar(string filePath, out string? avatarId)
    {
        return FileContainsPattern(filePath, @"avtr_[a-fA-F0-9]{8}-[a-fA-F0-9]{4}-[a-fA-F0-9]{4}-[a-fA-F0-9]{4}-[a-fA-F0-9]{12}", out avatarId);
    }

    public static bool FileContainsPattern(string filePath, string pattern, out string? matchedID)
    {
        matchedID = null;
        Regex regex = new(pattern, RegexOptions.Compiled);

        using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.SequentialScan);
        using var mmf = MemoryMappedFile.CreateFromFile(fs, null, 0, MemoryMappedFileAccess.Read, HandleInheritability.None, false);
        using var accessor = mmf.CreateViewStream(0, 0, MemoryMappedFileAccess.Read);
        var buffer = new byte[1024 * 512]; // 512 KB buffer
        int bytesRead;
        var leftover = "";

        while ((bytesRead = accessor.Read(buffer, 0, buffer.Length)) > 0)
        {
            var chunk = leftover + Encoding.ASCII.GetString(buffer, 0, bytesRead);

            var match = regex.Match(chunk);
            if (match.Success)
            {
                matchedID = match.Value;
                return true;
            }

            leftover = chunk.Length > 512 ? chunk[^512..] : chunk;
        }

        return false;
    }

}
