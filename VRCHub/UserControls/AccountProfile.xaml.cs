using System.Drawing.Imaging;
using System.Drawing;
using System.IO;
using System.Windows.Input;
using System.Windows.Media.Animation;
using BlurEffect = System.Windows.Media.Effects.BlurEffect;
using System.Windows.Forms;
using UserControl = System.Windows.Controls.UserControl;
using static VRCHub.Common;
using VRCHub.Resources;
namespace VRCHub;
public delegate void NotificationEventHandler(string message);
/// <summary>
/// Interaction logic for DatapackControl.xaml
/// </summary>
public partial class AccountProfile : UserControl
{

    public static event NotificationEventHandler? NotificationEvent;
    public AccountProfile()
    {
        InitializeComponent();
        PasswordVisiblity.Source = GetImageSource(BitmapToByteArray(MaterialIcons.visibility_off));
    }
    bool showpass = false;
    private void Pa(object sender, MouseButtonEventArgs e)
    {
        showpass = !showpass;
        PasswordVisiblity.Source = GetImageSource(BitmapToByteArray(showpass ? MaterialIcons.visibility_on : MaterialIcons.visibility_off));
        var blurAnimation = new DoubleAnimation
        {
            From = showpass ? 10 : 0,
            To = showpass ? 0 : 10,
            Duration = TimeSpan.FromSeconds(0.25)
        };
        Password.Effect.BeginAnimation(BlurEffect.RadiusProperty, blurAnimation);
    }

    private void Username_Click(object sender, MouseButtonEventArgs e)
    {
        try
        {
            Clipboard.SetText((string)Username.Content);
            NotificationEvent?.Invoke("Copied Username to clipboard!");
        }
        catch { }
    }
    private void Password_Click(object sender, MouseButtonEventArgs e)
    {
        try
        {
            Clipboard.SetText((string)Password.Content);
            NotificationEvent?.Invoke("Copied Password to clipboard!");
        } catch { }
    }
    private void Email_Click(object sender, MouseButtonEventArgs e)
    {
        try
        {
            Clipboard.SetText((string)Email.Content);
            NotificationEvent?.Invoke("Copied Email to clipboard!");
        } catch { }
    }
}
