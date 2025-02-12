using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

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
        //Datapack_Install.Click += (s, e) => InstallClicked?.Invoke();
        //Datapack_Uninstall.Click += (s, e) => UninstallClicked?.Invoke();
    }
}
