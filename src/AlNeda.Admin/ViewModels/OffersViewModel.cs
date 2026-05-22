using System.Collections.ObjectModel;
using AlNeda.Admin.Services.ApiClient;
using AlNeda.Core.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using System.IO;
using System.Text;

namespace AlNeda.Admin.ViewModels;

public partial class OffersViewModel : ObservableObject
{
    private readonly IAlNedaApiClient _api;
    private int? _editingOfferId;
    private string _savedImageUrl = string.Empty;

    [ObservableProperty] private ObservableCollection<OfferDesignItem> _offers = [];
    [ObservableProperty] private ObservableCollection<OfferDesignItem> _filteredOffers = [];
    [ObservableProperty] private ObservableCollection<MetricCard> _statCards = [];
    [ObservableProperty] private ObservableCollection<MetricCard> _analyticsCards = [];
    [ObservableProperty] private ObservableCollection<EditorStepItem> _editorSteps = [];
    [ObservableProperty] private ObservableCollection<string> _offerTypes = [];
    [ObservableProperty] private ObservableCollection<string> _offerStates = [];
    [ObservableProperty] private ObservableCollection<string> _statusFilters = [];
    [ObservableProperty] private ObservableCollection<string> _dateFilters = [];
    [ObservableProperty] private ObservableCollection<string> _previewModes = [];
    [ObservableProperty] private ObservableCollection<string> _productNames = [];
    [ObservableProperty] private ObservableCollection<ProductPickerItem> _productCatalog = [];
    [ObservableProperty] private ObservableCollection<OfferProductItem> _offerProducts = [];
    [ObservableProperty] private ObservableCollection<OfferImageItem> _offerImages = [];
    [ObservableProperty] private ObservableCollection<TargetRuleItem> _targetRules = [];
    [ObservableProperty] private ObservableCollection<PharmacyTargetItem> _targetPharmacies = [];
    [ObservableProperty] private ObservableCollection<ReadinessCheckItem> _readinessChecks = [];
    [ObservableProperty] private ObservableCollection<PharmacyInteractionItem> _pharmacyInteractions = [];
    [ObservableProperty] private ObservableCollection<OfferCalendarItem> _offerCalendarItems = [];
    [ObservableProperty] private ObservableCollection<ImageAssetItem> _imageAssetItems = [];
    [ObservableProperty] private ObservableCollection<VisibilityAuditItem> _visibilityAuditItems = [];
    [ObservableProperty] private ObservableCollection<PublishChecklistItem> _publishChecklistItems = [];
    [ObservableProperty] private OfferDesignItem? _selectedOffer;
    [ObservableProperty] private PharmacyTargetItem? _selectedPreviewPharmacy;
    [ObservableProperty] private ProductPickerItem? _selectedCatalogProduct;

    [ObservableProperty] private string _offerSearch = string.Empty;
    [ObservableProperty] private string _statusFilter = "الكل";
    [ObservableProperty] private string _dateFilter = "هذا الشهر";
    [ObservableProperty] private string _offerTitle = "عرض جديد للصيدليات";
    [ObservableProperty] private string _offerSubtitle = "خصم خاص لفترة محدودة على تطبيق الصيدلي";
    [ObservableProperty] private string _offerType = "خصم مباشر";
    [ObservableProperty] private string _offerState = "مسودة";
    [ObservableProperty] private string _previewMode = "كارت الصفحة الرئيسية";
    [ObservableProperty] private string _offerDescription = "اكتب وصف العرض وفائدته للصيدلية بشكل واضح ومختصر.";
    [ObservableProperty] private string _offerTerms = "يسري العرض حتى نفاد الكمية ولا يمكن جمعه مع عروض أخرى.";
    [ObservableProperty] private string _internalNotes = "ملاحظات داخلية لفريق المبيعات لا تظهر للصيدلي.";
    [ObservableProperty] private string _offerImagePath = "";
    [ObservableProperty] private decimal _oldPrice;
    [ObservableProperty] private decimal _newPrice;
    [ObservableProperty] private int _discountPercent;
    [ObservableProperty] private int _remainingQuantity;
    [ObservableProperty] private DateTime _startsAt = DateTime.Today;
    [ObservableProperty] private DateTime _endsAt = DateTime.Today.AddDays(7);
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private string _statusMessage = "جاهز لإنشاء عرض جديد ومعاينته قبل النشر";

    public string PreviewDiscountLabel => DiscountPercent > 0 ? $"-{DiscountPercent}%" : "عرض خاص";
    public string PreviewPriceLabel => NewPrice > 0 ? $"{NewPrice:N0} ج.م" : "سعر خاص";
    public string PreviewOldPriceLabel => OldPrice > 0 ? $"{OldPrice:N0} ج.م" : "";
    public string PreviewExpiryLabel => EndsAt.Date <= DateTime.Today
        ? "ينتهي اليوم"
        : $"ينتهي خلال {(EndsAt.Date - DateTime.Today).Days} يوم";
    public string PreviewImagePath => !string.IsNullOrWhiteSpace(OfferImagePath) && OfferImagePath.StartsWith("/")
        ? $"http://localhost:5000{OfferImagePath}"
        : OfferImagePath;
    public string ImageShortLabel => string.IsNullOrWhiteSpace(OfferImagePath)
        ? "لم يتم رفع صورة بعد"
        : OfferImagePath.Length <= 70 ? OfferImagePath : $"...{OfferImagePath[^67..]}";
    public string ProductPreviewSummary => OfferProducts.Count == 0
        ? "لا توجد منتجات داخل العرض بعد"
        : string.Join("، ", OfferProducts.Take(3).Select(p => p.ProductName));
    public string ProductCatalogSummary => ProductCatalog.Count == 0
        ? "لم يتم تحميل منتجات من المخزون بعد"
        : $"{ProductCatalog.Count:N0} منتج متاح للاختيار من المخزون الرئيسي";
    public string ImagesSummaryLabel => OfferImages.Count == 0
        ? "لا توجد صور مضافة بعد"
        : $"{OfferImages.Count:N0} صورة - الرئيسية: {OfferImages.FirstOrDefault(x => x.IsPrimary)?.DisplayName ?? OfferImages.First().DisplayName}";

