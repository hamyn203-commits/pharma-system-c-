using System.Collections.ObjectModel;
using System.IO;
using AlNeda.Admin.Services;
using AlNeda.Core.Entities;
using AlNeda.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AlNeda.Admin.ViewModels;

public partial class OrdersViewModel : ObservableObject
{
    private readonly OrderService _orderService;
    private readonly PharmacyService _pharmacyService;
    private readonly ProductService _productService;
    private readonly IDialogService _dialog;

    [ObservableProperty] private ObservableCollection<Order> _orders = [];
    [ObservableProperty] private ObservableCollection<Pharmacy> _pharmacies = [];
    [ObservableProperty] private ObservableCollection<Product> _products = [];
    [ObservableProperty] private ObservableCollection<OrderItem> _newItems = [];
    [ObservableProperty] private Order? _selectedOrder;
    [ObservableProperty] private bool _showEditor;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private int _selectedPharmacyId;
    [ObservableProperty] private decimal _totalAmount;
    [ObservableProperty] private decimal _discount;
    [ObservableProperty] private string _discountType = "value";
    [ObservableProperty] private string _statusFilter = "";
    [ObservableProperty] private string _orderDetail = "";
    [ObservableProperty] private string _validationMessage = "";
    [ObservableProperty] private bool _showDetail;

    public string[] StatusFilters { get; } = ["", "pending", "reviewed", "in_store", "with_driver", "on_the_way", "delivered", "postponed", "cancelled"];
    public string[] DiscountTypes { get; } = ["value", "percent"];

    public OrdersViewModel(OrderService os, PharmacyService ps, ProductService prs, IDialogService dialog) { _orderService = os; _pharmacyService = ps; _productService = prs; _dialog = dialog; }

    [RelayCommand]
    private async Task LoadAsync()
    {
        IsLoading = true;
        Orders = new ObservableCollection<Order>(await _orderService.GetAllAsync(string.IsNullOrEmpty(StatusFilter) ? null : StatusFilter));
        Pharmacies = new ObservableCollection<Pharmacy>(await _pharmacyService.GetAllAsync());
        Products = new ObservableCollection<Product>(await _productService.GetAllAsync());
        IsLoading = false;
    }

    [RelayCommand]
    private void NewOrder()
    {
        NewItems = []; TotalAmount = 0; Discount = 0; DiscountType = "value"; SelectedPharmacyId = 0; ShowEditor = true;
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        ValidationMessage = "";
        if (SelectedPharmacyId == 0) { ValidationMessage = "اختر الصيدلية"; return; }
        if (TotalAmount < 0) { ValidationMessage = "الإجمالي لا يمكن أن يكون سالباً"; return; }
        var order = new Order { PharmacyId = SelectedPharmacyId, TotalAmount = TotalAmount, Discount = Discount, DiscountType = DiscountType };
        await _orderService.CreateAsync(order, [.. NewItems]);
        ShowEditor = false; await LoadAsync();
    }

    [RelayCommand] private void CancelEdit() => ShowEditor = false;

    [RelayCommand]
    private async Task ViewDetailAsync(Order? order)
    {
        if (order == null) return;
        var full = await _orderService.GetByIdAsync(order.Id);
        if (full != null) { SelectedOrder = full; ShowDetail = true; }
    }

    [RelayCommand]
    private void CloseDetail() => ShowDetail = false;

    [RelayCommand]
    private async Task TransitionAsync(string? newStatus)
    {
        if (SelectedOrder == null || newStatus == null) return;
        if (newStatus == "cancelled" && !_dialog.Confirm("هل أنت متأكد من إلغاء هذا الطلب؟")) return;
        if (newStatus == "delivered" && !_dialog.Confirm("هل تم تسليم هذا الطلب فعلاً؟")) return;
        await _orderService.TransitionStatusAsync(SelectedOrder.Id, newStatus);
        await LoadAsync();
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
}

public partial class PaymentsViewModel : ObservableObject
{
    private readonly PaymentService _paymentService;
    private readonly PharmacyService _pharmacyService;

    [ObservableProperty] private ObservableCollection<Payment> _payments = [];
    [ObservableProperty] private ObservableCollection<Pharmacy> _pharmacies = [];
    [ObservableProperty] private Payment? _selectedPayment;
    [ObservableProperty] private bool _showEditor;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private int _selectedPharmacyId;
    [ObservableProperty] private decimal _amount;
    [ObservableProperty] private string _paymentType = "cash";
    [ObservableProperty] private string _paymentNotes = "";
    [ObservableProperty] private string _validationMessage = "";

    public string[] PaymentTypes { get; } = ["cash", "partial", "full", "deferred", "collect_on_delivery"];

