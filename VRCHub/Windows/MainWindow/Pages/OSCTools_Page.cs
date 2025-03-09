using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using BuildSoft.VRChat.Osc.Chatbox;
using Microsoft.VisualBasic.Logging;
using VRCHub.Resources;

namespace VRCHub;

public partial class MainWindow
{

    private void OSCTools_Button_Click(object sender, RoutedEventArgs e)
    {
        Page_Select("OSCTools");
    }
    private bool InfinityTypingEnabled = false;
    private bool InvisibleNameEnabled = false;
    private void InfinityTyping_Logic()
    {
        while (InfinityTypingEnabled)
        {
            OscChatbox.SetIsTyping(true);
            Thread.Sleep(2000);
        }
    }
    private void InvisibleName_Logic()
    {
        while (InvisibleNameEnabled)
        {
            OscChatbox.SendMessage(new string('\v', 256), true, true);
            Thread.Sleep(2000);
        }
    }

    private void OSCTools_InfinityTyping(object? sender, RoutedEventArgs? e)
    {
        OSCTools_DisableAll();
        InfinityTypingEnabled = true;
        new Thread(InfinityTyping_Logic).Start();
    }
    private void OSCTools_InvisibleName(object? sender, RoutedEventArgs? e)
    {
        OSCTools_DisableAll();
        InvisibleNameEnabled = true;
        new Thread(InvisibleName_Logic).Start();
    }
    private void OSCTools_DisableAll(object? sender = null, RoutedEventArgs? e = null)
    {
        OscChatbox.SendMessage("", true, true);
        InfinityTypingEnabled = false;
        InvisibleNameEnabled = false;
    }
}