    public string TargetedPharmaciesLabel
    {
        get
        {
            var selected = TargetRules.FirstOrDefault(x => x.IsSelected);
            var count = selected?.Key switch
            {
                "all" => TargetPharmacies.Count,
                "debt" => TargetPharmacies.Count(x => x.Balance > 0),
                "vip" => Math.Max(1, TargetPharmacies.Count / 5),
                "inactive" => Math.Max(1, TargetPharmacies.Count / 4),
                "manual" => TargetPharmacies.Count(x => x.IsSelected),
                _ => Math.Max(1, TargetPharmacies.Count / 3)
            };
            return $"الصيدليات المستهدفة قبل النشر: {count:N0}";
        }
    }
    public string AudienceRule => TargetRules.FirstOrDefault(x => x.IsSelected)?.Key == "manual" ? "selected_pharmacies" : "all";
    public bool IsSpecificAudience => AudienceRule == "selected_pharmacies";
    public string PreviewPharmacyName => SelectedPreviewPharmacy?.Name ?? TargetPharmacies.FirstOrDefault()?.Name ?? "صيدلية تجريبية";
    public string PreviewVisibilityResult
    {
        get
        {
            var pharmacy = SelectedPreviewPharmacy ?? TargetPharmacies.FirstOrDefault();
            if (pharmacy == null)
                return "أضف صيدليات أولًا حتى تراجع ظهور العرض على تطبيق الصيدلي.";
            if (ReadinessChecks.Any(x => !x.IsOk))
                return $"لن يظهر العرض لـ {pharmacy.Name} قبل معالجة تحذيرات الجاهزية.";
            if (IsSpecificAudience && !pharmacy.IsSelected)
                return $"لن يظهر العرض لـ {pharmacy.Name} لأنها خارج قائمة الاستهداف اليدوي.";
            if (EndsAt <= DateTime.Today)
                return $"لن يظهر العرض لـ {pharmacy.Name} لأن تاريخ الانتهاء غير صالح.";
            return $"العرض جاهز للظهور لـ {pharmacy.Name} على تطبيق الصيدلي.";
        }
    }

    public OffersViewModel(IAlNedaApiClient api)
    {
        _api = api;
        OfferTypes = [
            "خصم مباشر",
            "باكدج منتجات",
            "اشتر كمية وخد هدية",
            "منتج جديد",
            "تصفية مخزون",
            "عرض موجه"
        ];
        OfferStates = ["مسودة", "قيد المراجعة", "منشور", "موقوف", "منتهي"];
        StatusFilters = ["الكل", "مسودة", "قيد المراجعة", "منشور", "موقوف", "منتهي"];
        DateFilters = ["اليوم", "هذا الأسبوع", "هذا الشهر"];
        PreviewModes = ["كارت الصفحة الرئيسية", "صفحة تفاصيل العرض", "شكل العرض داخل السلة"];
        TargetRules = [
            new("all", "كل الصيدليات", true),
            new("governorate", "صيدليات محافظة معينة"),
            new("inactive", "لم تطلب منذ 30 يوم"),
            new("debt", "عليها مديونية"),
            new("vip", "صيدليات VIP"),
            new("manual", "صيدليات محددة يدوياً")
        ];
        foreach (var rule in TargetRules)
        {
            rule.SelectionChanged += (_, _) => RefreshAll();
        }
        BuildEditorSteps();
        BuildDemoRows();
        RefreshAll();
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        try
        {
            var offers = await _api.GetOffersAsync();
            Offers = new ObservableCollection<OfferDesignItem>(offers.Select(OfferDesignItem.FromDto));

            var products = await _api.GetProductsAsync();
            ProductCatalog = new ObservableCollection<ProductPickerItem>(
                products.Select(ProductPickerItem.FromDto).OrderBy(p => p.Name));
            SelectedCatalogProduct ??= ProductCatalog.FirstOrDefault();
            ProductNames = new ObservableCollection<string>(products.Select(p => p.Name).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct());
            if (ProductNames.Count == 0)
                ProductNames = ["باراسيتامول 500 مجم", "أموكسيسيلين 500 مجم", "فيتامين د نقط"];

            var pharmacies = await _api.GetPharmaciesAsync();
            TargetPharmacies = new ObservableCollection<PharmacyTargetItem>(
                pharmacies.Select(p => new PharmacyTargetItem(p.Id, p.Name, p.Phone ?? "", p.Balance)));
            SelectedPreviewPharmacy ??= TargetPharmacies.FirstOrDefault();

            if (Offers.Count == 0)
                SeedDemoOffers();

            if (SelectedOffer == null && Offers.Count > 0)
                SelectOffer(Offers[0]);

            StatusMessage = "تم تحميل العروض والمنتجات والصيدليات";
        }
        catch (Exception ex)
        {
            if (Offers.Count == 0)
                SeedDemoOffers();
            if (ProductNames.Count == 0)
                ProductNames = ["باراسيتامول 500 مجم", "أموكسيسيلين 500 مجم", "فيتامين د نقط", "سيتريزين أقراص"];
            if (ProductCatalog.Count == 0)
                ProductCatalog = new ObservableCollection<ProductPickerItem>(
                    ProductNames.Select((name, index) => new ProductPickerItem(0, name, 0, 0, "بيانات تجريبية")));
            SelectedCatalogProduct ??= ProductCatalog.FirstOrDefault();
            if (TargetPharmacies.Count == 0)
                TargetPharmacies = [
                    new(1, "صيدلية النور", "01000000001", 1200),
                    new(2, "صيدلية الشفاء", "01000000002", 0),
                    new(3, "صيدلية الرحمة", "01000000003", 450)
                ];
            if (SelectedOffer == null && Offers.Count > 0)
                SelectOffer(Offers[0]);
            SelectedPreviewPharmacy ??= TargetPharmacies.FirstOrDefault();
            StatusMessage = $"تعذر الاتصال بالـ API، تم عرض بيانات تجريبية للواجهة: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
            RefreshAll();
        }
    }

    [RelayCommand]
    private void SelectOffer(OfferDesignItem? offer)
    {
        if (offer == null) return;

        foreach (var item in Offers)
            item.IsSelected = false;
        offer.IsSelected = true;
        SelectedOffer = offer;
        _editingOfferId = offer.Id > 0 ? offer.Id : null;

        if (offer.Source != null)
        {
            var dto = offer.Source;
            _savedImageUrl = dto.ImageUrl;
            OfferTitle = dto.Title;
            OfferSubtitle = dto.Subtitle;
            OfferDescription = dto.Description;
            OfferType = ToDisplayOfferType(dto.OfferType);
            OfferState = ToDisplayStatus(dto.Status);
            OfferImagePath = dto.ImageUrl;
            OfferImages = new ObservableCollection<OfferImageItem>(
                (dto.ImageUrls.Count > 0 ? dto.ImageUrls : string.IsNullOrWhiteSpace(dto.ImageUrl) ? [] : [dto.ImageUrl])
                .Select((url, index) => new OfferImageItem(url, index == 0)));
            OldPrice = dto.OldPrice ?? 0;
            NewPrice = dto.NewPrice ?? 0;
            DiscountPercent = dto.DiscountPercent ?? 0;
            RemainingQuantity = dto.RemainingQuantity ?? 0;
            StartsAt = dto.StartsAt;
            EndsAt = dto.EndsAt;
            foreach (var pharmacy in TargetPharmacies)
                pharmacy.IsSelected = dto.TargetPharmacyIds.Contains(pharmacy.Id);
            OfferProducts = new ObservableCollection<OfferProductItem>(dto.OfferProducts.Select(OfferProductItem.FromDto));
            TrackProductRows();
        }
        else
        {
            OfferTitle = offer.Title;
            OfferType = offer.TypeLabel;
            OfferState = offer.Status;
            OldPrice = 420;
            NewPrice = 355;
            DiscountPercent = 15;
            RemainingQuantity = 80;
            BuildDemoRows();
        }

        StatusMessage = $"تم فتح العرض: {offer.Title}";
        RefreshAll();
    }

