using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using AlNeda.Admin.ViewModels;

namespace AlNeda.Admin.Views;

public partial class OffersDashboardWindow : Window
{
    private readonly OffersDashboardViewModel _dashboardVm;
    private readonly OffersViewModel _offersVm;

    public OffersDashboardWindow(OffersDashboardViewModel dashboardVm, OffersViewModel offersVm)
    {
        InitializeComponent();
        _dashboardVm = dashboardVm;
        _offersVm = offersVm;

        DataContext = _dashboardVm;
        OffersContent.DataContext = _offersVm;

        FitToScreen();
        LocationChanged += OnLocationChanged;
        Loaded += OnWindowLoaded;
    }

    private void FitToScreen()
    {
        var screen = SystemParameters.WorkArea;
        Width = Math.Min(1300, screen.Width * 0.92);
        Height = Math.Min(780, screen.Height * 0.88);
        Left = (screen.Width - Width) / 2 + screen.Left;
        Top = (screen.Height - Height) / 2 + screen.Top;
    }

    private void OnLocationChanged(object? sender, EventArgs e)
    {
        if (WindowState == WindowState.Maximized) return;
        var screen = SystemParameters.WorkArea;
        Left = Math.Max(screen.Left, Math.Min(screen.Right - Width, Left));
        Top = Math.Max(screen.Top, Math.Min(screen.Bottom - Height, Top));
    }

    private async void OnWindowLoaded(object sender, RoutedEventArgs e)
    {
        try
        {
            await Task.WhenAll(
                _dashboardVm.LoadCommand.ExecuteAsync(null),
                _offersVm.LoadCommand.ExecuteAsync(null)
            );
        }
        catch (Exception ex)
        {
            _dashboardVm.StatusMessage = $"خطأ في تحميل البيانات: {ex.Message}";
        }
    }

    private void TitleBarDrag(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left)
        {
            if (WindowState == WindowState.Maximized)
            {
                WindowState = WindowState.Normal;
                MaxBtn.Content = "\uE922";
            }
            DragMove();
        }
    }

    private void Minimize_Click(object sender, RoutedEventArgs e) =>
        WindowState = WindowState.Minimized;

    private void Maximize_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState == WindowState.Maximized
            ? WindowState.Normal
            : WindowState.Maximized;
        MaxBtn.Content = WindowState == WindowState.Maximized ? "\uE923" : "\uE922";
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}
