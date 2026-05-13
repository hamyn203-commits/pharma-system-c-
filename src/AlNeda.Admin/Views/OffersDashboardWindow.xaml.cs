using System.Windows;
using AlNeda.Admin.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AlNeda.Admin.Views;

public partial class OffersDashboardWindow : Window
{
    public OffersDashboardWindow(OffersDashboardViewModel dashboard, OffersViewModel editor)
    {
        InitializeComponent();
        var vm = new OffersDashboardWindowViewModel(dashboard, editor);
        DataContext = vm;
        Loaded += async (_, _) => await vm.RefreshCurrentAsync();
    }

    private void Minimize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;

    private void Maximize_Click(object sender, RoutedEventArgs e) =>
        WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;

    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}

public partial class OffersDashboardWindowViewModel : ObservableObject
{
    public OffersDashboardWindowViewModel(OffersDashboardViewModel dashboard, OffersViewModel editor)
    {
        Dashboard = dashboard;
        Editor = editor;
    }

    public OffersDashboardViewModel Dashboard { get; }
    public OffersViewModel Editor { get; }

    [ObservableProperty] private bool _isSidebarOpen = true;
    [ObservableProperty] private string _selectedSection = "dashboard";

    public GridLength SidebarWidth => IsSidebarOpen ? new GridLength(250) : new GridLength(64);

    public bool IsDashboardVisible => SelectedSection == "dashboard";
    public bool IsAllOffersVisible => SelectedSection == "all";
    public bool IsPublishedVisible => SelectedSection == "published";
    public bool IsReviewVisible => SelectedSection == "review";
    public bool IsDraftsVisible => SelectedSection == "drafts";
    public bool IsEditorVisible => SelectedSection == "editor";
    public bool IsProductsVisible => SelectedSection == "products";
    public bool IsTargetingVisible => SelectedSection == "targeting";
    public bool IsPreviewVisible => SelectedSection == "preview";
    public bool IsAnalyticsVisible => SelectedSection == "analytics";
    public bool IsEngagementVisible => SelectedSection == "engagement";
    public bool IsImageSettingsVisible => SelectedSection == "images";

    public string PageTitle => SelectedSection switch
    {
        "all" => "كل العروض",
        "published" => "العروض المنشورة",
        "review" => "قيد المراجعة",
        "drafts" => "المسودات",
        "editor" => "محرر العرض",
        "products" => "المنتجات داخل العرض",
        "targeting" => "الاستهداف",
        "preview" => "معاينة تطبيق الصيدلي",
        "analytics" => "التحليلات",
        "engagement" => "تفاعل الصيدليات",
        "images" => "إعدادات الصور",
        _ => "لوحة العروض"
    };

    public string PageSubtitle => SelectedSection switch
    {
        "all" => "بحث وفلترة وإدارة كل العروض في مساحة واسعة.",
        "published" => "متابعة العروض المنشورة وإيقافها عند الحاجة.",
        "review" => "مراجعة العروض قبل النشر وتسجيل أسباب التحذير.",
        "drafts" => "مسودات غير منشورة يمكن استكمال تحريرها.",
        "editor" => "صفحة كاملة لبيانات العرض والصورة والوصف وأزرار الحفظ والنشر.",
        "products" => "إدارة المنتجات والكميات والأسعار والخصومات داخل العرض.",
        "targeting" => "تحديد الجمهور والصيدليات التي سترى العرض.",
        "preview" => "مراجعة شكل العرض كما يظهر على تطبيق الصيدلي.",
        "analytics" => "تحليل المشاهدات والضغطات والطلبات ومعدل التحويل.",
        "engagement" => "جدول تفاعل الصيدليات مع كل عرض.",
        "images" => "إرشادات ومقاسات الصور لفريق الجرافيك.",
        _ => "ملخص أداء العروض ومؤشرات المشاهدات والضغطات والتحويل."
    };

    [RelayCommand]
    private void ToggleSidebar()
    {
        IsSidebarOpen = !IsSidebarOpen;
        OnPropertyChanged(nameof(SidebarWidth));
    }

    [RelayCommand]
    private async Task ShowSectionAsync(string section)
    {
        SelectedSection = string.IsNullOrWhiteSpace(section) ? "dashboard" : section;
        await RefreshCurrentAsync();
    }

    [RelayCommand]
    private async Task CreateNewOfferAsync()
    {
        SelectedSection = "editor";
        Editor.NewOfferCommand.Execute(null);
        await Editor.LoadAsync();
    }

    [RelayCommand]
    public async Task RefreshCurrentAsync()
    {
        if (IsDashboardVisible || IsAnalyticsVisible)
            await Dashboard.LoadAsync();
        else
            await Editor.LoadAsync();
    }

    partial void OnSelectedSectionChanged(string value)
    {
        OnPropertyChanged(nameof(IsDashboardVisible));
        OnPropertyChanged(nameof(IsAllOffersVisible));
        OnPropertyChanged(nameof(IsPublishedVisible));
        OnPropertyChanged(nameof(IsReviewVisible));
        OnPropertyChanged(nameof(IsDraftsVisible));
        OnPropertyChanged(nameof(IsEditorVisible));
        OnPropertyChanged(nameof(IsProductsVisible));
        OnPropertyChanged(nameof(IsTargetingVisible));
        OnPropertyChanged(nameof(IsPreviewVisible));
        OnPropertyChanged(nameof(IsAnalyticsVisible));
        OnPropertyChanged(nameof(IsEngagementVisible));
        OnPropertyChanged(nameof(IsImageSettingsVisible));
        OnPropertyChanged(nameof(PageTitle));
        OnPropertyChanged(nameof(PageSubtitle));
    }
}
