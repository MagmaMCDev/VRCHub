using System.Windows.Controls;

namespace VRCHub;

public partial class MainWindow
{
    public Grid[] Pages =>
    [
        VRCFX_Panel,
        VRCSpoofer_Panel,
        Datapacks_Panel,
        Splashscreen_Panel,
        Settings_Panel,
        DatapackCreator_Panel,
        MelonLoader_Panel,
        AccountManager_Panel,
    ];

    public void Page_Select(string Page)
    {
        foreach(Grid pageControl in Pages)
        {
            pageControl.Visibility = pageControl.Name.StartsWith(Page, StringComparison.OrdinalIgnoreCase) ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
        }
    }

}