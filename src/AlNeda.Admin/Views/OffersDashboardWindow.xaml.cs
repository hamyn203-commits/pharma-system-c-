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

    public GridLength SidebarWidth => IsSidebarOpen ? new GridLength(248) : new GridLength(64);

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
        "editor" => "بيانات العرض",
        "products" => "منتجات العرض",
        "targeting" => "استهداف الصيدليات",
        "preview" => "معاينة تطبيق الصيدلي",
        "analytics" => "تحليلات العروض",
        "engagement" => "تفاعل الصيدليات",
        "images" => "إعدادات الصور",
        _ => "لوحة العروض"
    };

    public string PageSubtitle => SelectedSection switch
    {
        "all" => "قائمة تشغيل كاملة للبحث، الفلترة، وفتح أي عرض للتعديل.",
        "published" => "العروض الظاهرة حاليًا للصيدليات مع مؤشرات الأداء الأساسية.",
        "review" => "العروض التي تحتاج مراجعة قبل النشر على تطبيق الصيدلي.",
        "drafts" => "مسودات غير منشورة يمكن استكمالها وتجهيزها للنشر.",
        "editor" => "بيانات العرض الأساسية، الأسعار، المدة، والحفظ أو النشر.",
        "products" => "إدارة المنتجات والكميات والأسعار المرتبطة بالعرض.",
        "targeting" => "حدد هل العرض لكل الصيدليات أم لفئة أو صيدليات بعينها.",
        "preview" => "راجع شكل العرض كما سيظهر داخل تطبيق الصيدلي قبل النشر.",
        "analytics" => "مشاهدات، ضغطات، طلبات، وتحويلات كل عرض.",
        "engagement" => "متابعة الصيدليات التي شاهدت أو ضغطت أو طلبت من العرض.",
        "images" => "مقاسات وإرشادات الصور لفريق الجرافيك.",
        _ => "ملخص سريع لأداء العروض والمهام التي تحتاج قرار."
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
        if (IsDashboardVisible || IsAnalyticsVisible || IsPublishedVisible || IsReviewVisible || IsDraftsVisible)
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
