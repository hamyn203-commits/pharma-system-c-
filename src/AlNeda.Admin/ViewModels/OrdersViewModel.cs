using System.Collections.ObjectModel;
using System.Data;
using System.IO;
using System.Windows.Data;
using AlNeda.Admin.Services;
using AlNeda.Admin.Services.ApiClient;
using AlNeda.Core.Entities;
using AlNeda.Core.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AlNeda.Admin.ViewModels;

public partial class OrdersViewModel : ObservableObject
{
    private readonly IAlNedaApiClient _apiClient;
    private readonly PrintingService _printingService;
    private readonly IDialogService _dialog;

    [ObservableProperty] private ObservableCollection<OrderDto> _orders = [];
    [ObservableProperty] private OrderDto? _selectedOrder;
    [ObservableProperty] private Order? _selectedDetailOrder;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string _statusMessage = "";

    [ObservableProperty] private string _statusFilter = "";
    [ObservableProperty] private string _sourceFilter = "";
    [ObservableProperty] private string _searchText = "";
    [ObservableProperty] private string _barcodeSearch = "";
    [ObservableProperty] private DateTime _filterFrom = DateTime.Today.AddMonths(-1);
    [ObservableProperty] private DateTime _filterTo = DateTime.Today;
    [ObservableProperty] private bool _showDetail;

    [ObservableProperty] private ObservableCollection<PharmacyDto> _pharmacies = [];
    [ObservableProperty] private ObservableCollection<ProductDto> _products = [];
    [ObservableProperty] private ObservableCollection<ProductDto> _filteredProducts = [];
    [ObservableProperty] private ObservableCollection<OrderItem> _newItems = [];
    [ObservableProperty] private bool _showEditor;
    [ObservableProperty] private int _selectedPharmacyId;
    [ObservableProperty] private decimal _totalAmount;
    [ObservableProperty] private decimal _discount;
    [ObservableProperty] private string _discountType = "value";
    [ObservableProperty] private ProductDto? _selectedProductForOrder;
    [ObservableProperty] private string _validationMessage = "";

    [ObservableProperty] private int _todayOrdersCount;
    [ObservableProperty] private int _pendingOrdersCount;
    [ObservableProperty] private decimal _monthlyRevenue;
    [ObservableProperty] private int _totalOrdersCount;

    public string[] StatusFilters { get; } = ["", "pending", "reviewed", "in_store", "with_driver", "on_the_way", "delivered", "postponed", "cancelled"];
    public string[] SourceFilters { get; } = ["", "admin", "mobile"];
    public string[] DiscountTypes { get; } = ["value", "percent"];

    public static string StatusDisplayName(string s) => s switch
    {
        "pending" => "قيد الانتظار",
        "reviewed" => "تم المراجعة",
        "in_store" => "في المخزن",
        "with_driver" => "مع المندوب",
        "on_the_way" => "في الطريق",
        "delivered" => "تم التسليم",
        "postponed" => "مؤجل",
        "cancelled" => "ملغي",
        "" => "الكل",
        _ => s
    };

    public OrdersViewModel(IAlNedaApiClient apiClient, PrintingService printingService, IDialogService dialog)
    {
        _apiClient = apiClient;
        _printingService = printingService;
        _dialog = dialog;
    }

    partial void OnSearchTextChanged(string value) => ApplyFilter();
    partial void OnStatusFilterChanged(string value) => ApplyFilter();
    partial void OnSourceFilterChanged(string value) => ApplyFilter();
    partial void OnFilterFromChanged(DateTime value) => ApplyFilter();
    partial void OnFilterToChanged(DateTime value) => ApplyFilter();

