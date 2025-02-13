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
public partial class NotificationCenter : UserControl
{
    public event Action? InstallClicked;
    public event Action? UninstallClicked;
    public NotificationCenter()
    {
        InitializeComponent();
        //Datapack_Install.Click += (s, e) => InstallClicked?.Invoke();
        //Datapack_Uninstall.Click += (s, e) => UninstallClicked?.Invoke();
    }
}
