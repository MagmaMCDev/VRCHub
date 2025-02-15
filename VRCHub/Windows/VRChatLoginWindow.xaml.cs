using System.Windows;
using OpenVRChatAPI;
using VRCAuth2 = OpenVRChatAPI.Models.VRCAuth;
using static VRCHub.SimpleLogger;

namespace VRCHub.Windows;
/// <summary>
/// Interaction logic for VRChatLoginWindow.xaml
/// </summary>
public partial class VRChatLoginWindow : Window
{
    private readonly bool _allowEmail;
    private bool _isCodeMode;

    public string Username
    {
        get; private set;
    }
    public string Email
    {
        get; private set;
    }
    public string Password
    {
        get; private set;
    }
    public VRCAuth2? UserAuth
    {
        get; private set;
    }
    public VRCUser UserData
    {
        get; private set;
    }

    public VRChatLoginWindow(bool allowEmail = false)
    {
        InitializeComponent();
        _allowEmail = allowEmail;
        ResetInputFields();
        SetMessage(string.Empty);
    }

    /// <summary>
    /// Resets the UI fields to their initial state.
    /// </summary>
    private void ResetInputFields()
    {
        UsernameInput.IsEnabled = true;
        EmailInput.IsEnabled = true;
        PasswordInput.Visibility = Visibility.Visible;
        CodeInput.Visibility = Visibility.Collapsed;
        _isCodeMode = false;
    }

    /// <summary>
    /// Opens the login window modally and returns the authenticated user data.
    /// </summary>
    public async Task<VRCAuth2?> LoginAsync()
    {
        var tcs = new TaskCompletionSource<VRCAuth2?>();
        Closed += (sender, e) =>
        {
            tcs.TrySetResult(UserAuth);
        };

        Show();
        return await tcs.Task;
    }


    /// <summary>
    /// Handles the login button click event.
    /// </summary>
    private void Button_Click (object sender, RoutedEventArgs e)
    {
        if (!_isCodeMode)
        {
            HandlePrimaryLogin();
        }
        else
        {
            HandleTwoFactorLogin();
        }
    }

    /// <summary>
    /// Handles the first stage of login where the username, email, and password are validated.
    /// </summary>
    private void HandlePrimaryLogin()
    {
        Username = UsernameInput.Text.Trim();
        Email = EmailInput.Text.Trim();
        Password = PasswordInput.Text.Trim();

        // Ensure all fields are filled
        if (Username.Length <= 1 || Email.Length <= 1 || Password.Length <= 1)
        {
            SetMessage("Please fill in all fields.");
            return;
        }

        // Test login using both username and email
        var usernameTest = VRCAPI.Login(Username, Password);
        var emailTest = VRCAPI.Login(Email, Password);

        if (usernameTest == null && emailTest == null)
        {
            SetMessage("Password is incorrect!");
            return;
        }

        if (usernameTest == null)
        {
            SetMessage("Username does not match email!");
            return;
        }

        if (emailTest == null)
        {
            SetMessage("Email does not match username!");
            return;
        }

        if (usernameTest.requiresEmailAuth == true)
        {
            SetMessage("It looks like you're logging in from somewhere new!");
            return;
        }
        SetMessage("");

        // Switch UI to two-factor authentication mode
        UsernameInput.IsEnabled = false;
        EmailInput.IsEnabled = false;
        PasswordInput.Visibility = Visibility.Collapsed;
        CodeInput.Visibility = Visibility.Visible;
        _isCodeMode = true;
    }

    /// <summary>
    /// Handles the two-factor authentication stage.
    /// </summary>
    private void HandleTwoFactorLogin()
    {
        var auth = VRCAPI.Login(Username, Password, CodeInput.Text);
        if (auth == null)
        {
            SetMessage("Password is incorrect!");
            return;
        }

        if (string.IsNullOrEmpty(auth.Auth))
        {
            SetMessage("Sorry, we do not support email one-time codes. Please add 2FA to your account.");
            ResetInputFields();
            return;
        }

        if (string.IsNullOrEmpty(auth.TwoFactorAuth))
        {
            SetMessage("Code is invalid!");
            return;
        }
        SetMessage("");

        VRCAPI.SetAuth(auth);
        Debug("Wrote VRChat Auth!");

        if (!VRCAPI.LoggedIn)
        {
            SetMessage("Authentication is invalid!");
            ResetInputFields();
            return;
        }

        // Successful login; store auth data and close the window.
        UserAuth = auth;
        UserData = VRCAPI.GetUser();
        Warn($"Username: {UserData.username}");
        Warn($"ID: {UserData.id}");
        Warn($"Adult: {UserData.Adult}");
        Close();
    }

    /// <summary>
    /// Updates the message label and logs the message.
    /// </summary>
    private void SetMessage(string message)
    {
        Message.Content = message;
        if (!string.IsNullOrEmpty(message))
            Debug($"[VRCLogin] {message}");
    }
}
