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
    public static VRCUser[] AccountsCache
    {
        get; set;
    } = [];
    public static VRCAccount[] SavedAccounts
    {
        get; set;
    } = [];

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
                    SavedAccounts = config.SavedAccounts;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading config: {ex.Message}");
        }
    }

    private static bool Writable = true;
    public static void SaveConfig()
    {
        if (Writable)
            return;
        Writable = false;
        try
        {
            if (!Directory.Exists(ConfigPath))
                Directory.CreateDirectory(ConfigPath);

            var config = new ConfigData
            {
                VRC_Path = VRC_Path,
                SendAnalytics = SendAnalytics,
                VRChatRegBackup = VRChatRegBackup,
                SavedAccounts = SavedAccounts
            };

            string json = JsonSerializer.Serialize(config, JsonConfig);
            File.WriteAllText(ConfigFilename, json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error saving config: {ex.Message}");
        } finally
        {
            Writable = true;
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
        public VRCAccount[] SavedAccounts
        {
            get; set;
        } = [];
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
#pragma warning disable IDE1006 // Naming Styles
public class VRCAccount
{
    public string id
    {
        get; set;
    } = "";
    public string username
    {
        get; set;
    } = "";
    public string email
    {
        get; set;
    } = "";

    public string auth
    {
        get; set;
    } = "";
    public string twoauth
    {
        get; set;
    } = "";
}
public class VRCUser
{
    public string? bio
    {
        get; set;
    }
    public string? currentAvatarImageUrl
    {
        get; set;
    }
    public string? currentAvatarThumbnailImageUrl
    {
        get; set;
    }
    public string date_joined
    {
        get; set;
    } = "";
    public string displayName
    {
        get; set;
    } = "";
    public string id
    {
        get; set;
    } = "";
    public string? isAdult
    {
        get; set;
    }
    public string? last_platform
    {
        get; set;
    }
    public string? profilePicOverride
    {
        get; set;
    }
    public string? profilePicOverrideThumbnail
    {
        get; set;
    }
    public string? pronouns
    {
        get; set;
    }
    public string? state
    {
        get; set;
    }
    public string? status
    {
        get; set;
    }
    public string? statusDescription
    {
        get; set;
    }
    public string[] tags
    {
        get; set;
    } = [];
    public string? userIcon
    {
        get; set;
    }
    public string username
    {
        get; set;
    } = "";
}
#pragma warning restore IDE1006 // Naming Styles