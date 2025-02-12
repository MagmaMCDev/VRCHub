using System.Drawing.Imaging;
using System.Drawing;
using System.IO;
using System.Windows.Input;
using System.Windows.Media.Animation;
using BlurEffect = System.Windows.Media.Effects.BlurEffect;
using System.Windows.Forms;
using UserControl = System.Windows.Controls.UserControl;
using static VRCHub.Common;
namespace VRCHub;
/// <summary>
/// Interaction logic for DatapackControl.xaml
/// </summary>
public partial class AccountProfile : UserControl
{
    public event Action? InstallClicked;
    public event Action? UninstallClicked;
    public AccountProfile()
    {
        InitializeComponent();
        PasswordVisiblity.Source = GetImageSource(BitmapToByteArray(MaterialIcons.visibility_off));
        //Datapack_Install.Click += (s, e) => InstallClicked?.Invoke();
        //Datapack_Uninstall.Click += (s, e) => UninstallClicked?.Invoke();
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
        Clipboard.SetText((string)Username.Content);
    }
    private void Password_Click(object sender, MouseButtonEventArgs e)
    {
        Clipboard.SetText((string)Password.Content);
    }
    private void Email_Click(object sender, MouseButtonEventArgs e)
    {
        Clipboard.SetText((string)Email.Content);
    }
}
