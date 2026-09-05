using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;

namespace StreamElementsToStreamerBotOverlayMigrator.Behaviors;

public static class GlowBehavior
{
    public static readonly DependencyProperty IsGlowingProperty = DependencyProperty.RegisterAttached
    (
        "IsGlowing",
        typeof(bool),
        typeof(GlowBehavior),
        new PropertyMetadata(false, OnIsGlowingChanged)
    );

    public static void SetIsGlowing(UIElement element, bool value)
        => element.SetValue(IsGlowingProperty, value);

    public static bool GetIsGlowing(UIElement element)
        => (bool) element.GetValue(IsGlowingProperty);

    private static readonly Color GlowColor = Color.FromRgb(0x6E, 0xB4, 0xFF); //TODO move to AppColors

    private static void OnIsGlowingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not FrameworkElement element)
            return;

        if ((bool) e.NewValue)
            StartGlow(element);
        else
            StopGlow(element);
    }

    private static void StartGlow(FrameworkElement element)
    {
        var effect = new DropShadowEffect
        {
            Color       = GlowColor,
            ShadowDepth = 0,
            BlurRadius  = 8,
            Opacity     = 0.85
        };
        element.Effect = effect;

        var blurAnimation = new DoubleAnimation
        {
            From           = 6,
            To             = 16,
            Duration       = TimeSpan.FromSeconds(0.9),
            AutoReverse    = true,
            RepeatBehavior = RepeatBehavior.Forever,
            EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut }
        };

        var opacityAnimation = new DoubleAnimation
        {
            From           = 0.6,
            To             = 1.0,
            Duration       = TimeSpan.FromSeconds(0.9),
            AutoReverse    = true,
            RepeatBehavior = RepeatBehavior.Forever,
            EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut }
        };

        effect.BeginAnimation(DropShadowEffect.BlurRadiusProperty, blurAnimation);
        effect.BeginAnimation(DropShadowEffect.OpacityProperty, opacityAnimation);
    }

    private static void StopGlow(FrameworkElement element)
    {
        if (element.Effect is DropShadowEffect effect)
        {
            effect.BeginAnimation(DropShadowEffect.BlurRadiusProperty, null);
            effect.BeginAnimation(DropShadowEffect.OpacityProperty, null);
        }

        element.Effect = null;
    }
}