    public PaymentsViewModel(PaymentService ps, PharmacyService phs) { _paymentService = ps; _pharmacyService = phs; }

    [RelayCommand] private async Task LoadAsync() { IsLoading = true; Payments = new ObservableCollection<Payment>(await _paymentService.GetAllAsync()); Pharmacies = new ObservableCollection<Pharmacy>(await _pharmacyService.GetAllAsync()); IsLoading = false; }

    [RelayCommand] private void NewPayment() { Amount = 0; PaymentType = "cash"; PaymentNotes = ""; SelectedPharmacyId = 0; ShowEditor = true; }

    [RelayCommand]
    private async Task SaveAsync()
    {
        ValidationMessage = "";
        if (SelectedPharmacyId == 0) { ValidationMessage = "اختر الصيدلية"; return; }
        if (Amount <= 0) { ValidationMessage = "المبلغ يجب أن يكون أكبر من صفر"; return; }
        await _paymentService.AddAsync(new Payment { PharmacyId = SelectedPharmacyId, Amount = Amount, PaymentType = PaymentType, PaymentNotes = PaymentNotes });
        ShowEditor = false; await LoadAsync();
    }

    [RelayCommand] private void CancelEdit() => ShowEditor = false;
}

public partial class ReturnsViewModel : ObservableObject
{
    private readonly ReturnService _returnService;
    private readonly PharmacyService _pharmacyService;
    private readonly ProductService _productService;

    [ObservableProperty] private ObservableCollection<Return> _returns = [];
    [ObservableProperty] private ObservableCollection<Pharmacy> _pharmacies = [];
    [ObservableProperty] private ObservableCollection<Product> _products = [];
    [ObservableProperty] private ObservableCollection<ReturnItem> _newItems = [];
    [ObservableProperty] private Return? _selectedReturn;
    [ObservableProperty] private bool _showEditor;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private int _selectedPharmacyId;
    [ObservableProperty] private decimal _totalAmount;
    [ObservableProperty] private string _returnType = "expired";
    [ObservableProperty] private string _reason = "";
    [ObservableProperty] private bool _adjustStock = true;
    [ObservableProperty] private bool _adjustBalance = true;
    [ObservableProperty] private string _validationMessage = "";

    public string[] ReturnTypes { get; } = ["expired", "damaged", "wrong_item", "extra_quantity", "customer_return", "other"];

    public ReturnsViewModel(ReturnService rs, PharmacyService ps, ProductService prs) { _returnService = rs; _pharmacyService = ps; _productService = prs; }

    [RelayCommand] private async Task LoadAsync() { IsLoading = true; Returns = new ObservableCollection<Return>(await _returnService.GetAllAsync()); Pharmacies = new ObservableCollection<Pharmacy>(await _pharmacyService.GetAllAsync()); Products = new ObservableCollection<Product>(await _productService.GetAllAsync()); IsLoading = false; }

    [RelayCommand] private void NewReturn() { NewItems = []; TotalAmount = 0; ReturnType = "expired"; Reason = ""; AdjustStock = true; AdjustBalance = true; SelectedPharmacyId = 0; ShowEditor = true; }

    [RelayCommand]
    private async Task SaveAsync()
    {
        ValidationMessage = "";
        if (SelectedPharmacyId == 0) { ValidationMessage = "اختر الصيدلية"; return; }
        if (TotalAmount < 0) { ValidationMessage = "المبلغ لا يمكن أن يكون سالباً"; return; }
        var ret = new Return { PharmacyId = SelectedPharmacyId, TotalAmount = TotalAmount, ReturnType = ReturnType, Reason = Reason, StockAdjusted = AdjustStock, BalanceAdjusted = AdjustBalance };
        await _returnService.CreateAsync(ret, [.. NewItems]);
        ShowEditor = false; await LoadAsync();
    }

    [RelayCommand] private void CancelEdit() => ShowEditor = false;
}

public partial class AccountStatementViewModel : ObservableObject
{
    private readonly PharmacyService _pharmacyService;
    private readonly ReportService _reportService;

    [ObservableProperty] private ObservableCollection<Pharmacy> _pharmacies = [];
    [ObservableProperty] private Pharmacy? _selectedPharmacy;
    [ObservableProperty] private string _ledgerText = "اختر صيدلية واضغط 'عرض الكشف'";
    [ObservableProperty] private bool _isLoading;

    public AccountStatementViewModel(PharmacyService ps, ReportService rs)
    {
        _pharmacyService = ps;
        _reportService = rs;
    }

    [RelayCommand] private async Task LoadAsync()
    {
        IsLoading = true;
        Pharmacies = new ObservableCollection<Pharmacy>(await _pharmacyService.GetAllAsync());
        IsLoading = false;
    }

