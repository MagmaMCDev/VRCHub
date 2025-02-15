using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace VRCHub;

#region Button Manager
public class ButtonData(string text, bool paused)
{
    public string BaseText = text;
    public bool Paused = paused;
}
public static class ButtonManager
{
    private static readonly Dictionary<Button, ButtonData> ButtonToggles = [];
    public static void PauseButton(Button button, string? SetText = null)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            if (!ButtonToggles.ContainsKey(button))
                ButtonToggles.Add(button, new((string)button.Content, true));
            if (SetText != null)
                button.Content = SetText;
        });
    }
    public static bool ButtonPaused(Button button)
    {
        if (ButtonToggles.TryGetValue(button, out var value))
            return value.Paused;
        else
            return false;
    }
    public static void ResumeButon(Button button)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            if (!ButtonToggles.TryGetValue(button, out var value))
                ButtonToggles.Add(button, new((string)button.Content, false));
            else
                value.Paused = false;
            button.Content = ButtonToggles[button].BaseText;
        });
    }
}
#endregion