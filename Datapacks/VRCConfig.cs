using System.IO;
using Newtonsoft.Json;

namespace VRCHub.Models;
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
    public static string GetVRChatPath() =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "..\\LocalLow\\VRChat\\VRChat\\");
    public static VRCConfig GetVRChatConfig()
    {
        var ConfigData = File.ReadAllText(GetVRChatPath() + "config.json");
        return JsonConvert.DeserializeObject<VRCConfig>(ConfigData)!;
    }
    public static string GetContentCachePath()
    {
        try
        {
            VRCConfig Conf = GetVRChatConfig();
            return Path.Combine(Conf.cache_directory, "Cache-WindowsPlayer");
        }
        catch
        {
            return Path.Combine(GetVRChatPath(), "Cache-WindowsPlayer\\");
        }
    }
}
