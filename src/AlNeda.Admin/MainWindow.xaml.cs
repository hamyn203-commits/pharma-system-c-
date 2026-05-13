using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using AlNeda.Admin.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace AlNeda.Admin;

public partial class MainWindow : Window
{
    public MainViewModel ViewModel { get; }
    private readonly Dictionary<string, Button> _navButtons = [];

    public MainWindow()
    {
        InitializeComponent();
        ViewModel = ((App)Application.Current).Services.GetRequiredService<MainViewModel>();
        DataContext = ViewModel;
        ViewModel.LogoutRequested += OnLogoutRequested;

        _navButtons["dashboard"] = NavDashboard;
        _navButtons["products"] = NavProducts;
        _navButtons["categories"] = NavCategories;
        _navButtons["suppliers"] = NavSuppliers;
        _navButtons["purchases"] = NavPurchases;
        _navButtons["pharmacies"] = NavPharmacies;
        _navButtons["orders"] = NavOrders;
        _navButtons["payments"] = NavPayments;
        _navButtons["returns"] = NavReturns;
        _navButtons["statements"] = NavStatements;
        _navButtons["offers"] = NavOffers;
        _navButtons["reports"] = NavReports;
        _navButtons["audit"] = NavAudit;
        _navButtons["backup"] = NavBackup;
        _navButtons["settings"] = NavSettings;

        ViewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(ViewModel.CurrentPage))
                UpdateNavHighlight();
        };

        StartClock();
    }

    private void UpdateNavHighlight()
    {
        var tag = ViewModel.ActiveNavTag;
        if (string.IsNullOrEmpty(tag)) return;

        foreach (var kvp in _navButtons)
        {
            kvp.Value.Uid = kvp.Key == tag ? "active" : null;
        }
    }

    private void NavButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button btn) return;
        var tag = btn.Tag?.ToString();
        if (string.IsNullOrEmpty(tag)) return;

        Log.Information("Navigating to: {Tag}", tag);
        UpdateNavHighlight();
        ViewModel.NavigateCommand.Execute(tag);
    }

    private async void StartClock()
    {
        var ticks = 0;
        while (true)
        {
            try
            {
                var now = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                ClockText.Text = now;
                ViewModel.FooterClock = now;

                if (ticks == 0 || ticks % 10 == 0)
                    await ViewModel.RefreshFooterHealthAsync();

                ticks++;
            }
            catch { }
            await Task.Delay(1000);
        }
    }

    private void OnLogoutRequested(object? sender, EventArgs e)
    {
        var result = MessageBox.Show("هل أنت متأكد من تسجيل الخروج؟", "تأكيد",
            MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (result == MessageBoxResult.Yes)
        {
            Log.Information("User logged out");
            new LoginWindow().Show();
            Close();
        }
    }

    private void Minimize_Click(object sender, RoutedEventArgs e)
        => WindowState = WindowState.Minimized;

    private void Maximize_Click(object sender, RoutedEventArgs e)
    {
        if (WindowState == WindowState.Maximized)
            WindowState = WindowState.Normal;
        else
            WindowState = WindowState.Maximized;
    }

    private void Close_Click(object sender, RoutedEventArgs e)
        => Close();

    private void Window_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed)
            DragMove();
    }
}