    [RelayCommand]
    private void NewOffer()
    {
        _editingOfferId = null;
        _savedImageUrl = string.Empty;
        foreach (var item in Offers)
            item.IsSelected = false;
        SelectedOffer = null;
        OfferTitle = "عرض جديد للصيدليات";
        OfferSubtitle = "خصم خاص لفترة محدودة على تطبيق الصيدلي";
        OfferType = OfferTypes.First();
        OfferState = "مسودة";
        PreviewMode = PreviewModes.First();
        OfferDescription = "اكتب وصف العرض وفائدته للصيدلية بشكل واضح ومختصر.";
        OfferTerms = "يسري العرض حتى نفاد الكمية ولا يمكن جمعه مع عروض أخرى.";
        InternalNotes = "ملاحظات داخلية لفريق المبيعات لا تظهر للصيدلي.";
        OfferImagePath = "";
        OfferImages.Clear();
        OldPrice = 0;
        NewPrice = 0;
        DiscountPercent = 0;
        RemainingQuantity = 0;
        StartsAt = DateTime.Today;
        EndsAt = DateTime.Today.AddDays(7);
        OfferProducts.Clear();
        if (SelectedCatalogProduct != null)
            AddOfferProductFromCatalog(SelectedCatalogProduct);
        foreach (var target in TargetRules)
            target.IsSelected = target.Key == "all";
        StatusMessage = "تم تجهيز عرض جديد. أكمل المنتجات والصورة والاستهداف قبل النشر.";
        RefreshAll();
    }

    [RelayCommand]
    private void AddOfferProduct()
    {
        if (SelectedCatalogProduct != null)
        {
            AddOfferProductFromCatalog(SelectedCatalogProduct);
            return;
        }

        var product = new OfferProductItem
        {
            ProductId = 0,
            ProductName = ProductNames.FirstOrDefault() ?? "منتج جديد",
            AvailableQuantity = 120,
            OfferQuantity = 20,
            OldPrice = OldPrice > 0 ? OldPrice : 100,
            NewPrice = NewPrice > 0 ? NewPrice : 85,
            MinimumOrder = 1
        };
        TrackProductRow(product);
        OfferProducts.Add(product);
        RefreshAll();
    }

    [RelayCommand]
    private void AddSelectedProductToOffer()
    {
        if (SelectedCatalogProduct == null)
        {
            StatusMessage = "اختار منتج من مخزون البرنامج الأول.";
            return;
        }

        AddOfferProductFromCatalog(SelectedCatalogProduct);
    }

    private void AddOfferProductFromCatalog(ProductPickerItem selectedProduct)
    {
        var existing = OfferProducts.FirstOrDefault(p => p.ProductId == selectedProduct.Id && selectedProduct.Id > 0);
        if (existing != null)
        {
            var nextQuantity = Math.Max(existing.OfferQuantity + 1, 1);
            existing.OfferQuantity = existing.AvailableQuantity > 0
                ? Math.Min(nextQuantity, existing.AvailableQuantity)
                : nextQuantity;
            StatusMessage = $"تم تحديث كمية {existing.ProductName} داخل العرض.";
            RefreshAll();
            return;
        }

        var price = selectedProduct.UnitPrice > 0 ? selectedProduct.UnitPrice : 100;
        var product = new OfferProductItem
        {
            ProductId = selectedProduct.Id,
            ProductName = selectedProduct.Name,
            AvailableQuantity = selectedProduct.Quantity,
            OfferQuantity = Math.Max(1, Math.Min(20, selectedProduct.Quantity > 0 ? selectedProduct.Quantity : 20)),
            OldPrice = price,
            NewPrice = Math.Round(price * 0.90m, 2),
            MinimumOrder = 1
        };

        TrackProductRow(product);
        OfferProducts.Add(product);
        StatusMessage = $"تم ربط {selectedProduct.Name} بالعرض من ملف المنتجات.";
        RefreshAll();
    }

    [RelayCommand]
    private void RemoveOfferProduct(OfferProductItem? product)
    {
        if (product == null) return;
        OfferProducts.Remove(product);
        RefreshAll();
    }

    [RelayCommand]
    private void ChooseImage()
    {
        var dialog = new OpenFileDialog
        {
            Title = "اختيار تصميم العرض",
            Filter = "Images|*.png;*.jpg;*.jpeg;*.webp|All files|*.*"
        };

        if (dialog.ShowDialog() == true)
        {
            OfferImagePath = dialog.FileName;
            OfferImages.Clear();
            OfferImages.Add(new OfferImageItem(dialog.FileName, true));
            StatusMessage = "تم تحميل الصورة للمعاينة. سيتم رفعها للخادم عند الحفظ.";
        }
    }

    [RelayCommand]
    private void AddOfferImages()
    {
        var dialog = new OpenFileDialog
        {
            Title = "اختيار صور العرض",
            Filter = "Images|*.png;*.jpg;*.jpeg;*.webp|All files|*.*",
            Multiselect = true
        };

        if (dialog.ShowDialog() != true)
            return;

        foreach (var file in dialog.FileNames)
        {
            if (OfferImages.Any(x => string.Equals(x.Path, file, StringComparison.OrdinalIgnoreCase)))
                continue;

            OfferImages.Add(new OfferImageItem(file, OfferImages.Count == 0));
        }

        var primary = OfferImages.FirstOrDefault(x => x.IsPrimary) ?? OfferImages.FirstOrDefault();
        if (primary != null)
        {
            primary.IsPrimary = true;
            OfferImagePath = primary.Path;
        }

        StatusMessage = $"تمت إضافة {dialog.FileNames.Length:N0} صورة للعرض. سيتم رفعها عند الإنشاء أو النشر.";
        RefreshAll();
    }

