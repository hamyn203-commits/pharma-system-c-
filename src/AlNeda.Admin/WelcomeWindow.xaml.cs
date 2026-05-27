using System.Windows;
using System.Windows.Threading;

namespace AlNeda.Admin;

public partial class WelcomeWindow : Window
{
    private readonly DispatcherTimer _closeTimer;

    public WelcomeWindow()
    {
        InitializeComponent();

        _closeTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(10)
        };
        _closeTimer.Tick += (_, _) =>
        {
            _closeTimer.Stop();
            Close();
        };
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        _closeTimer.Start();
    }
}
