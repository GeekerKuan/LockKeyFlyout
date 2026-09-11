using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace LockKeyFlyout;

public unsafe partial class FlyoutWindow : Window
{
    readonly DispatcherTimer closeTimer = new();
    bool initialized, closing;
    double targetTop;

    public FlyoutWindow()
    {
        InitializeComponent();
        closeTimer.Tick += (_, _) => BeginExit();
        SourceInitialized += (_, _) => ConfigureWindow();
    }

    void ConfigureWindow()
    {
        initialized = true;
        nint hwnd = new WindowInteropHelper(this).Handle;
        Native.ACCENT_POLICY accent = new() { AccentState = 4, AccentFlags = 2, GradientColor = 0xD4232323 };
        Native.WINDOWCOMPOSITIONATTRIBDATA data = new() { Attrib = 19, pvData = (nint)(&accent), cbData = sizeof(Native.ACCENT_POLICY) };
        Native.SetWindowCompositionAttribute(hwnd, &data);
    }

    public void Present(string text, bool isOn, int rawX, int rawY, uint dpi, int duration, bool animate, bool bold)
    {
        closeTimer.Stop(); closing = false;
        double scale = dpi / 96.0;
        Left = rawX / scale;
        targetTop = rawY / scale;
        MessageText.Text = text;
        MessageText.FontWeight = bold ? FontWeights.SemiBold : FontWeights.Normal;
        AnimateState(isOn, animate);

        if (!IsVisible)
        {
            Top = targetTop + 20;
            Opacity = animate ? 0 : 1;
            Show();
            if (initialized && animate) BeginEntrance(); else Top = targetTop;
        }
        else
        {
            BeginAnimation(TopProperty, null);
            Top = targetTop;
            Opacity = 1;
        }
        closeTimer.Interval = TimeSpan.FromMilliseconds(duration);
        closeTimer.Start();
    }

    static IEasingFunction Spring() => new BackEase { Amplitude = 0.14, EasingMode = EasingMode.EaseOut };
    static IEasingFunction Smooth() => new CubicEase { EasingMode = EasingMode.EaseOut };

    void BeginEntrance()
    {
        var story = new Storyboard();
        story.Children.Add(new DoubleAnimation(Top, targetTop, TimeSpan.FromMilliseconds(260)) { EasingFunction = Spring() });
        story.Children.Add(new DoubleAnimation(Opacity, 1, TimeSpan.FromMilliseconds(180)) { EasingFunction = Smooth() });
        Storyboard.SetTargetProperty(story.Children[0], new PropertyPath(TopProperty));
        Storyboard.SetTargetProperty(story.Children[1], new PropertyPath(OpacityProperty));
        story.Begin(this, HandoffBehavior.SnapshotAndReplace, true);
    }

    void AnimateState(bool isOn, bool animate)
    {
        double toWidth = isOn ? 60 : 36;
        Color toColor = isOn ? Color.FromRgb(0x60, 0xCD, 0xFF) : Color.FromRgb(0x80, 0x80, 0x80);
        double toAngle = isOn ? 0 : 24;
        if (!animate) { Indicator.BeginAnimation(WidthProperty, null); Indicator.Width = toWidth; IndicatorBrush.BeginAnimation(SolidColorBrush.ColorProperty, null); IndicatorBrush.Color = toColor; ShackleTransform.BeginAnimation(RotateTransform.AngleProperty, null); ShackleTransform.Angle = toAngle; return; }
        Indicator.BeginAnimation(WidthProperty, new DoubleAnimation(Indicator.ActualWidth, toWidth, TimeSpan.FromMilliseconds(190)) { EasingFunction = Smooth() }, HandoffBehavior.SnapshotAndReplace);
        IndicatorBrush.BeginAnimation(SolidColorBrush.ColorProperty, new ColorAnimation(IndicatorBrush.Color, toColor, TimeSpan.FromMilliseconds(190)) { EasingFunction = Smooth() }, HandoffBehavior.SnapshotAndReplace);
        ShackleTransform.BeginAnimation(RotateTransform.AngleProperty, new DoubleAnimation(ShackleTransform.Angle, toAngle, TimeSpan.FromMilliseconds(190)) { EasingFunction = Smooth() }, HandoffBehavior.SnapshotAndReplace);
    }

    void BeginExit()
    {
        closeTimer.Stop(); if (closing || !IsVisible) return; closing = true;
        var story = new Storyboard();
        story.Children.Add(new DoubleAnimation(Top, targetTop + 9, TimeSpan.FromMilliseconds(150)) { EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn } });
        story.Children.Add(new DoubleAnimation(Opacity, 0, TimeSpan.FromMilliseconds(145)) { EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn } });
        Storyboard.SetTargetProperty(story.Children[0], new PropertyPath(TopProperty)); Storyboard.SetTargetProperty(story.Children[1], new PropertyPath(OpacityProperty));
        story.Completed += (_, _) => { if (closing) { Hide(); Opacity = 1; closing = false; } };
        story.Begin(this, HandoffBehavior.SnapshotAndReplace, true);
    }
}