    [RelayCommand]
    private void RemoveOfferImage(OfferImageItem? image)
    {
        if (image == null)
            return;

        var wasPrimary = image.IsPrimary;
        OfferImages.Remove(image);
        if (OfferImages.Count > 0 && wasPrimary)
            OfferImages[0].IsPrimary = true;

        OfferImagePath = OfferImages.FirstOrDefault(x => x.IsPrimary)?.Path ?? OfferImages.FirstOrDefault()?.Path ?? string.Empty;
        RefreshAll();
    }

    [RelayCommand]
    private void SetPrimaryImage(OfferImageItem? image)
    {
        if (image == null)
            return;

        foreach (var item in OfferImages)
            item.IsPrimary = false;
        image.IsPrimary = true;
        OfferImagePath = image.Path;
        RefreshAll();
    }

    [RelayCommand]
    private async Task SaveDraftAsync() => await SaveAsync("draft");

    [RelayCommand]
    private async Task SubmitReviewAsync()
    {
        OfferState = "قيد المراجعة";
        await SaveAsync("review");
    }

    [RelayCommand]
    private async Task CreateAndPublishAsync() => await PublishAsync();

    [RelayCommand]
    private async Task PublishAsync()
    {
        if (ReadinessChecks.Any(x => !x.IsOk))
        {
            StatusMessage = "لا يمكن نشر العرض قبل معالجة تحذيرات فحص الجاهزية.";
            return;
        }

        var saved = await SaveAsync("published");
        if (saved != null)
        {
            await _api.PublishOfferAsync(saved.Id);
            OfferState = "منشور";
            await LoadAsync();
            StatusMessage = "تم نشر العرض للصيدليات المستهدفة";
        }
    }

    [RelayCommand]
    private async Task PauseAsync()
    {
        if (_editingOfferId.HasValue)
            await _api.PauseOfferAsync(_editingOfferId.Value);
        OfferState = "موقوف";
        StatusMessage = "تم إيقاف العرض ولن يظهر للصيدلي.";
        RefreshAll();
    }

    [RelayCommand]
    private async Task DuplicateAsync()
    {
        if (IsBusy) return;
        _editingOfferId = null;
        SelectedOffer = null;
        OfferTitle = $"{OfferTitle} - نسخة";
        OfferState = "مسودة";
        var saved = await SaveAsync("draft");
        StatusMessage = saved == null
            ? "تعذر حفظ نسخة العرض."
            : $"تم نسخ العرض وحفظه كمسودة جديدة رقم {saved.Id}.";
    }

    [RelayCommand]
    private async Task DeleteOfferAsync()
    {
        if (IsBusy) return;
        try
        {
            IsBusy = true;
            if (_editingOfferId.HasValue)
                await _api.DeleteOfferAsync(_editingOfferId.Value);
            else if (SelectedOffer != null)
                Offers.Remove(SelectedOffer);

            _editingOfferId = null;
            SelectedOffer = null;
            IsBusy = false;
            await LoadAsync();
            StatusMessage = "تم حذف العرض من قاعدة البيانات.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"تعذر حذف العرض: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
            ApplyFilters();
            RefreshAll();
        }
    }

    [RelayCommand]
    private async Task SendTestAsync()
    {
        if (IsBusy) return;
        var pharmacy = SelectedPreviewPharmacy ?? TargetPharmacies.FirstOrDefault(x => x.IsSelected) ?? TargetPharmacies.FirstOrDefault();
        if (pharmacy == null)
        {
            StatusMessage = "لا توجد صيدلية متاحة لإرسال التجربة.";
            return;
        }

        try
        {
            var saved = _editingOfferId.HasValue
                ? await SaveAsync(ToApiStatus(OfferState))
                : await SaveAsync("draft");
            if (saved == null)
                return;

            var result = await _api.TestSendOfferAsync(saved.Id, new OfferTestSendRequest { PharmacyId = pharmacy.Id });
            StatusMessage = result?.Message ?? $"تم إرسال تجربة العرض إلى {pharmacy.Name}.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"تعذر إرسال التجربة: {ex.Message}";
        }
    }

    [RelayCommand]
    private void ExportReport()
    {
        var dialog = new SaveFileDialog
        {
            Title = "تصدير تقرير العروض",
            Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*",
            FileName = $"offers_report_{DateTime.Now:yyyyMMdd_HHmm}.csv"
        };

        if (dialog.ShowDialog() != true)
            return;

        var csv = BuildOffersReportCsv();
        File.WriteAllText(dialog.FileName, "\uFEFF" + csv, Encoding.UTF8);
        StatusMessage = $"تم تصدير تقرير العروض: {dialog.FileName}";
    }

    [RelayCommand]
    private void PreviewAddToOrder()
    {
        var pharmacy = SelectedPreviewPharmacy ?? TargetPharmacies.FirstOrDefault();
        var product = OfferProducts.FirstOrDefault();
        StatusMessage = product == null
            ? "المعاينة لا تحتوي على منتج لإضافته للطلب."
            : $"تم اختبار زر أضف للطلب في المعاينة: {product.ProductName} لصيدلية {pharmacy?.Name ?? "تجريبية"}.";
    }

    private string BuildOffersReportCsv()
    {
        var sb = new StringBuilder();
        sb.AppendLine("العرض,الحالة,النوع,المشاهدات,الطلبات,التحويل,ينتهي في,المنتجات,الصيدليات المستهدفة");
        foreach (var offer in Offers.OrderByDescending(o => o.EndsAt))
        {
            var sourceProducts = offer.Source?.OfferProducts.Select(p => p.ProductName).Where(x => !string.IsNullOrWhiteSpace(x)).ToList() ?? [];
            var products = sourceProducts.Count > 0
                ? string.Join(" | ", sourceProducts)
                : offer.Id == SelectedOffer?.Id ? ProductPreviewSummary : "-";
            var targetCount = offer.Source?.AudienceRule == "selected_pharmacies"
                ? offer.Source.TargetPharmacyIds.Count.ToString("N0")
                : "كل الصيدليات";
            sb.AppendLine(string.Join(",",
                Csv(offer.Title),
                Csv(offer.Status),
                Csv(offer.TypeLabel),
                Csv(offer.Views),
                Csv(offer.Orders),
                Csv(offer.Conversion),
                Csv(offer.EndsAtLabel),
                Csv(products),
                Csv(targetCount)));
        }

        sb.AppendLine();
        sb.AppendLine("ملخص,القيمة");
        foreach (var card in StatCards)
            sb.AppendLine($"{Csv(card.Title)},{Csv(card.Value)}");

        return sb.ToString();
    }

