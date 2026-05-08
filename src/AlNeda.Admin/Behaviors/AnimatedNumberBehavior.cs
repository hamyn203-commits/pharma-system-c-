using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using Microsoft.Xaml.Behaviors;

namespace AlNeda.Admin.Behaviors;

public class AnimatedNumberBehavior : Behavior<TextBlock>
{
    public static readonly DependencyProperty TargetNumberProperty =
        DependencyProperty.Register(nameof(TargetNumber), typeof(double), typeof(AnimatedNumberBehavior),
            new PropertyMetadata(0.0, OnTargetNumberChanged));

    public double TargetNumber
    {
        get => (double)GetValue(TargetNumberProperty);
        set => SetValue(TargetNumberProperty, value);
    }

    private static void OnTargetNumberChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is AnimatedNumberBehavior behavior && behavior.AssociatedObject != null)
        {
            behavior.Animate((double)e.OldValue, (double)e.NewValue);
        }
    }

    private void Animate(double from, double to)
    {
        var animation = new DoubleAnimation
        {
            From = from,
            To = to,
            Duration = TimeSpan.FromMilliseconds(800),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };

        animation.Changed += (s, e) =>
        {
            // This is a bit tricky for TextBlock.Text directly as it's not a DP of type double.
            // We can use a Storyboard with a discrete ObjectAnimation or just update in code.
        };

        // Better approach: Animate a dummy DP and update Text in its changed handler.
        var dummy = new Dummy { Value = from };
        var dpAnimation = new DoubleAnimation(from, to, TimeSpan.FromMilliseconds(800))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };

        Storyboard.SetTarget(dpAnimation, dummy);
        Storyboard.SetTargetProperty(dpAnimation, new PropertyPath(Dummy.ValueProperty));

        var sb = new Storyboard();
        sb.Children.Add(dpAnimation);
        
        dummy.ValueChanged += (s, val) =>
        {
            if (AssociatedObject != null)
                AssociatedObject.Text = val.ToString("N0"); // Or custom format
        };

        sb.Begin();
    }

    private class Dummy : DependencyObject
    {
        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register("Value", typeof(double), typeof(Dummy), new PropertyMetadata(0.0, (d, e) => 
            {
                ((Dummy)d).ValueChanged?.Invoke(d, (double)e.NewValue);
            }));

        public double Value
        {
            get => (double)GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }

        public event EventHandler<double>? ValueChanged;
    }
}
