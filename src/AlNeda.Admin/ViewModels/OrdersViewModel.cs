using System.Collections.ObjectModel;
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
    [ObservableProperty] private ObservableCollection<PharmacyDto> _pharmacies = [];
    [ObservableProperty] private ObservableCollection<ProductDto> _products = [];
    [ObservableProperty] private ObservableCollection<ProductDto> _filteredProducts = [];
    [ObservableProperty] private ObservableCollection<OrderItem> _newItems = [];
    [ObservableProperty] private Order? _selectedOrder;
    [ObservableProperty] private bool _showEditor;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private int _selectedPharmacyId;
    [ObservableProperty] private decimal _totalAmount;
    [ObservableProperty] private decimal _discount;
    [ObservableProperty] private string _discountType = "value";
    [ObservableProperty] private string _statusFilter = "";
    [ObservableProperty] private string _barcodeSearch = "";
    [ObservableProperty] private ProductDto? _selectedProductForOrder;
    [ObservableProperty] private string _orderDetail = "";
    [ObservableProperty] private string _validationMessage = "";
    [ObservableProperty] private bool _showDetail;
    [ObservableProperty] private string _printStatus = "";

    public string[] StatusFilters { get; } = ["", "pending", "reviewed", "in_store", "with_driver", "on_the_way", "delivered", "postponed", "cancelled"];
    public string[] DiscountTypes { get; } = ["value", "percent"];

    public OrdersViewModel(IAlNedaApiClient apiClient, AlNeda.Admin.Services.PrintingService printingService, IDialogService dialog)
    {
        _apiClient = apiClient;
        _printingService = printingService;
        _dialog = dialog;
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            var orders = await _apiClient.GetOrdersAsync(string.IsNullOrEmpty(StatusFilter) ? null : StatusFilter);
            Orders = new ObservableCollection<OrderDto>(orders);
            var phs = await _apiClient.GetPharmaciesAsync();
            Pharmacies = new ObservableCollection<PharmacyDto>(phs);
            var prods = await _apiClient.GetProductsAsync();
            Products = new ObservableCollection<ProductDto>(prods);
            FilteredProducts = new ObservableCollection<ProductDto>(Products);
        }
        catch (ApiException ex)
        {
            _dialog.ShowError(ex.Message, "خطأ");
        }
        IsLoading = false;
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
                .Take(10)
                .ToList();
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

        CalculateTotal();
        BarcodeSearch = "";
        SelectedProductForOrder = null;
    }

    [RelayCommand]
    private void NewOrder()
    {
        NewItems = [];
        TotalAmount = 0;
        Discount = 0;
        DiscountType = "value";
        SelectedPharmacyId = 0;
        ShowEditor = true;
    }

    private void CalculateTotal()
    {
        TotalAmount = NewItems.Sum(i => i.Quantity * i.UnitPrice);
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        ValidationMessage = "";
        if (SelectedPharmacyId == 0) { ValidationMessage = "اختر الصيدلية"; return; }
        if (TotalAmount < 0) { ValidationMessage = "الإجمالي لا يمكن أن يكون سالباً"; return; }

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

        var finalTotal = DiscountType == "percent"
            ? TotalAmount - (TotalAmount * Discount / 100)
            : TotalAmount - Discount;

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
            var result = await _apiClient.CreateOrderAsync(request);
            if (result != null)
                _dialog.ShowMessage($"تم إنشاء الطلب #{result.OrderNumber} بنجاح", "نجاح");
        }
        catch (ApiException ex)
        {
            ValidationMessage = ex.Message;
            return;
        }
        ShowEditor = false; await LoadAsync();
    }

    [RelayCommand]
    private void CancelEdit() => ShowEditor = false;

    [RelayCommand]
    private async Task ViewDetailAsync(OrderDto? order)
    {
        if (order == null) return;
        var full = await _apiClient.GetOrderByIdAsync(order.Id);
        if (full != null) { SelectedOrder = new Order
        {
            Id = full.Id, OrderNumber = full.OrderNumber, PharmacyId = full.PharmacyId,
            TotalAmount = full.TotalAmount, Discount = full.Discount,
            DiscountType = full.DiscountType, FinalTotal = full.FinalTotal,
            AmountPaid = full.AmountPaid, BalanceBefore = full.BalanceBefore,
            BalanceAfter = full.BalanceAfter, Status = full.Status, CreatedAt = full.CreatedAt,
            Items = full.Items.Select(i => new OrderItem
            {
                Id = i.Id, ProductId = i.ProductId, Quantity = i.Quantity,
                UnitPrice = i.UnitPrice, TotalPrice = i.TotalPrice
            }).ToList()
        }; ShowDetail = true; }
    }

    [RelayCommand]
    private void CloseDetail() => ShowDetail = false;

    [RelayCommand]
    private async Task TransitionAsync(string? newStatus)
    {
        if (SelectedOrder == null || newStatus == null) return;
        if (newStatus == "cancelled" && !_dialog.Confirm("هل أنت متأكد من إلغاء هذا الطلب؟")) return;
        if (newStatus == "delivered" && !_dialog.Confirm("هل تم تسليم هذا الطلب فعلاً؟")) return;
        try
        {
            await _apiClient.UpdateOrderStatusAsync(SelectedOrder.Id, newStatus);
            _dialog.ShowMessage($"تم تغيير حالة الطلب إلى {newStatus}", "نجاح");
            await LoadAsync();
        }
        catch (ApiException ex)
        {
            _dialog.ShowError(ex.Message, "خطأ");
        }
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
    }

    [RelayCommand]
    private async Task PrintOrderAsync(Order? order)
    {
        if (order == null)
        {
            PrintStatus = "اختر طلباً أولاً";
            return;
        }

        try
        {
            var fullOrder = await _printingService.GetOrderWithDetailsAsync(order.Id);
            if (fullOrder != null)
            {
                if (_printingService.PrintOrder(fullOrder))
                {
                    PrintStatus = "تم إرسال الفاتورة إلى الطابعة";
                }
                else
                {
                    PrintStatus = "تم إلغاء الطباعة";
                }
            }
            else
            {
                PrintStatus = "خطأ في تحميل بيانات الطلب";
            }
        }
        catch (Exception ex)
        {
            PrintStatus = $"خطأ: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task PrintSelectedOrderAsync()
    {
        if (SelectedOrder == null)
        {
            PrintStatus = "اختر طلباً من القائمة أولاً";
            return;
        }

        await PrintOrderAsync(SelectedOrder);
    }

    [RelayCommand]
    private async Task ExportOrderHtmlAsync(Order? order)
    {
        if (order == null)
        {
            PrintStatus = "اختر طلباً أولاً";
            return;
        }

        try
        {
            var fullOrder = await _printingService.GetOrderWithDetailsAsync(order.Id);
            if (fullOrder != null)
            {
                var dialog = new Microsoft.Win32.SaveFileDialog
                {
                    Title = "تصدير فاتورة HTML",
                    Filter = "HTML Files (*.html)|*.html",
                    FileName = $"Invoice_{fullOrder.OrderNumber}.html"
                };

                if (dialog.ShowDialog() == true)
                {
                    var html = _printingService.ExportOrderToHtml(fullOrder);
                    System.IO.File.WriteAllText(dialog.FileName, html, System.Text.Encoding.UTF8);
                    PrintStatus = $"تم التصدير: {System.IO.Path.GetFileName(dialog.FileName)}";
                }
            }
        }
        catch (Exception ex)
        {
            PrintStatus = $"خطأ: {ex.Message}";
        }
    }
}
