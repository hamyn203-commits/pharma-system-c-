using System.Collections.ObjectModel;
using AlNeda.Core.Entities;
using AlNeda.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AlNeda.Admin.ViewModels;

public partial class DashboardViewModel : ObservableObject
{
    private readonly ProductService _productService;
    private readonly PharmacyService _pharmacyService;
    private readonly OrderService _orderService;

    [ObservableProperty] private int _productCount;
    [ObservableProperty] private int _pharmacyCount;
    [ObservableProperty] private int _activeOrderCount;
    [ObservableProperty] private decimal _totalBalance;
    [ObservableProperty] private decimal _todaySales;
    [ObservableProperty] private decimal _monthlySales;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private int _activeProducts;
    [ObservableProperty] private int _todayOrders;
    [ObservableProperty] private int _pendingPayments;
    [ObservableProperty] private string _currentDate = DateTime.Now.ToString("dddd، dd MMMM yyyy", new System.Globalization.CultureInfo("ar-EG"));
    [ObservableProperty] private string _lastBackup = "غير متوفر";

    public DashboardViewModel(ProductService productService, PharmacyService pharmacyService, OrderService orderService)
    {
        _productService = productService;
        _pharmacyService = pharmacyService;
        _orderService = orderService;
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        IsLoading = true;
        await Task.Delay(100);

        var products = await _productService.GetAllAsync();
        ProductCount = products.Count;
        ActiveProducts = products.Count(p => p.IsActive == 1 && p.Quantity > 0);

        var pharmacies = await _pharmacyService.GetAllAsync();
        PharmacyCount = pharmacies.Count;
        TotalBalance = pharmacies.Sum(p => p.Balance);
        PendingPayments = pharmacies.Count(p => p.Balance > 0);

        var orders = await _orderService.GetAllAsync();
        ActiveOrderCount = orders.Count(o => o.Status != "delivered" && o.Status != "cancelled");
        TodayOrders = orders.Count(o => o.CreatedAt.Date == DateTime.Today);

        var today = DateTime.Today;
        TodaySales = orders.Where(o => o.CreatedAt.Date == today).Sum(o => o.FinalTotal);
        MonthlySales = orders.Where(o => o.CreatedAt.Year == today.Year && o.CreatedAt.Month == today.Month).Sum(o => o.FinalTotal);

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

        IsLoading = false;
    }
}
