using System.Collections.ObjectModel;
using AlNeda.Admin.ViewModels;
using AlNeda.Admin.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using AlNeda.Admin.Services;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;

namespace AlNeda.Admin.ViewModels;

public partial class MainViewModel : ObservableObject, INavigationService
{
    private readonly IServiceProvider _services;

    [ObservableProperty] private string _currentUserName = string.Empty;
    [ObservableProperty] private string _currentUserRole = string.Empty;
    [ObservableProperty] private string _currentPageTitle = "لوحة التحكم الرئيسية";
    [ObservableProperty] private string _statusMessage = "جاهز";
    [ObservableProperty] private object? _currentPage;
    [ObservableProperty] private string _activeNavTag = "dashboard";

    public ObservableCollection<NavItem> NavItems { get; } =
    [
        new("عام", null, true),
        new("الرئيسية", "dashboard", false),
        new("المخزون", null, true),
        new("المنتجات", "products", false),
        new("التصنيفات", "categories", false),
        new("المبيعات والحسابات", null, true),
        new("الصيدليات", "pharmacies", false),
        new("الطلبات", "orders", false),
        new("التحصيلات", "payments", false),
        new("المرتجعات", "returns", false),
        new("كشف الحساب", "statements", false),
        new("المتابعة", null, true),
        new("التقارير", "reports", false),
        new("سجل العمليات", "audit", false),
        new("النسخ الاحتياطي", "backup", false),
        new("الإعدادات", "settings", false),
    ];

    public string CurrentRoleLabel => _roleLabels.GetValueOrDefault(CurrentUserRole, CurrentUserRole);

    private static readonly Dictionary<string, string> _roleLabels = new()
    {
        ["admin"] = "مدير النظام",
        ["accountant"] = "محاسب",
        ["rep"] = "مندوب"
    };

    private static readonly Dictionary<string, string> _pageTitles = new()
    {
        ["dashboard"] = "لوحة التحكم الرئيسية",
        ["products"] = "إدارة المنتجات",
        ["categories"] = "إدارة التصنيفات",
        ["suppliers"] = "إدارة الموردين",
        ["purchases"] = "إدارة المشتريات",
        ["pharmacies"] = "إدارة الصيدليات",
        ["orders"] = "إدارة الطلبات",
        ["payments"] = "إدارة التحصيلات",
        ["returns"] = "المرتجعات",
        ["statements"] = "كشف الحساب",
        ["reports"] = "التقارير",
        ["audit"] = "سجل العمليات",
        ["backup"] = "النسخ الاحتياطي",
        ["settings"] = "الإعدادات",
    };

    public MainViewModel(IServiceProvider services)
    {
        _services = services;
    }

    public event EventHandler? LogoutRequested;

    public void Initialize(string username, string role)
    {
        CurrentUserName = username;
        CurrentUserRole = role;
        OnPropertyChanged(nameof(CurrentRoleLabel));

        var defaultPage = role switch
        {
            "rep" => "orders",
            "accountant" => "payments",
            _ => "dashboard"
        };
        NavigateTo(defaultPage);
    }

    [RelayCommand]
    private void Navigate(string tag)
    {
        ActiveNavTag = tag;
        CurrentPageTitle = _pageTitles.GetValueOrDefault(tag, tag);
        NavigateTo(tag);
    }

    public void NavigateTo(string tag)
    {
        var normalizedTag = tag.ToLowerInvariant();
        CurrentPage = normalizedTag switch
        {
            "dashboard" => CreateWithViewModel<DashboardView, DashboardViewModel>(vm => vm.LoadCommand.Execute(null)),
            "products" => CreateWithViewModel<ProductsView, ProductsViewModel>(vm => vm.LoadCommand.Execute(null)),
            "categories" => CreateWithViewModel<CategoriesView, CategoryViewModel>(vm => vm.LoadCommand.Execute(null)),
            "suppliers" => CreateWithViewModel<SuppliersView, SuppliersViewModel>(vm => vm.LoadCommand.Execute(null)),
            "purchases" => CreateWithViewModel<PurchasesView, PurchasesViewModel>(vm => vm.LoadCommand.Execute(null)),
            "pharmacies" => CreateWithViewModel<PharmaciesView, PharmaciesViewModel>(vm => vm.LoadCommand.Execute(null)),
            "orders" => CreateWithViewModel<OrdersView, OrdersViewModel>(vm => vm.LoadCommand.Execute(null)),
            "payments" => CreateWithViewModel<PaymentsView, PaymentsViewModel>(vm => vm.LoadCommand.Execute(null)),
            "returns" => CreateWithViewModel<ReturnsView, ReturnsViewModel>(vm => vm.LoadCommand.Execute(null)),
            "statements" => CreateWithViewModel<AccountStatementView, AccountStatementViewModel>(vm => vm.LoadCommand.Execute(null)),
            "reports" => new ReportsView { DataContext = _services.GetRequiredService<ReportsViewModel>() },
            "audit" => CreateWithViewModel<AuditLogView, AuditLogViewModel>(vm => vm.LoadCommand.Execute(null)),
            "backup" => CreateWithViewModel<BackupView, BackupViewModel>(vm => vm.LoadCommand.Execute(null)),
            "settings" => new SettingsView { DataContext = _services.GetRequiredService<SettingsViewModel>() },
            _ => new TextBlockPlaceholder(CurrentPageTitle)
        };
    }

    private T CreateWithViewModel<T, VM>(Action<VM> init) where T : System.Windows.Controls.UserControl, new() where VM : class
    {
        var vm = _services.GetRequiredService<VM>();
        var view = new T { DataContext = vm };
        init(vm);
        return view;
    }

    [RelayCommand]
    private void Logout()
    {
        LogoutRequested?.Invoke(this, EventArgs.Empty);
    }
}

public record NavItem(string Label, string? Tag, bool IsHeader);
