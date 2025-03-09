using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Segment;
using VRCHub.Resources;
using VRCHub.Windows;
using static VRCHub.Common;

namespace VRCHub;

public partial class MainWindow
{
    private void AccountManager_Click(object sender, RoutedEventArgs e)
    {
        Page_Select("AccountManager");
        SetupAccountManager();

    }
    bool setupAccounts = false;
    private async void SetupAccountManager(bool force = false)
    {
        if (setupAccounts && !force)
            return;
        await Task.Run(async () =>
        {
            setupAccounts = true;
            while (!Config.Loaded)
                await Task.Delay(25);
            Config.SavedAccounts ??= [];
            var Accounts = Config.SavedAccounts;
            Parallel.ForEach(Accounts, account =>
            {
                if (!account.Validate())
                    Config.SavedAccounts.Remove(account);
            }); // how to wait before continueing
            foreach (var acc in Config.SavedAccounts)
                AddAccount(acc, false);
            FormatAccounts();
            if (Config.SavedAccounts.Any(value => value.main))
                Application.Current.Dispatcher.Invoke(() => ManageAccountsButton.Content = "Add");

        });

    }

    private async void ManageAccountsButton_Click(object sender, RoutedEventArgs e)
    {
        bool main = ManageAccountsButton.Content.ToString() != "Add";
        var LoginWindow = new VRChatLoginWindow();
        var auth = await LoginWindow.LoginAsync();
        if (auth == null)
            return;
        VRCAccount account = new()
        {
            id = LoginWindow.UserData.id,
            email = LoginWindow.Email,
            username = LoginWindow.Username,
            password = LoginWindow.Password,
            auth = auth.auth,
            twoFactorAuth = auth.twoFactorAuth,
            main = main,
        };
        SimpleLogger.Debug("Logged User Sucessfully");

        Config.SavedAccounts ??= [];
        Config.SavedAccounts.Add(account);
        AddAccount(account);
        Config.SaveConfig();
    }
    private Bitmap GetRank(ref VRCUser user)
    {
        TrustRank rank = user.GetRank();
        switch (rank)
        {
            case TrustRank.Visitor:
                return MaterialIcons.Visitor;
            case TrustRank.New:
                return MaterialIcons.New;
            case TrustRank.User:
                return MaterialIcons.User;
            case TrustRank.Known:
                return MaterialIcons.Known;
            case TrustRank.Trusted:
                return MaterialIcons.Trusted;
            case TrustRank.Administrator:
                return MaterialIcons.Administrator;
            case TrustRank.Developer:
                return MaterialIcons.Developer;
            default:
                return MaterialIcons.Visitor;
        }
    }
    private SolidColorBrush GetColor(string color) =>
        new((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(color));

    private void FormatAccounts()
    {
        const ushort Height = 130;
        const ushort Width = 300;
        const ushort vSpacing = 20;
        const ushort hSpacing = 20;
        const ushort initialTop = 10;
        const ushort initialLeft = 45;
        const byte PerRow = 2;

        Application.Current.Dispatcher.Invoke(() =>
        {
            int i = 0;
            foreach (AccountProfile profile in AccountManager_Canvas.Children)
            {
                if (!profile.Username.Content.ToString()!.ToLowerInvariant().Contains(SearchQuery.ToLowerInvariant()))
                {
                    profile.Visibility = Visibility.Hidden;
                    continue;
                }
                else
                    profile.Visibility = Visibility.Visible;

                var row = i / PerRow;
                var column = i % PerRow;
                float topPosition = initialTop + (Height + vSpacing) * row;
                float leftPosition = initialLeft + (Width + hSpacing) * column;
                Canvas.SetLeft(profile, leftPosition);
                Canvas.SetTop(profile, topPosition);
                var newCanvasHeight = topPosition + Height + initialTop;
                if (AccountManager_Canvas.Height < newCanvasHeight)
                    AccountManager_Canvas.Height = newCanvasHeight;
                i++;
            }
        });
    }
    private void AddAccount(VRCAccount Account, bool format = true)
    {
        VRCAPI.SetAuth(Account);
        if (Account.UserData == null)
        {
            Account.UserData = VRCAPI.GetUser();
            SimpleLogger.Warn("User Data Was Not Initialized!");
        }

        Application.Current.Dispatcher.Invoke(() =>
        {
            var AccountControl = new AccountProfile();
            AccountControl.Username.Content = Account.UserData!.displayName;
            AccountControl.Email.Content = Account.email;
            AccountControl.Password.Content = Account.password;
            AccountControl.StatusMessage.Content = Account.UserData.GetStatus();
            AccountControl.AgeVerified.Source = Account.UserData.Adult ? GetImageSource(MaterialIcons._18Plus) : null;
            AccountControl.Tag.Source = GetImageSource(GetRank(ref Account.UserData));
            if (Account.UserData.status == "join me")
                AccountControl.StatusColor.SetCurrentValue(Ellipse.FillProperty, GetColor("#43c9fe"));
            else if (Account.UserData.status == "active")
                AccountControl.StatusColor.SetCurrentValue(Ellipse.FillProperty, GetColor("#2ED319"));
            else if (Account.UserData.status == "ask me")
                AccountControl.StatusColor.SetCurrentValue(Ellipse.FillProperty, GetColor("#ea8036"));
            else if (Account.UserData.status == "do not disturb")
                AccountControl.StatusColor.SetCurrentValue(Ellipse.FillProperty, GetColor("#5c0e0e"));

            string? Background = string.IsNullOrEmpty(Account.UserData.profilePicOverrideThumbnail) ? Account.UserData.currentAvatarThumbnailImageUrl : Account.UserData.profilePicOverrideThumbnail;

            AccountManager_Canvas.Children.Add(AccountControl);
            AccountControl.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
            Task.Run(() =>
            {
                if (Background == null)
                    return;
                Task<byte[]> Image = api!.GetByteArrayAsync(Background);
                var Image2 = Image.GetAwaiter().GetResult();
                Application.Current.Dispatcher.Invoke(() =>
                {
                    AccountControl.ProfileImage.Source = GetImageSource(Image2);
                });
            });
            if (format)
                FormatAccounts();
        });
    }

    private void SearchBarKeyUp(object sender, System.Windows.Input.KeyEventArgs e)
    {
        SearchQuery = SearchBar.Text.ToString();
        FormatAccounts();
    }
}