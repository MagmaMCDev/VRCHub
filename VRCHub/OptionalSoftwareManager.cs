using System;
using System.IO;
using ZER0.Core.Cryptography;
using ZER0.Core;
using System.Net.Http;
using System.Net.Http.Json;
using System.Diagnostics;
using System.IO.Compression;

namespace VRCHub;
public static class OptionalSoftwareManager
{
    private static readonly HttpClient _httpClient = new();
    public static string GetFileHash(string fileName) => GetFileHash(File.ReadAllBytes(fileName));

    public static string GetFileHash(byte[] data)
    {
        Crc32B cb = new();
        var hash = cb.ComputeChecksum(data);
        return hash.ToString("X8");
    }

    public static bool SoftwareInstalled(string fileName, string name)
    {
        try
        {
            var SoftwarePath = GetSoftwarePath(name);
            var Executable = Path.Combine(SoftwarePath, fileName);
            return File.Exists(Executable) && CheckSoftwareHashAsync(GetFileHash(Executable), fileName).GetAwaiter().GetResult();
        } catch
        {
            return false;
        }
    }

    public static void DownloadSoftware(string filename, string name)
    {
        var SoftwarePath = GetSoftwarePath(name);
        var Executable = Path.Combine(SoftwarePath, filename);
        string extension = Path.GetExtension(Executable);
        if (!SoftwareInstalled(filename, name))
        {
            File.WriteAllBytes(Executable, _httpClient.GetByteArrayAsync(ServerAPI.GetServer($"https://software.vrchub.site/{filename}")).GetAwaiter().GetResult());
        }
    }

    public static void DownloadSoftware(string filename, string name, string MainExecutable)
    {
        var SoftwarePath = GetSoftwarePath(name);
        var Executable = Path.Combine(SoftwarePath, name);
        string extension = Path.GetExtension(Executable);
        if (!SoftwareInstalled(MainExecutable, name))
        {
            DeleteSoftware(name);

            string ZipPath = Path.GetTempFileName();
            File.WriteAllBytes(ZipPath, _httpClient.GetByteArrayAsync(ServerAPI.GetServer($"https://software.vrchub.site/{filename}")).GetAwaiter().GetResult());
            ZipFile.ExtractToDirectory(ZipPath, SoftwarePath, true);
            File.Delete(ZipPath);
        }
    }


    /// <summary>
    /// Installs software by creating a shortcut in the Start Menu.
    /// </summary>
    /// <param name="fileName">Path to the software executable.</param>
    /// <param name="name">Name of the software.</param>
    public static void InstallSoftware(string fileName, string name)
    {
        var startMenu = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.StartMenu), "Programs", "VRCHub", name);
        Directory.CreateDirectory(Path.GetDirectoryName(startMenu)!);
        ShortcutManager.CreateShortcut(startMenu, Path.Combine(GetSoftwarePath(name), fileName), GetSoftwarePath(name));
    }

    /// <summary>
    /// Uninstalls software by removing the shortcut from the Start Menu.
    /// </summary>
    /// <param name="name">Name of the software.</param>
    public static void UninstallSoftware(string name)
    {
        var startMenu = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.StartMenu), "Programs", "VRCHub", name);
        ShortcutManager.DeleteShortcut(startMenu);
    }

    public static void DeleteSoftware(string name)
    {
        var SoftwarePath = GetSoftwarePath(name);
        try
        {
            Directory.Delete(SoftwarePath);
        }
        catch { }
    }

    public static async Task<bool> CheckSoftwareHashAsync(string hash, string filename)
    {
        try
        {
            FileCache[]? Cache = await _httpClient.GetFromJsonAsync<FileCache[]>(ServerAPI.GetServer("https://software.vrchub.site/Hash/"));
            if (Cache == null)
                throw new ArgumentNullException();
            foreach (FileCache cache in Cache)
            {
                if (filename.ToLower().Trim() == cache.filename.ToLower().Trim())
                    if (cache.hash.ToUpper().Trim() == hash.ToUpper().Trim()) 
                        return true;
            }
        }
        catch { }
        return false;
    }


    public static string GetSoftwarePath(string name)
    {
        var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "VRCHub", name);
        Directory.CreateDirectory(path); 
        return path;
    }
}

internal class FileCache
{
    public string filename { get; set; } = "";
    public string hash { get; set; } = "";
    public ulong last_modified { get; set; } = 0;
}