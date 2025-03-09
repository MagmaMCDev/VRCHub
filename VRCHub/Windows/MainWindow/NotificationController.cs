using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using System.Windows.Threading;
namespace VRCHub;

public partial class MainWindow
{
    private bool NotificationDebounce = true;

    private DispatcherTimer? delay;
    private static readonly Thickness startMargin = new(992, 435, -362, 0);
    private static readonly Thickness endMargin = new(630, 435, 0, 0);
    private static readonly ThicknessAnimation StartAnimation = new()
    {
        From = startMargin,
        To = endMargin,
        Duration = TimeSpan.FromSeconds(0.5),
        EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
    };
    static readonly ThicknessAnimation EndAnimation = new()
    {
        From = endMargin,
        To = startMargin,
        Duration = TimeSpan.FromSeconds(0.5),
        EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
    };

    public void ShowNotification(string message)
    {
        if (delay == null)
        {
            delay = new()
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            delay.Tick += (s, e) =>
            {
                Notification.BeginAnimation(Control.MarginProperty, EndAnimation);
                delay.Stop();
                NotificationDebounce = true;
            };
        }
        if (!NotificationDebounce)
        {
            Notification.Message.Content = message;
            Notification.Margin = startMargin;
            delay.Stop();
            delay.Start();
            return;
        }

        NotificationDebounce = false;
        Notification.Message.Content = message;
        Notification.Margin = startMargin;

        Notification.BeginAnimation(Control.MarginProperty, StartAnimation);
        delay.Start();
    }

}