using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using MagmaMc.JEF;

namespace VRCHub;

public static class Config
{
    /// <summary>
    /// VRChat Local Executable
    /// </summary>
    public static string VRChatInstallPath { get; set; } = @"C:\Program Files (x86)\Steam\steamapps\common\VRChat\VRChat.exe";

    /// <summary>
    /// Send Analytics About App Usage
    /// </summary>
    public static bool SendAnalytics { get; set; } = true;
    public static RegKey[]? VRChatRegBackup { get; set; }
    public static List<VRCUser> AccountsCache
    {
        get; set;
    } = [];
    public static List<VRCAccount>? SavedAccounts
    {
        get; set;
    } = [];
    public static bool Loaded = false;
    private static readonly string ConfigPath = Path.Combine(JEF.Utils.Folders.LocalUserAppData, "VRCHub");
    private static readonly string ConfigFilename = Path.Combine(ConfigPath, "Config.json");
    private static readonly JsonSerializerOptions JsonConfig = new() { 
        ReadCommentHandling = JsonCommentHandling.Skip, 
        WriteIndented = true, 
        IndentSize = 4, 
        IncludeFields = true,
        PropertyNameCaseInsensitive = true
    };
    public static void LoadConfig()
    {
        try
        {
            if (!Directory.Exists(ConfigPath))
                Directory.CreateDirectory(ConfigPath);

            if (File.Exists(ConfigFilename))
            {
                var json = File.ReadAllText(ConfigFilename);
                var config = JsonSerializer.Deserialize<ConfigData>(json, JsonConfig);
                Console.WriteLine(json);
                if (config != null)
                {
                    VRChatInstallPath = config.VRC_Path ?? VRChatInstallPath;
                    SendAnalytics = config.SendAnalytics;
                    VRChatRegBackup = config.VRChatRegBackup;
#pragma warning disable CS8619
                    SavedAccounts = (config.SavedAccounts ?? [])
                        .Select(base64Compressed =>
                        {
                            try
                            {
                                byte[] compressedBytes = Convert.FromBase64String(base64Compressed);
                                byte[] jsonBytes = Decompress(compressedBytes);
                                string json = Encoding.UTF8.GetString(jsonBytes);
                                return JsonSerializer.Deserialize<VRCAccount>(json);
                            }
                            catch
                            {
                                return null;
                            }
                        }).Where(account => account != null).ToList();
#pragma warning restore CS8619
                }
                Loaded = true;
            }
        }
        catch (Exception ex)
        {
            SimpleLogger.Error($"Failed loading config: {ex.Message}");
        }
    }

    private static bool Writable = true;
    public static void SaveConfig()
    {
        try
        {
            if (!Directory.Exists(ConfigPath))
                Directory.CreateDirectory(ConfigPath);

            var config = new ConfigData
            {
                VRC_Path = VRChatInstallPath,
                SendAnalytics = SendAnalytics,
                VRChatRegBackup = VRChatRegBackup
            };
            config.SavedAccounts = new string[SavedAccounts?.Count ?? 0];
            int index = 0;
            foreach (var account in SavedAccounts ?? [])
            {
                byte[] jsonBytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(account));
                byte[] compressedBytes = Compress(jsonBytes);
                string base64Compressed = Convert.ToBase64String(compressedBytes);
                config.SavedAccounts[index++] = base64Compressed;
            }

            string json = JsonSerializer.Serialize(config, JsonConfig);
            File.WriteAllText(ConfigFilename, json);
            Writable = true;
        }
        catch (Exception ex)
        {
            SimpleLogger.Error($"Failed loading config: {ex.Message}");
            Writable = true;
        } finally
        {
            Writable = true;
        }
    }
    private static byte[] Compress(byte[] data)
    {
        using var outputStream = new MemoryStream();
        using (var gzipStream = new GZipStream(outputStream, CompressionLevel.Optimal))
        {
            gzipStream.Write(data, 0, data.Length);
        }
        return outputStream.ToArray();
    }
    private static byte[] Decompress(byte[] data)
    {
        using var inputStream = new MemoryStream(data);
        using var gzipStream = new GZipStream(inputStream, CompressionMode.Decompress);
        using var outputStream = new MemoryStream();
        gzipStream.CopyTo(outputStream);
        return outputStream.ToArray();
    }


    private class ConfigData
    {
        public string? VRC_Path
        {
            get; set;
        }
        public bool SendAnalytics
        {
            get; set;
        }
        public string[]? SavedAccounts
        {
            get; set;
        } = null;
        public RegKey[]? VRChatRegBackup { get; set; } = null;
    }
}
public class RegKey
{
    public string ObjectName { get; set; } = "";
    public object? Value
    {
        get; set;
    }
}
