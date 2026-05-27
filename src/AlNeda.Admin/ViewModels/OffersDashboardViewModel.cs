using System.Collections.ObjectModel;
using AlNeda.Admin.Services.ApiClient;
using AlNeda.Core.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AlNeda.Admin.ViewModels;

public partial class OffersDashboardViewModel : ObservableObject
{
    private readonly IAlNedaApiClient _api;

    [ObservableProperty] private ObservableCollection<OfferAnalyticsDto> _offers = [];
    [ObservableProperty] private int _activeOffers;
    [ObservableProperty] private int _publishedOffers;
    [ObservableProperty] private int _uniqueViewedPharmacies;
    [ObservableProperty] private int _uniqueDailyImpressions;
    [ObservableProperty] private int _clicks;
    [ObservableProperty] private int _ordersFromOffers;
    [ObservableProperty] private double _conversionRate;
    [ObservableProperty] private bool _hasDeviceFallbackEvents;
    [ObservableProperty] private string _statusMessage = "جاهز لتحميل مؤشرات العروض";
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private bool _isLoading;

    public int TotalOffers => Offers.Count;
    public int ReviewOffers => Offers.Count(IsReviewOffer);
    public int DraftOffers => Offers.Count(IsDraftOffer);
    public IEnumerable<OfferAnalyticsDto> PublishedOfferItems => Offers.Where(IsPublishedOffer);
    public IEnumerable<OfferAnalyticsDto> ReviewOfferItems => Offers.Where(IsReviewOffer);
    public IEnumerable<OfferAnalyticsDto> DraftOfferItems => Offers.Where(IsDraftOffer);
    public IEnumerable<OfferAnalyticsDto> AttentionOfferItems => Offers
        .Where(x => IsReviewOffer(x) || IsDraftOffer(x) || x.ConversionRate <= 0.02)
        .Take(8);

    public string ConversionRateText => $"{ConversionRate:P1}";
    public string DeviceFallbackWarning => HasDeviceFallbackEvents
        ? "تنبيه: بعض الأحداث بلا DeviceId، لذلك إحصاء الجهاز تقديري ويعتمد على الصيدلية."
        : "إحصاء المشاهدات اليومية يعتمد على DeviceId عند توفره.";

    public OffersDashboardViewModel(IAlNedaApiClient api)
    {
        _api = api;
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        IsLoading = true;
        try
        {
            var analytics = await _api.GetOffersAnalyticsAsync();
            if (analytics == null)
            {
                StatusMessage = "لم تصل بيانات من API";
                return;
            }

            ActiveOffers = analytics.ActiveOffers;
            PublishedOffers = analytics.PublishedOffers;
            UniqueViewedPharmacies = analytics.UniqueViewedPharmacies;
            UniqueDailyImpressions = analytics.UniqueDailyImpressions;
            Clicks = analytics.Clicks;
            OrdersFromOffers = analytics.OrdersFromOffers;
            ConversionRate = analytics.ConversionRate;
            HasDeviceFallbackEvents = analytics.HasDeviceFallbackEvents;
            Offers = new ObservableCollection<OfferAnalyticsDto>(
                analytics.Offers.OrderByDescending(o => o.ConversionRate));
            StatusMessage = Offers.Count == 0
                ? "لا توجد عروض بعد. أنشئ أول عرض من صفحة إدارة العروض."
                : $"تم تحميل {Offers.Count:N0} عرض من API — {ActiveOffers} نشط حالياً";
            OnPropertyChanged(nameof(ConversionRateText));
            OnPropertyChanged(nameof(DeviceFallbackWarning));
        }
        catch (Exception ex)
        {
            StatusMessage = $"تعذر تحميل مؤشرات العروض: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
            IsLoading = false;
            RefreshOfferBreakdowns();
        }
    }

    partial void OnConversionRateChanged(double value) => OnPropertyChanged(nameof(ConversionRateText));
    partial void OnHasDeviceFallbackEventsChanged(bool value) => OnPropertyChanged(nameof(DeviceFallbackWarning));
    partial void OnOffersChanged(ObservableCollection<OfferAnalyticsDto> value) => RefreshOfferBreakdowns();

    private void RefreshOfferBreakdowns()
    {
        OnPropertyChanged(nameof(TotalOffers));
        OnPropertyChanged(nameof(ReviewOffers));
        OnPropertyChanged(nameof(DraftOffers));
        OnPropertyChanged(nameof(PublishedOfferItems));
        OnPropertyChanged(nameof(ReviewOfferItems));
        OnPropertyChanged(nameof(DraftOfferItems));
        OnPropertyChanged(nameof(AttentionOfferItems));
    }

    private static bool IsPublishedOffer(OfferAnalyticsDto offer) =>
        string.Equals(offer.Status, "published", StringComparison.OrdinalIgnoreCase);

    private static bool IsReviewOffer(OfferAnalyticsDto offer) =>
        string.Equals(offer.Status, "review", StringComparison.OrdinalIgnoreCase);

    private static bool IsDraftOffer(OfferAnalyticsDto offer) =>
        string.Equals(offer.Status, "draft", StringComparison.OrdinalIgnoreCase) ||
        string.IsNullOrWhiteSpace(offer.Status);
}