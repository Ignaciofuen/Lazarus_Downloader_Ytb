using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace YtdlpDesktop.Helpers;

public static class ProgressBarSmoother
{
    public static readonly DependencyProperty SmoothValueProperty =
        DependencyProperty.RegisterAttached(
            "SmoothValue",
            typeof(double),
            typeof(ProgressBarSmoother),
            new PropertyMetadata(0.0, OnSmoothValueChanged));

    public static double GetSmoothValue(DependencyObject obj) => (double)obj.GetValue(SmoothValueProperty);
    public static void SetSmoothValue(DependencyObject obj, double value) => obj.SetValue(SmoothValueProperty, value);

    private static void OnSmoothValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ProgressBar progressBar)
        {
            double targetValue = (double)e.NewValue;
            var anim = new DoubleAnimation(targetValue, TimeSpan.FromMilliseconds(200))
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            progressBar.BeginAnimation(System.Windows.Controls.Primitives.RangeBase.ValueProperty, anim);
        }
    }
}