    [RelayCommand] private async Task ShowStatementAsync()
    {
        if (SelectedPharmacy == null) return;
        IsLoading = true;
        LedgerText = "جاري تحميل الكشف...";
        LedgerText = await _reportService.GetAccountStatementAsync(SelectedPharmacy.Id);
        IsLoading = false;
    }
}

public partial class ReportsViewModel : ObservableObject
{
    private readonly ReportService _reportService;

    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string _reportContent = "اختر تقريراً من القائمة";

    public ReportsViewModel(ReportService reportService) => _reportService = reportService;

    [RelayCommand] private async Task LoadAsync() { IsLoading = false; await Task.CompletedTask; }

    [RelayCommand]
    private async Task RunReportAsync(string? reportName)
    {
        if (string.IsNullOrEmpty(reportName)) return;
        IsLoading = true;
        ReportContent = "جاري تحميل التقرير...";

        try
        {
            ReportContent = reportName switch
            {
                "daily_sales" => await _reportService.GetDailySalesReportAsync(),
                "monthly_sales" => await _reportService.GetMonthlySalesReportAsync(),
                "top_products" => await _reportService.GetTopProductsReportAsync(),
                "top_pharmacies" => await _reportService.GetTopPharmaciesReportAsync(),
                "expiry" => await _reportService.GetExpiryReportAsync(),
                "debts" => await _reportService.GetDebtsReportAsync(),
                "stock" => await _reportService.GetStockReportAsync(),
                _ => $"تقرير غير معروف: {reportName}"
            };
        }
        catch (Exception ex)
        {
            ReportContent = $"خطأ في تحميل التقرير: {ex.Message}";
        }

        IsLoading = false;
    }
}

public partial class AuditLogViewModel : ObservableObject
{
    private readonly AuditService _auditService;

    [ObservableProperty] private ObservableCollection<AuditLog> _logs = [];
    [ObservableProperty] private bool _isLoading;

    public AuditLogViewModel(AuditService auditService) => _auditService = auditService;

    [RelayCommand] private async Task LoadAsync() { IsLoading = true; Logs = new ObservableCollection<AuditLog>(await _auditService.GetAllAsync()); IsLoading = false; }
}

public partial class BackupViewModel : ObservableObject
{
    private readonly BackupService _backupService;
    private readonly IDialogService _dialog;

    [ObservableProperty] private string _status = "جاهز";
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string[] _backups = [];

    public BackupViewModel(BackupService backupService, IDialogService dialog) { _backupService = backupService; _dialog = dialog; }

    [RelayCommand]
    private async Task CreateBackupAsync()
    {
        if (!_dialog.Confirm("هل تريد إنشاء نسخة احتياطية الآن؟")) return;
        IsLoading = true; Status = "جاري إنشاء النسخة الاحتياطية...";
        try
        {
            var path = await _backupService.BackupAsync();
            Status = "تم إنشاء النسخة الاحتياطية بنجاح";
            Backups = _backupService.ListBackups();
        }
        catch (Exception ex)
        {
            Status = $"خطأ: {ex.Message}";
        }
        IsLoading = false;
    }

    [RelayCommand]
    private async Task RestoreBackupAsync(string? backupPath)
    {
        if (string.IsNullOrEmpty(backupPath)) return;
        if (!_dialog.Confirm("تحذير: سيتم استبدال قاعدة البيانات الحالية بالنسخة المحددة!\nهل تريد المتابعة؟")) return;

        IsLoading = true; Status = "جاري استرجاع النسخة الاحتياطية...";
        try
        {
            var success = await _backupService.RestoreAsync(backupPath);
            Status = success ? "تم استرجاع النسخة الاحتياطية بنجاح" : "فشل في استرجاع النسخة";
        }
        catch (Exception ex)
        {
            Status = $"خطأ: {ex.Message}";
        }
        IsLoading = false;
    }

    [RelayCommand]
    private void DeleteBackup(string? backupPath)
    {
        if (string.IsNullOrEmpty(backupPath)) return;
        if (!_dialog.Confirm("هل تريد حذف هذه النسخة الاحتياطية؟")) return;

        var success = _backupService.DeleteBackup(backupPath);
        Backups = _backupService.ListBackups();
        Status = success ? "تم حذف النسخة الاحتياطية" : "فشل في حذف النسخة";
    }

    [RelayCommand] private Task LoadAsync() { Backups = _backupService.ListBackups(); Status = $"يوجد {Backups.Length} نسخة احتياطية"; return Task.CompletedTask; }
}

public partial class SettingsViewModel : ObservableObject
{
    [ObservableProperty] private string _dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "pharmacy.db");
    [ObservableProperty] private string _logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
}
