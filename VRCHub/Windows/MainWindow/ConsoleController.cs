using System.Diagnostics;

namespace VRCHub;

public partial class MainWindow
{
    private static void SetupConsole(string[] args)
    {
        if (args.Contains("--console") || args.Contains("-v") || args.Contains("--verbose") || Debugger.IsAttached)
        {
            KERNAL32.AllocConsole();
            Console.Title = "VRCHub - Debug Console";
            Console.SetOut(new System.IO.StreamWriter(Console.OpenStandardOutput()) { AutoFlush = true });
            Console.OutputEncoding = System.Text.Encoding.UTF8;
        }
    }
}