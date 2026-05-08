using System.Collections.ObjectModel;
using System.Net.Http;
using AlNeda.Admin.Services;
using AlNeda.Admin.Services.ApiClient;
using AlNeda.Core.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using Timer = System.Timers.Timer;

namespace AlNeda.Admin.ViewModels;

public partial class DashboardViewModel : ObservableObject
{
    private readonly IAlNedaApiClient _apiClient;
    private readonly INavigationService _navigationService;
    private readonly IDialogService _dialog;
    private Timer? _alertTimer;
    private bool _alertsChecked;

    [ObservableProperty] private DashboardStats _stats = new();
    [ObservableProperty] private bool _isLoading = true;
    [ObservableProperty] private string _currentDate = DateTime.Now.ToString("dddd، dd MMMM yyyy", new System.Globalization.CultureInfo("ar-EG"));
    [ObservableProperty] private string _lastBackup = "غير متوفر";

    // Chart Properties
    [ObservableProperty] private ISeries[] _salesSeries = [];
    [ObservableProperty] private Axis[] _xAxes = [];
    [ObservableProperty] private Axis[] _yAxes = [];

    [ObservableProperty] private ObservableCollection<ProductAlertDto> _lowStockProducts = [];
    [ObservableProperty] private ObservableCollection<ProductAlertDto> _expiringProducts = [];
    [ObservableProperty] private int _lowStockCount;
    [ObservableProperty] private int _expiringCount;
    [ObservableProperty] private int _expiredCount;
    [ObservableProperty] private bool _hasAlerts;
    [ObservableProperty] private string _alertSummary = "";
    [ObservableProperty] private bool _showAlertNotification;

    public DashboardViewModel(IAlNedaApiClient apiClient, INavigationService navigationService, IDialogService dialog)
    {
        _apiClient = apiClient;
        _navigationService = navigationService;
        _dialog = dialog;

        _alertTimer = new Timer(TimeSpan.FromMinutes(30).TotalMilliseconds);
        _alertTimer.Elapsed += (s, e) => _ = CheckAlertsAsync();
        _alertTimer.AutoReset = true;
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        IsLoading = true;
        
        try
        {
            // 1. Load Main Stats
            var stats = await _apiClient.GetDashboardStatsAsync();
            if (stats != null) Stats = stats;

            // 2. Load Chart Data
            var salesData = await _apiClient.GetLast30DaysSalesAsync();
            if (salesData != null)
            {
                SalesSeries = new ISeries[]
                {
                    new LineSeries<decimal>
                    {
                        Values = salesData.Select(x => x.Value).ToArray(),
                        Name = "المبيعات",
                        Fill = new LinearGradientPaint(new SKColor(16, 185, 129, 50), new SKColor(16, 185, 129, 0)),
                        Stroke = new SolidColorPaint(new SKColor(16, 185, 129)) { StrokeThickness = 3 },
                        GeometrySize = 8,
                        GeometryStroke = new SolidColorPaint(new SKColor(16, 185, 129)) { StrokeThickness = 2 }
                    }
                };

                XAxes = new Axis[]
                {
                    new Axis
                    {
                        Labels = salesData.Select(x => x.Label).ToArray(),
                        LabelsRotation = 45,
                        SeparatorsPaint = new SolidColorPaint(new SKColor(61, 69, 82, 50))
                    }
                };

                YAxes = new Axis[]
                {
                    new Axis
                    {
                        Labeler = value => value.ToString("N0"),
                        SeparatorsPaint = new SolidColorPaint(new SKColor(61, 69, 82, 50))
                    }
                };
            }

            // 3. Load Alerts
            await CheckAlertsAsync();

            // 4. Load Backup Info
            try
            {
                var backupDir = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "backups");
                if (System.IO.Directory.Exists(backupDir))
                {
                    var latest = System.IO.Directory.GetFiles(backupDir, "*.db")
                        .OrderByDescending(f => System.IO.File.GetCreationTime(f))
                        .FirstOrDefault();
                    if (latest != null)
                        LastBackup = System.IO.File.GetCreationTime(latest).ToString("yyyy-MM-dd HH:mm");
                }
            }
            catch { }

            _alertTimer?.Start();
        }
        catch (HttpRequestException)
        {
            _dialog.ShowError("تعذر الاتصال بالخادم. تأكد من تشغيل خدمة API", "خطأ في الاتصال");
        }