    private void ApplyFilter()
    {
        var view = CollectionViewSource.GetDefaultView(Orders);
        if (view == null) return;
        view.Filter = o =>
        {
            if (o is not OrderDto order) return false;
            if (!string.IsNullOrEmpty(StatusFilter) && !order.Status.Equals(StatusFilter, StringComparison.OrdinalIgnoreCase))
                return false;
            if (!string.IsNullOrEmpty(SourceFilter) && !order.Source.Equals(SourceFilter, StringComparison.OrdinalIgnoreCase))
                return false;
            if (order.CreatedAt < FilterFrom || order.CreatedAt > FilterTo.AddDays(1))
                return false;
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var q = SearchText.Trim();
                if (!(order.OrderNumber?.Contains(q, StringComparison.OrdinalIgnoreCase) == true ||
                      order.PharmacyName?.Contains(q, StringComparison.OrdinalIgnoreCase) == true))
                    return false;
            }
            return true;
        };
        RecalcKpis();
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        IsLoading = true;
        StatusMessage = "جاري تحميل الطلبات...";
        try
        {
            var orders = await _apiClient.GetOrdersAsync(null);
            Orders = new ObservableCollection<OrderDto>(orders);
            var phs = await _apiClient.GetPharmaciesAsync();
            Pharmacies = new ObservableCollection<PharmacyDto>(phs);
            var prods = await _apiClient.GetProductsAsync();
            Products = new ObservableCollection<ProductDto>(prods);
            FilteredProducts = new ObservableCollection<ProductDto>(Products);
            ApplyFilter();
            StatusMessage = $"تم تحميل {Orders.Count} طلب";
        }
        catch (ApiException ex)
        {
            StatusMessage = "خطأ في تحميل الطلبات";
            _dialog.ShowError(ex.Message, "خطأ");
        }
        IsLoading = false;
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        await LoadAsync();
    }

    [RelayCommand]
    private Task QuickReviewAsync(OrderDto? order)
    {
        if (order == null) return Task.CompletedTask;
        SelectedOrder = order;
        return TransitionStatusAsync("reviewed");
    }

    [RelayCommand]
    private Task QuickPrepareAsync(OrderDto? order)
    {
        if (order == null) return Task.CompletedTask;
        SelectedOrder = order;
        return TransitionStatusAsync("in_store");
    }

    [RelayCommand]
    private Task SendToDriverAsync(OrderDto? order)
    {
        if (order == null) return Task.CompletedTask;
        SelectedOrder = order;
        return TransitionStatusAsync("with_driver");
    }

    private void RecalcKpis()
    {
        var today = DateTime.Today;
        var monthStart = new DateTime(today.Year, today.Month, 1);
        TodayOrdersCount = Orders.Count(o => o.CreatedAt.Date == today);
        PendingOrdersCount = Orders.Count(o => o.Status is "pending" or "reviewed" or "in_store" or "with_driver" or "on_the_way");
        MonthlyRevenue = Orders.Where(o => o.CreatedAt >= monthStart && o.Status == "delivered").Sum(o => o.FinalTotal);
        TotalOrdersCount = Orders.Count;
    }

    [RelayCommand]
    private async Task ViewDetailAsync(OrderDto? order)
    {
        if (order == null) return;
        StatusMessage = "جاري تحميل تفاصيل الطلب...";
        try
        {
            var full = await _printingService.GetOrderWithDetailsAsync(order.Id);
            if (full != null)
            {
                SelectedDetailOrder = full;
                SelectedOrder = order;
                ShowDetail = true;
                StatusMessage = $"الطلب #{full.OrderNumber}";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"خطأ: {ex.Message}";
        }
    }

    [RelayCommand]
    private void CloseDetail() => ShowDetail = false;

    [RelayCommand]
    private async Task TransitionStatusAsync(string? newStatus)
    {
        if (SelectedOrder == null || string.IsNullOrEmpty(newStatus)) return;
        if (newStatus == "cancelled" && !_dialog.Confirm("هل أنت متأكد من إلغاء هذا الطلب؟")) return;
        if (newStatus == "delivered" && !_dialog.Confirm("هل تم تسليم هذا الطلب فعلاً؟")) return;
        try
        {
            StatusMessage = $"جاري تغيير الحالة إلى {StatusDisplayName(newStatus)}...";
            await _apiClient.UpdateOrderStatusAsync(SelectedOrder.Id, newStatus);
            StatusMessage = $"تم تغيير حالة الطلب #{SelectedOrder.OrderNumber} إلى {StatusDisplayName(newStatus)}";
            await LoadAsync();
            if (ShowDetail && SelectedDetailOrder != null)
            {
                SelectedDetailOrder.Status = newStatus;
                SelectedOrder.Status = newStatus;
            }
        }
        catch (ApiException ex)
        {
            StatusMessage = "خطأ في تغيير الحالة";
            _dialog.ShowError(ex.Message, "خطأ");
        }
    }

    [RelayCommand]
    private async Task PrintGridOrderAsync(OrderDto? dto)
    {
        if (dto == null) { StatusMessage = "الرجاء اختيار طلب"; return; }
        try
        {
            var full = await _printingService.GetOrderWithDetailsAsync(dto.Id);
            if (full == null) { StatusMessage = "خطأ في تحميل بيانات الطلب"; return; }
            if (_printingService.PrintOrder(full))
                StatusMessage = $"تم إرسال الفاتورة #{full.OrderNumber} إلى الطابعة";
            else
                StatusMessage = "تم إلغاء الطباعة";
        }
        catch (Exception ex) { StatusMessage = $"خطأ: {ex.Message}"; }
    }

    [RelayCommand]
    private Task PrintDetailOrderAsync()
    {
        if (SelectedDetailOrder == null) 
        { 
            StatusMessage = "الرجاء اختيار طلب"; 
            return Task.CompletedTask; 
        }
        try
        {
            if (_printingService.PrintOrder(SelectedDetailOrder))
                StatusMessage = $"تم إرسال الفاتورة #{SelectedDetailOrder.OrderNumber} إلى الطابعة";
            else
                StatusMessage = "تم إلغاء الطباعة";
        }
        catch (Exception ex) { StatusMessage = $"خطأ: {ex.Message}"; }
        return Task.CompletedTask;
    }

    [RelayCommand]
    private async Task ExportHtmlAsync()
    {
        if (SelectedDetailOrder == null) { StatusMessage = "الرجاء اختيار طلب"; return; }
        try
        {
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Title = "تصدير فاتورة HTML",
                Filter = "HTML Files (*.html)|*.html",
                FileName = $"Invoice_{SelectedDetailOrder.OrderNumber}.html"
            };
            if (dialog.ShowDialog() == true)
            {
                var html = _printingService.ExportOrderToHtml(SelectedDetailOrder);
                await File.WriteAllTextAsync(dialog.FileName, html, System.Text.Encoding.UTF8);
                StatusMessage = $"تم التصدير: {Path.GetFileName(dialog.FileName)}";
            }
        }
        catch (Exception ex) { StatusMessage = $"خطأ: {ex.Message}"; }
    }

    [RelayCommand]
    private async Task ExportCsvAsync()
    {
        try
        {
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Title = "تصدير الطلبات CSV",
                Filter = "CSV Files (*.csv)|*.csv",
                FileName = $"Orders_{DateTime.Today:yyyy-MM-dd}.csv"
            };
            if (dialog.ShowDialog() != true) return;
            using var writer = new StreamWriter(dialog.FileName, false, System.Text.Encoding.UTF8);
            await writer.WriteLineAsync("رقم الطلب,الصيدلية,الإجمالي,الخصم,الصافي,الحالة,التاريخ");
            foreach (var o in Orders)
            {
                await writer.WriteLineAsync($"\"{o.OrderNumber}\",\"{o.PharmacyName}\",{o.TotalAmount},{o.Discount},{o.FinalTotal},\"{StatusDisplayName(o.Status)}\",{o.CreatedAt:yyyy-MM-dd HH:mm}");
            }
            StatusMessage = $"تم تصدير {Orders.Count} طلب إلى {Path.GetFileName(dialog.FileName)}";
        }
        catch (Exception ex) { StatusMessage = $"خطأ: {ex.Message}"; }
    }

    [RelayCommand]
    private void NewOrder()
    {
        NewItems = [];
        TotalAmount = 0;
        Discount = 0;
        DiscountType = "value";
        SelectedPharmacyId = 0;
        ValidationMessage = "";
        ShowEditor = true;
    }

    partial void OnBarcodeSearchChanged(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            FilteredProducts = new ObservableCollection<ProductDto>(Products);
        }
        else
        {
            var filtered = Products.Where(p =>
                (p.Barcode?.Contains(value, StringComparison.OrdinalIgnoreCase) == true) ||
                (p.Name?.Contains(value, StringComparison.OrdinalIgnoreCase) == true))
                .Take(10).ToList();
            FilteredProducts = new ObservableCollection<ProductDto>(filtered);
        }
    }

    [RelayCommand]
    private void AddProductByBarcode()
    {
        if (SelectedProductForOrder == null) return;
        var existing = NewItems.FirstOrDefault(i => i.ProductId == SelectedProductForOrder.Id);
        if (existing != null)
        {
            existing.Quantity++;
            existing.TotalPrice = existing.Quantity * existing.UnitPrice;
        }
        else
        {
            NewItems.Add(new OrderItem
            {
                ProductId = SelectedProductForOrder.Id,
                Quantity = 1,
                UnitPrice = SelectedProductForOrder.UnitPrice,
                TotalPrice = SelectedProductForOrder.UnitPrice
            });
        }
        RecalcNewTotal();
        BarcodeSearch = "";
        SelectedProductForOrder = null;
    }

    [RelayCommand]
    private void AddItem()
    {
        NewItems.Add(new OrderItem());
    }

    [RelayCommand]
    private void RemoveItem(OrderItem? item)
    {
        if (item != null) NewItems.Remove(item);
        RecalcNewTotal();
    }

    private void RecalcNewTotal()
    {
        TotalAmount = NewItems.Sum(i => i.Quantity * i.UnitPrice);
    }

    [RelayCommand]
    private async Task SaveOrderAsync()
    {
        ValidationMessage = "";
        if (SelectedPharmacyId == 0) { ValidationMessage = "اختر الصيدلية"; return; }
        if (TotalAmount <= 0) { ValidationMessage = "الإجمالي يجب أن يكون أكبر من صفر"; return; }

        var validItems = NewItems.Where(i => i.ProductId > 0 && i.Quantity > 0).ToList();
        if (validItems.Count == 0) { ValidationMessage = "أضف منتجات على الأقل"; return; }

        foreach (var item in validItems)
        {
            var product = Products.FirstOrDefault(p => p.Id == item.ProductId);
            if (product == null) { ValidationMessage = $"منتج غير موجود"; return; }
            if (product.Quantity < item.Quantity)
            {
                ValidationMessage = $"الكمية المتوفرة من '{product.Name}' هي {product.Quantity} فقط";
                return;
            }
        }

        var request = new CreateOrderRequest
        {
            PharmacyId = SelectedPharmacyId,
            TotalAmount = TotalAmount,
            Discount = Discount,
            DiscountType = DiscountType,
            Items = validItems.Select(i => new CreateOrderItemRequest
            {
                ProductId = i.ProductId,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice
            }).ToList()
        };

        try
        {
            StatusMessage = "جاري إنشاء الطلب...";
            var result = await _apiClient.CreateOrderAsync(request);
            if (result != null)
            {
                _dialog.ShowMessage($"تم إنشاء الطلب #{result.OrderNumber} بنجاح", "نجاح");
                StatusMessage = $"تم إنشاء الطلب #{result.OrderNumber}";
            }
            ShowEditor = false;
            await LoadAsync();
        }
        catch (ApiException ex)
        {
            ValidationMessage = ex.Message;
            StatusMessage = "خطأ في إنشاء الطلب";
        }
    }

    [RelayCommand]
    private void CancelEdit() => ShowEditor = false;
}
