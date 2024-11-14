using System;
using System.IO;
using System.Text.Json;
using MagmaMc.JEF;

namespace VRCHub;

public static class Config
{
    /// <summary>
    /// VRChat Local Executable
    /// </summary>
    public static string VRC_Path { get; set; } = @"C:\Program Files (x86)\Steam\steamapps\common\VRChat\VRChat.exe";

    /// <summary>
    /// Send Analytics About App Usage
    /// </summary>
    public static bool SendAnalytics { get; set; } = true;
    public static RegKey[]? VRChatRegBackup { get; set; }

    private static readonly string ConfigPath = Path.Combine(JEF.Utils.Folders.LocalUserAppData, "VRCHub");
    private static readonly string ConfigFilename = Path.Combine(ConfigPath, "Config.json");
    private static readonly JsonSerializerOptions JsonConfig = new() { ReadCommentHandling = JsonCommentHandling.Skip, WriteIndented = true, IndentSize = 4 };
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

                if (config != null)
                {
                    VRC_Path = config.VRC_Path ?? VRC_Path;
                    SendAnalytics = config.SendAnalytics;
                    VRChatRegBackup = config.VRChatRegBackup;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading config: {ex.Message}");
        }
    }

    public static void SaveConfig()
    {
        try
        {
            if (!Directory.Exists(ConfigPath))
            {
                Directory.CreateDirectory(ConfigPath);
            }

            var config = new ConfigData
            {
                VRC_Path = VRC_Path,
                SendAnalytics = SendAnalytics,
                VRChatRegBackup = VRChatRegBackup
            };

            string json = JsonSerializer.Serialize(config, JsonConfig);
            File.WriteAllText(ConfigFilename, json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error saving config: {ex.Message}");
        }
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
        public RegKey[]? VRChatRegBackup { get; set; }
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