        await Task.Delay(500);
        IsLoading = false;
    }

    // Navigation Commands
    [RelayCommand] private void NavToReceivables() => _navigationService.NavigateTo("Pharmacies");
    [RelayCommand] private void NavToActiveOrders() => _navigationService.NavigateTo("Orders");
    [RelayCommand] private void NavToPharmacies() => _navigationService.NavigateTo("Pharmacies");
    [RelayCommand] private void NavToProducts() => _navigationService.NavigateTo("Products");
    [RelayCommand] private void NavToDailySales() => _navigationService.NavigateTo("Reports");
    [RelayCommand] private void NavToMonthlySales() => _navigationService.NavigateTo("Reports");

    [RelayCommand]
    private async Task CheckAlertsAsync()
    {
        try
        {
            var lowStock = await _apiClient.GetLowStockProductsAsync();
            var expiring = await _apiClient.GetExpiringProductsAsync();
            var expired = await _apiClient.GetExpiredProductsAsync();

            System.Windows.Application.Current?.Dispatcher?.Invoke(() =>
            {
                LowStockProducts = new ObservableCollection<ProductAlertDto>(lowStock ?? []);
                ExpiringProducts = new ObservableCollection<ProductAlertDto>(expiring ?? []);
            });

            LowStockCount = lowStock?.Count ?? 0;
            ExpiringCount = expiring?.Count ?? 0;
            ExpiredCount = expired?.Count ?? 0;

            HasAlerts = LowStockCount > 0 || ExpiringCount > 0 || ExpiredCount > 0;

            var summaryParts = new List<string>();
            if (LowStockCount > 0) summaryParts.Add($"مخزون منخفض: {LowStockCount}");
            if (ExpiringCount > 0) summaryParts.Add($"تنتهي قريباً: {ExpiringCount}");
            if (ExpiredCount > 0) summaryParts.Add($"منتهي: {ExpiredCount}");
            AlertSummary = summaryParts.Count > 0 ? string.Join(" | ", summaryParts) : "";

            if (HasAlerts && !_alertsChecked)
            {
                _alertsChecked = true;
                ShowAlertNotification = true;

                var message = "تنبيهات المخزون:\n\n";
                if (LowStockCount > 0) message += $"{LowStockCount} منتج مخزونها منخفض\n";
                if (ExpiringCount > 0) message += $"{ExpiringCount} منتج تنتهي صلاحيتها قريباً\n";
                if (ExpiredCount > 0) message += $"{ExpiredCount} منتج منتهي الصلاحية\n";
                message += "\nراجع لوحة التحكم للتفاصيل";

                System.Windows.Application.Current?.Dispatcher?.Invoke(() =>
                {
                    var result = System.Windows.MessageBox.Show(
                        message,
                        "تنبيهات المخزون",
                        System.Windows.MessageBoxButton.OKCancel,
                        System.Windows.MessageBoxImage.Warning
                    );

                    ShowAlertNotification = result == System.Windows.MessageBoxResult.OK;
                });
            }
        }
        catch { }
    }

    [RelayCommand]
    private async Task RefreshAlertsAsync()
    {
        await CheckAlertsAsync();
    }

    [RelayCommand]
    private void DismissNotification()
    {
        ShowAlertNotification = false;
    }

    [RelayCommand]
    private async Task ShowLowStockProductsAsync()
    {
        var products = await _apiClient.GetLowStockProductsAsync();
        if (products == null || products.Count == 0)
        {
            _dialog.ShowMessage("جميع المنتجات لديها مخزون كافٍ", "مخزون منخفض");
            return;
        }

        var message = "المنتجات ذات المخزون المنخفض:\n\n" +
            string.Join("\n", products.Select(p => $"• {p.Name} - الكمية: {p.Quantity}"));
        _dialog.ShowMessage(message, $"مخزون منخفض ({products.Count})");
    }

    [RelayCommand]
    private async Task ShowExpiringProductsAsync()
    {
        var products = await _apiClient.GetExpiringProductsAsync();
        if (products == null || products.Count == 0)
        {
            _dialog.ShowMessage("لا توجد منتجات تنتهي صلاحيتها قريباً", "منتجات تنتهي قريباً");
            return;
        }

        var message = "المنتجات التي تنتهي صلاحيتها خلال 30 يوم:\n\n" +
            string.Join("\n", products.Select(p => $"• {p.Name} - تاريخ الانتهاء: {p.ExpiryDate}"));
        _dialog.ShowMessage(message, $"تنتهي قريباً ({products.Count})");
    }

    [RelayCommand]
    private async Task ShowExpiredProductsAsync()
    {
        var products = await _apiClient.GetExpiredProductsAsync();
        if (products == null || products.Count == 0)
        {
            _dialog.ShowMessage("لا توجد منتجات منتهية الصلاحية", "منتجات منتهية");
            return;
        }

        var message = "المنتجات منتهية الصلاحية:\n\n" +
            string.Join("\n", products.Select(p => $"• {p.Name} - تاريخ الانتهاء: {p.ExpiryDate}"));
        _dialog.ShowMessage(message, $"منتهية الصلاحية ({products.Count})");
    }

    public void Cleanup()
    {
        _alertTimer?.Stop();
        _alertTimer?.Dispose();
        _alertTimer = null;
    }
}