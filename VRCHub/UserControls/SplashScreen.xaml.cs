using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Threading;
using VRCHub.Resources;
using static VRCHub.Common;

namespace VRCHub;
public partial class SplashScreen : Window, IDisposable
{
    private static SplashScreen? _instance;
    private static readonly object _lock = new();
    private readonly TimeSpan _fadeInDuration = TimeSpan.FromSeconds(0.75);
    private readonly TimeSpan _fadeOutDuration = TimeSpan.FromSeconds(0.5);
    private readonly DispatcherTimer _textAnimationTimer;
    private int _textAnimationState = 0;
    internal static string BaseText = "INITIALIZING COMPONENTS";

    private SplashScreen()
    {
        Hide();
        InitializeComponent();
        this.BringIntoView();
        this.Topmost = true; // Ensure the splash screen is on top

        _textAnimationTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(300) // Adjust interval as needed
        };
        _textAnimationTimer.Tick += OnTextAnimationTick;
    }

    public static SplashScreen Create()
    {
        lock (_lock)
        {
            if (_instance != null)
                return _instance;

            _instance = new SplashScreen();
            if (File.Exists(Config.VRChatInstallPath))
                _instance.Splash.Source = GetImageSource(File.ReadAllBytes(SplashscreenEditor.SplashScreenPath));
            else
                _instance.Splash.Source = GetImageSource(AppResources.SplashScreen);
            _instance.MainText.Text = BaseText;
            _instance.StartFadeIn();
            _instance.Show();
            return _instance;
        }
    }

    public async Task EndAsync()
    {
        if (_instance == null) return;

        await Task.Delay((int)_fadeInDuration.TotalMilliseconds);
        _instance.StartFadeOut();
        await Task.Delay((int)_fadeOutDuration.TotalMilliseconds);
        Dispose();
    }

    public void StartFadeIn()
    {
        var fadeInAnimation = new DoubleAnimation
        {
            From = 0,
            To = 1,
            Duration = _fadeInDuration
        };
        fadeInAnimation.Completed += (s, e) => OnFadeInCompleted();
        this.BeginAnimation(OpacityProperty, fadeInAnimation);
        var blurAnimation = new DoubleAnimation
        {
            From = 20,
            To = 0,
            Duration = TimeSpan.FromSeconds(1)
        };
        blureffect.BeginAnimation(BlurEffect.RadiusProperty, blurAnimation);
    }

    private void OnFadeInCompleted() { }

    public void StartFadeOut()
    {
        var fadeOutAnimation = new DoubleAnimation
        {
            From = 1,
            To = 0,
            Duration = _fadeOutDuration
        };
        this.BeginAnimation(UIElement.OpacityProperty, fadeOutAnimation);
        var blurAnimation = new DoubleAnimation
        {
            From = 0,
            To = 30,
            Duration = TimeSpan.FromSeconds(0.5)
        };
        blureffect.BeginAnimation(BlurEffect.RadiusProperty, blurAnimation);
    }

    public void SetText(string Text)
    {
       Application.Current.Dispatcher.Invoke(() => MainText.Text = Text);
    }
    public void StartTextAnimation()
    {
        _textAnimationState = 0;
        _textAnimationTimer.Start();
    }

    public void EndTextAnimation()
    {
        _textAnimationTimer.Stop();
        this.MainText.Text = BaseText;
    }

    private void OnTextAnimationTick(object? sender, EventArgs e)
    {
        _textAnimationState = (_textAnimationState + 1) % 4;
        this.MainText.Text = BaseText + new string('.', _textAnimationState);
    }
    private void SplashScreen_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left)
        {
            this.DragMove();
        }
    }


    public void Dispose()
    {
        if (_instance != null)
        {
            _instance.Close();
            _instance = null;
        }

    }
    void IDisposable.Dispose()
    {
        Dispose();
    }
}