    private static string Csv(string? value)
    {
        value ??= string.Empty;
        return $"\"{value.Replace("\"", "\"\"")}\"";
    }

    private async Task<MarketingOfferDto?> SaveAsync(string status)
    {
        if (IsBusy) return null;
        IsBusy = true;
        try
        {
            if (string.IsNullOrWhiteSpace(OfferTitle))
            {
                StatusMessage = "اكتب عنوان العرض أولاً";
                return null;
            }
            if (EndsAt <= StartsAt)
            {
                StatusMessage = "تاريخ نهاية العرض يجب أن يكون بعد البداية";
                return null;
            }

            var imageUrls = await EnsureImagesUploadedAsync();
            var imageUrl = imageUrls.FirstOrDefault() ?? string.Empty;
            var request = new UpdateMarketingOfferRequest
            {
                Title = OfferTitle,
                Subtitle = OfferSubtitle,
                Description = OfferDescription,
                ImageUrl = imageUrl,
                ImageUrls = imageUrls,
                OfferType = ToApiOfferType(OfferType),
                AudienceRule = AudienceRule,
                StartsAt = StartsAt,
                EndsAt = EndsAt,
                Status = status,
                RemainingQuantity = RemainingQuantity > 0 ? RemainingQuantity : null,
                OldPrice = OldPrice > 0 ? OldPrice : OfferProducts.FirstOrDefault()?.OldPrice,
                NewPrice = NewPrice > 0 ? NewPrice : OfferProducts.FirstOrDefault()?.NewPrice,
                DiscountPercent = DiscountPercent > 0 ? DiscountPercent : OfferProducts.FirstOrDefault()?.DiscountPercent,
                OfferProducts = OfferProducts
                    .Where(p => p.ProductId > 0)
                    .Select(p => new MarketingOfferProductRequest
                    {
                        ProductId = p.ProductId,
                        OfferQuantity = p.OfferQuantity,
                        MinimumOrder = p.MinimumOrder,
                        OldPrice = p.OldPrice,
                        NewPrice = p.NewPrice,
                        GiftProduct = p.GiftProduct
                    })
                    .ToList(),
                TargetPharmacyIds = IsSpecificAudience
                    ? TargetPharmacies.Where(p => p.IsSelected).Select(p => p.Id).ToList()
                    : []
            };

            MarketingOfferDto? saved;
            if (_editingOfferId.HasValue)
                saved = await _api.UpdateOfferAsync(_editingOfferId.Value, request);
            else
            {
                saved = await _api.CreateOfferAsync(request);
                _editingOfferId = saved?.Id;
            }

            StatusMessage = status switch
            {
                "published" => "تم حفظ العرض وجاهز للنشر",
                "review" => "تم إرسال العرض للمراجعة",
                _ => "تم حفظ العرض كمسودة"
            };
            IsBusy = false;
            await LoadAsync();
            if (saved != null)
            {
                var savedOffer = Offers.FirstOrDefault(o => o.Id == saved.Id);
                if (savedOffer != null)
                    SelectOffer(savedOffer);
            }
            return saved;
        }
        catch (Exception ex)
        {
            StatusMessage = $"تعذر حفظ العرض: {ex.Message}";
            return null;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task<List<string>> EnsureImagesUploadedAsync()
    {
        if (OfferImages.Count == 0 && !string.IsNullOrWhiteSpace(OfferImagePath))
            OfferImages.Add(new OfferImageItem(OfferImagePath, true));

        if (OfferImages.Count == 0)
            return [];

        var orderedImages = OfferImages
            .OrderByDescending(x => x.IsPrimary)
            .ThenBy(x => x.DisplayName)
            .ToList();

        var urls = new List<string>();
        foreach (var image in orderedImages)
        {
            if (string.IsNullOrWhiteSpace(image.Path))
                continue;

            if (image.Path.StartsWith("/") || image.Path.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                urls.Add(image.Path);
                continue;
            }

            var uploaded = await _api.UploadOfferImageAsync(image.Path);
            var url = uploaded?.ImageUrl ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(url))
            {
                image.Path = url;
                urls.Add(url);
            }
        }

        if (urls.Count > 0)
        {
            _savedImageUrl = urls[0];
            OfferImagePath = urls[0];
            if (!OfferImages.Any(x => x.IsPrimary))
                OfferImages[0].IsPrimary = true;
        }

        return urls.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }

    private async Task<string> EnsureImageUploadedAsync()
    {
        if (string.IsNullOrWhiteSpace(OfferImagePath))
            return string.Empty;
        if (OfferImagePath.StartsWith("/") || OfferImagePath.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            return OfferImagePath;
        if (OfferImagePath == _savedImageUrl)
            return _savedImageUrl;

        var uploaded = await _api.UploadOfferImageAsync(OfferImagePath);
        _savedImageUrl = uploaded?.ImageUrl ?? string.Empty;
        OfferImagePath = _savedImageUrl;
        return _savedImageUrl;
    }

    private void BuildDemoRows()
    {
        var firstProduct = ProductCatalog.FirstOrDefault();
        var secondProduct = ProductCatalog.Skip(1).FirstOrDefault();
        var firstPrice = firstProduct is { UnitPrice: > 0 } ? firstProduct.UnitPrice : 120m;
        var secondPrice = secondProduct is { UnitPrice: > 0 } ? secondProduct.UnitPrice : 80m;

        OfferProducts = [
            new OfferProductItem
            {
                ProductId = firstProduct?.Id ?? 0,
                ProductName = firstProduct?.Name ?? ProductNames.FirstOrDefault() ?? "باراسيتامول 500 مجم",
                AvailableQuantity = firstProduct?.Quantity ?? 90,
                OfferQuantity = 24,
                OldPrice = firstPrice,
                NewPrice = Math.Round(firstPrice * 0.80m, 2),
                MinimumOrder = 2
            },
            new OfferProductItem
            {
                ProductId = secondProduct?.Id ?? 0,
                ProductName = secondProduct?.Name ?? ProductNames.Skip(1).FirstOrDefault() ?? "سيتريزين أقراص",
                AvailableQuantity = secondProduct?.Quantity ?? 12,
                OfferQuantity = 20,
                OldPrice = secondPrice,
                NewPrice = Math.Round(secondPrice * 0.85m, 2),
                MinimumOrder = 1,
                GiftProduct = "شرائط عينات"
            }
        ];
        TrackProductRows();

        PharmacyInteractions = [
            new("صيدلية النور", "01000000001", "اليوم 10:42", true, true, true, "#1042", "1,850 ج.م"),
            new("صيدلية الشفاء", "01000000002", "أمس 18:11", true, false, false, "-", "-"),
            new("صيدلية الرحمة", "01000000003", "هذا الأسبوع", true, true, true, "#1036", "920 ج.م")
        ];
    }

    private void TrackProductRows()
    {
        foreach (var product in OfferProducts)
            TrackProductRow(product);
    }

    private void TrackProductRow(OfferProductItem product) => product.PropertyChanged += (_, _) => RefreshAll();

    private void SeedDemoOffers()
    {
        Offers = [
            OfferDesignItem.Demo(-1, "خصم موسمي على المسكنات", "منشور", "خصم مباشر", 1240, 82, DateTime.Today.AddDays(5), true),
            OfferDesignItem.Demo(-2, "باكدج فيتامينات الشتاء", "قيد المراجعة", "باكدج منتجات", 410, 19, DateTime.Today.AddDays(12), false),
            OfferDesignItem.Demo(-3, "تصفية مخزون قريب الصلاحية", "مسودة", "تصفية مخزون", 0, 0, DateTime.Today.AddDays(20), false)
        ];
    }

    private void RefreshAll()
    {
        ApplyFilters();
        BuildStats();
        BuildReadiness();
        BuildAnalytics();
        BuildEditorSteps();
        BuildOperationalPanels();
        OnPropertyChanged(nameof(PreviewDiscountLabel));
        OnPropertyChanged(nameof(PreviewPriceLabel));
        OnPropertyChanged(nameof(PreviewOldPriceLabel));
        OnPropertyChanged(nameof(PreviewExpiryLabel));
        OnPropertyChanged(nameof(PreviewImagePath));
        OnPropertyChanged(nameof(ImageShortLabel));
        OnPropertyChanged(nameof(ImagesSummaryLabel));
        OnPropertyChanged(nameof(ProductPreviewSummary));
        OnPropertyChanged(nameof(ProductCatalogSummary));
        OnPropertyChanged(nameof(TargetedPharmaciesLabel));
        OnPropertyChanged(nameof(AudienceRule));
        OnPropertyChanged(nameof(IsSpecificAudience));
        OnPropertyChanged(nameof(PreviewPharmacyName));
        OnPropertyChanged(nameof(PreviewVisibilityResult));
    }

    private void BuildOperationalPanels()
    {
        PublishChecklistItems = new ObservableCollection<PublishChecklistItem>([
            new("بيانات العرض", !string.IsNullOrWhiteSpace(OfferTitle) && OfferTitle.Length >= 4, "العنوان والوصف واضحين للصيدلي"),
            new("المنتجات", OfferProducts.Count > 0, $"{OfferProducts.Count:N0} منتج داخل العرض"),
            new("الأسعار", NewPrice > 0 || OfferProducts.Any(x => x.NewPrice > 0), "أسعار وخصومات قابلة للمراجعة"),
            new("الاستهداف", !IsSpecificAudience || TargetPharmacies.Any(x => x.IsSelected), TargetedPharmaciesLabel),
            new("الصورة", !string.IsNullOrWhiteSpace(OfferImagePath), ImageShortLabel),
            new("المدة", EndsAt > StartsAt && EndsAt > DateTime.Today, $"{StartsAt:yyyy-MM-dd} إلى {EndsAt:yyyy-MM-dd}")
        ]);

        ImageAssetItems = new ObservableCollection<ImageAssetItem>([
            new("Banner", "1920x700", !string.IsNullOrWhiteSpace(OfferImagePath), "للواجهة الرئيسية داخل تطبيق الصيدلي"),
            new("Card", "1080x1350", !string.IsNullOrWhiteSpace(OfferImagePath), "لكارت العرض في قائمة العروض"),
            new("Thumbnail", "600x600", !string.IsNullOrWhiteSpace(OfferImagePath), "للقوائم الصغيرة والتنبيهات")
        ]);

        OfferCalendarItems = new ObservableCollection<OfferCalendarItem>(Offers
            .OrderBy(x => x.EndsAt)
            .Take(6)
            .Select(x => new OfferCalendarItem(x.Title, x.Status, x.EndsAt, x.EndsAt <= DateTime.Today.AddDays(3) ? "ينتهي قريبًا" : "مجدول")));

        var previewPharmacies = TargetPharmacies.Take(5).DefaultIfEmpty(new PharmacyTargetItem(0, "صيدلية تجريبية", "-", 0));
        VisibilityAuditItems = new ObservableCollection<VisibilityAuditItem>(previewPharmacies.Select(p =>
        {
            var isVisible = !ReadinessChecks.Any(x => !x.IsOk) && (!IsSpecificAudience || p.IsSelected);
            var reason = isVisible ? "سيظهر العرض" : IsSpecificAudience && !p.IsSelected ? "خارج الاستهداف" : "تحذيرات قبل النشر";
            return new VisibilityAuditItem(p.Name, p.Phone, isVisible, reason);
        }));
    }

    private void ApplyFilters()
    {
        var query = OfferSearch.Trim();
        var filtered = Offers.Where(o =>
            (StatusFilter == "الكل" || o.Status == StatusFilter) &&
            (string.IsNullOrWhiteSpace(query) || o.Title.Contains(query, StringComparison.OrdinalIgnoreCase)));
        FilteredOffers = new ObservableCollection<OfferDesignItem>(filtered);
    }

    private void BuildStats()
    {
        var published = Offers.Count(x => x.Status == "منشور");
        var review = Offers.Count(x => x.Status == "قيد المراجعة");
        var views = Offers.Sum(x => x.ViewsCount);
        var orders = Offers.Sum(x => x.OrdersCount);
        var conversion = views == 0 ? "0%" : $"{orders / (double)views:P1}";
        StatCards = [
            new("إجمالي العروض", Offers.Count.ToString("N0"), "\uE71D", "#FF38BDF8"),
            new("العروض المنشورة", published.ToString("N0"), "\uE8FB", "#FF31D0AA"),
            new("قيد المراجعة", review.ToString("N0"), "\uE9D9", "#FFF59E0B"),
            new("إجمالي المشاهدات", views.ToString("N0"), "\uE890", "#FF60A5FA"),
            new("معدل التحويل", conversion, "\uE9D2", "#FFFB7185")
        ];
    }

    private void BuildReadiness()
    {
        var checks = new List<ReadinessCheckItem>();
        AddCheck(string.IsNullOrWhiteSpace(OfferImagePath), "الصورة غير مرفوعة");
        AddCheck(OfferProducts.Count == 0, "لا توجد منتجات مرتبطة");
        AddCheck(OfferProducts.Any(x => x.ProductId <= 0), "اختار المنتجات من ملف المخزون الرئيسي");
        AddCheck(EndsAt <= StartsAt, "تاريخ النهاية قبل البداية");
        AddCheck(OfferProducts.Any(x => x.DiscountPercent > 60), "الخصم أكبر من المسموح");
        AddCheck(OfferProducts.Any(x => x.OfferQuantity > x.AvailableQuantity), "الكمية غير كافية");
        AddCheck(OfferDescription.Length > 180, "الوصف طويل جداً على الموبايل");
        AddCheck(IsSpecificAudience && !TargetPharmacies.Any(x => x.IsSelected), "لم يتم اختيار جمهور مستهدف");

        if (checks.Count == 0)
            checks.Add(new ReadinessCheckItem(true, "العرض جاهز للنشر"));
        ReadinessChecks = new ObservableCollection<ReadinessCheckItem>(checks);

        void AddCheck(bool failed, string message)
        {
            if (failed) checks.Add(new ReadinessCheckItem(false, message));
        }
    }

    private void BuildAnalytics()
    {
        var offer = SelectedOffer ?? Offers.FirstOrDefault();
        var views = offer?.ViewsCount ?? 0;
        var clicks = Math.Max(0, views / 3);
        var cartAdds = Math.Max(0, views / 6);
        var orders = offer?.OrdersCount ?? 0;
        AnalyticsCards = [
            new("شاهدت العرض", views.ToString("N0"), "\uE890", "#FF38BDF8"),
            new("ضغطات", clicks.ToString("N0"), "\uE8B7", "#FFF59E0B"),
            new("إضافات للسلة", cartAdds.ToString("N0"), "\uE7BF", "#FF31D0AA"),
            new("طلبات", orders.ToString("N0"), "\uE8A5", "#FF60A5FA"),
            new("إجمالي المبيعات", $"{orders * 850:N0} ج.م", "\uE8C7", "#FF31D0AA"),
            new("معدل التحويل", views == 0 ? "0%" : $"{orders / (double)views:P1}", "\uE9D2", "#FFFB7185"),
            new("أفضل منتج", OfferProducts.FirstOrDefault()?.ProductName ?? "-", "\uE719", "#FF38BDF8"),
            new("آخر طلب", PharmacyInteractions.FirstOrDefault(x => x.Ordered)?.LastOrder ?? "-", "\uE8A5", "#FFF59E0B")
        ];
    }

    private void BuildEditorSteps()
    {
        EditorSteps = [
            new("1", "بيانات العرض", "#FF31D0AA", "#2631D0AA"),
            new("2", "المنتجات", OfferProducts.Count > 0 ? "#FF31D0AA" : "#FF64748B", "#141E293B"),
            new("3", "الاستهداف", TargetRules.Any(x => x.IsSelected) ? "#FF31D0AA" : "#FF64748B", "#141E293B"),
            new("4", "المعاينة", "#FF38BDF8", "#141E293B"),
            new("5", "النشر", ReadinessChecks.Any(x => !x.IsOk) ? "#FFF59E0B" : "#FF31D0AA", "#141E293B")
        ];
    }

    partial void OnOfferSearchChanged(string value) => ApplyFilters();
    partial void OnStatusFilterChanged(string value) => ApplyFilters();
    partial void OnDateFilterChanged(string value) => ApplyFilters();
    partial void OnOfferTitleChanged(string value) => RefreshAll();
    partial void OnOfferSubtitleChanged(string value) => RefreshAll();
    partial void OnOfferTypeChanged(string value) => RefreshAll();
    partial void OnOfferStateChanged(string value) => RefreshAll();
    partial void OnOfferDescriptionChanged(string value) => RefreshAll();
    partial void OnOfferTermsChanged(string value) => RefreshAll();
    partial void OnInternalNotesChanged(string value) => RefreshAll();
    partial void OnOfferImagePathChanged(string value) => RefreshAll();
    partial void OnOldPriceChanged(decimal value) => RefreshAll();
    partial void OnNewPriceChanged(decimal value) => RefreshAll();
    partial void OnDiscountPercentChanged(int value) => RefreshAll();
    partial void OnRemainingQuantityChanged(int value) => RefreshAll();
    partial void OnStartsAtChanged(DateTime value) => RefreshAll();
    partial void OnEndsAtChanged(DateTime value) => RefreshAll();
    partial void OnSelectedPreviewPharmacyChanged(PharmacyTargetItem? value)
    {
        OnPropertyChanged(nameof(PreviewPharmacyName));
        OnPropertyChanged(nameof(PreviewVisibilityResult));
    }

    private static string ToApiOfferType(string display) => display switch
    {
        "باكدج منتجات" => "bundle",
        "اشتر كمية وخد هدية" => "buy_get",
        "منتج جديد" => "new_product",
        "تصفية مخزون" => "clearance",
        "عرض موجه" => "targeted",
        _ => "discount"
    };

    private static string ToDisplayOfferType(string api) => api switch
    {
        "bundle" => "باكدج منتجات",
        "buy_get" => "اشتر كمية وخد هدية",
        "new_product" => "منتج جديد",
        "clearance" => "تصفية مخزون",
        "targeted" => "عرض موجه",
        _ => "خصم مباشر"
    };

    private static string ToDisplayStatus(string api) => api switch
    {
        "published" => "منشور",
        "review" => "قيد المراجعة",
        "paused" => "موقوف",
        "expired" => "منتهي",
        _ => "مسودة"
    };

    private static string ToApiStatus(string display) => display switch
    {
        "منشور" => "published",
        "قيد المراجعة" => "review",
        "موقوف" => "paused",
        "منتهي" => "expired",
        _ => "draft"
    };
}

public partial class OfferDesignItem : ObservableObject
{
    [ObservableProperty] private bool _isSelected;

    public int Id { get; }
    public string Title { get; }
    public string Status { get; }
    public string TypeLabel { get; }
    public int ViewsCount { get; }
    public int OrdersCount { get; }
    public DateTime EndsAt { get; }
    public MarketingOfferDto? Source { get; }
    public string Views => ViewsCount.ToString("N0");
    public string Orders => OrdersCount.ToString("N0");
    public string Conversion => ViewsCount == 0 ? "0%" : $"{OrdersCount / (double)ViewsCount:P1}";
    public string EndsAtLabel => $"ينتهي: {EndsAt:yyyy-MM-dd}";
    public string Accent => Status switch
    {
        "منشور" => "#FF31D0AA",
        "قيد المراجعة" => "#FFF59E0B",
        "موقوف" => "#FF60A5FA",
        "منتهي" => "#FFFB7185",
        _ => "#FF38BDF8"
    };
    public string CardBackground => IsSelected ? "#2238BDF8" : "#B8142238";
    public string ActiveBorder => IsSelected ? "#FF38BDF8" : "#2638BDF8";

    private OfferDesignItem(int id, string title, string status, string typeLabel, int views, int orders, DateTime endsAt, MarketingOfferDto? source)
    {
        Id = id;
        Title = title;
        Status = status;
        TypeLabel = typeLabel;
        ViewsCount = views;
        OrdersCount = orders;
        EndsAt = endsAt;
        Source = source;
    }

    public static OfferDesignItem FromDto(MarketingOfferDto dto)
    {
        var status = dto.Status switch
        {
            "published" => "منشور",
            "review" => "قيد المراجعة",
            "paused" => "موقوف",
            "expired" or "depleted" => "منتهي",
            _ => "مسودة"
        };
        return new OfferDesignItem(dto.Id, dto.Title, status, dto.OfferType, 0, 0, dto.EndsAt, dto);
    }

    public static OfferDesignItem Demo(int id, string title, string status, string typeLabel, int views, int orders, DateTime endsAt, bool selected)
    {
        return new OfferDesignItem(id, title, status, typeLabel, views, orders, endsAt, null) { IsSelected = selected };
    }

    partial void OnIsSelectedChanged(bool value)
    {
        OnPropertyChanged(nameof(CardBackground));
        OnPropertyChanged(nameof(ActiveBorder));
    }
}

public partial class OfferProductItem : ObservableObject
{
    [ObservableProperty] private int _productId;
    [ObservableProperty] private string _productName = string.Empty;
    [ObservableProperty] private int _availableQuantity;
    [ObservableProperty] private int _offerQuantity;
    [ObservableProperty] private decimal _oldPrice;
    [ObservableProperty] private decimal _newPrice;
    [ObservableProperty] private int _minimumOrder = 1;
    [ObservableProperty] private string _giftProduct = string.Empty;

    public int DiscountPercent => OldPrice <= 0 || NewPrice <= 0 ? 0 : (int)Math.Round((1 - (NewPrice / OldPrice)) * 100);
    public string DiscountLabel => DiscountPercent <= 0 ? "-" : $"{DiscountPercent}%";
    public string Warning => OfferQuantity > AvailableQuantity
        ? "الكمية غير كافية"
        : AvailableQuantity <= 15 ? "كمية قليلة" : "";

    public static OfferProductItem FromDto(MarketingOfferProductDto dto) => new()
    {
        ProductId = dto.ProductId,
        ProductName = dto.ProductName,
        AvailableQuantity = dto.AvailableQuantity,
        OfferQuantity = dto.OfferQuantity,
        OldPrice = dto.OldPrice,
        NewPrice = dto.NewPrice,
        MinimumOrder = dto.MinimumOrder,
        GiftProduct = dto.GiftProduct
    };

    partial void OnOldPriceChanged(decimal value) => Refresh();
    partial void OnNewPriceChanged(decimal value) => Refresh();
    partial void OnOfferQuantityChanged(int value) => Refresh();
    partial void OnAvailableQuantityChanged(int value) => Refresh();

    private void Refresh()
    {
        OnPropertyChanged(nameof(DiscountPercent));
        OnPropertyChanged(nameof(DiscountLabel));
        OnPropertyChanged(nameof(Warning));
    }
}

public partial class OfferImageItem : ObservableObject
{
    [ObservableProperty] private string _path = string.Empty;
    [ObservableProperty] private bool _isPrimary;

    public OfferImageItem(string path, bool isPrimary = false)
    {
        _path = path;
        _isPrimary = isPrimary;
    }

    public string DisplayName => string.IsNullOrWhiteSpace(Path) ? "صورة بدون اسم" : System.IO.Path.GetFileName(Path);
    public string StatusText => Path.StartsWith("/") || Path.StartsWith("http", StringComparison.OrdinalIgnoreCase) ? "مرفوعة" : "جاهزة للرفع";
    public string PrimaryText => IsPrimary ? "رئيسية" : "اجعلها رئيسية";
    public string Accent => IsPrimary ? "#FF31D0AA" : "#FF38BDF8";

    partial void OnPathChanged(string value)
    {
        OnPropertyChanged(nameof(DisplayName));
        OnPropertyChanged(nameof(StatusText));
    }

    partial void OnIsPrimaryChanged(bool value)
    {
        OnPropertyChanged(nameof(PrimaryText));
        OnPropertyChanged(nameof(Accent));
    }
}

public partial class TargetRuleItem(string key, string label, bool isSelected = false) : ObservableObject
{
    public event EventHandler? SelectionChanged;
    public string Key { get; } = key;
    public string Label { get; } = label;
    [ObservableProperty] private bool _isSelected = isSelected;

    partial void OnIsSelectedChanged(bool value) => SelectionChanged?.Invoke(this, EventArgs.Empty);
}

public partial class PharmacyTargetItem(int id, string name, string phone, decimal balance = 0) : ObservableObject
{
    public int Id { get; } = id;
    public string Name { get; } = name;
    public string Phone { get; } = phone;
    public decimal Balance { get; } = balance;
    [ObservableProperty] private bool _isSelected;

    public override string ToString() => Name;
}

public record ProductPickerItem(int Id, string Name, int Quantity, decimal UnitPrice, string Category)
{
    public string DisplayName => $"{Name} | المتاح {Quantity:N0} | {UnitPrice:N0} ج.م";

    public static ProductPickerItem FromDto(ProductDto product) => new(
        product.Id,
        product.Name,
        product.Quantity,
        product.UnitPrice,
        product.CategoryName ?? product.Category);

    public override string ToString() => DisplayName;
}

public record MetricCard(string Title, string Value, string Icon, string Accent);
public record EditorStepItem(string Number, string Title, string Accent, string Background);
public record ReadinessCheckItem(bool IsOk, string Message)
{
    public string Icon => IsOk ? "\uE930" : "\uE7BA";
    public string Accent => IsOk ? "#FF31D0AA" : "#FFF59E0B";
}
public record PharmacyInteractionItem(
    string PharmacyName,
    string Phone,
    string ViewedAt,
    bool Clicked,
    bool AddedToCart,
    bool Ordered,
    string LastOrder,
    string OrderValue);

public record PublishChecklistItem(string Title, bool IsOk, string Detail)
{
    public string StatusText => IsOk ? "جاهز" : "ناقص";
    public string Accent => IsOk ? "#FF31D0AA" : "#FFF59E0B";
}

public record ImageAssetItem(string Title, string Size, bool IsReady, string Usage)
{
    public string StatusText => IsReady ? "جاهزة" : "تحتاج صورة";
    public string Accent => IsReady ? "#FF31D0AA" : "#FFFF6FAE";
}

public record OfferCalendarItem(string Title, string Status, DateTime Date, string Hint)
{
    public string DateLabel => Date.ToString("yyyy-MM-dd");
    public string Accent => Hint.Contains("قريب") ? "#FFF59E0B" : "#FF38BDF8";
}

public record VisibilityAuditItem(string PharmacyName, string Phone, bool IsVisible, string Reason)
{
    public string StatusText => IsVisible ? "ظاهر" : "غير ظاهر";
    public string Accent => IsVisible ? "#FF31D0AA" : "#FFFF6FAE";
}
