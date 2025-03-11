using System.Diagnostics;

namespace VRCHub;

public struct LibSerials
{
    private static string ExecuteFunction(string command)
    {
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "VRCHubNative.exe",
                Arguments = command,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                WindowStyle = ProcessWindowStyle.Hidden,
                CreateNoWindow = true
            }
        };

        process.Start();
        string result = process.StandardOutput.ReadToEnd().Trim();
        process.WaitForExit();
        return result;
    }

    public static string System_HWID() => ExecuteFunction("System_HWID");
    public static string Baseboard_Manufacturer() => ExecuteFunction("Baseboard_Manufacturer");
    public static string Baseboard_Product() => ExecuteFunction("Baseboard_Product");
    public static string Baseboard_Serial() => ExecuteFunction("Baseboard_Serial");
    public static string CPU_Product() => ExecuteFunction("CPU_Product");
    public static string CPU_Serial() => ExecuteFunction("CPU_Serial");
    public static string BIOS_Vendor() => ExecuteFunction("BIOS_Vendor");
    public static string BIOS_Version() => ExecuteFunction("BIOS_Version");
    public static string BIOS_Date() => ExecuteFunction("BIOS_Date");
}
