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
            Offers = new ObservableCollection<OfferAnalyticsDto>(analytics.Offers);
            StatusMessage = "تم تحديث لوحة العروض من API";
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
        }
    }

    partial void OnConversionRateChanged(double value) => OnPropertyChanged(nameof(ConversionRateText));
    partial void OnHasDeviceFallbackEventsChanged(bool value) => OnPropertyChanged(nameof(DeviceFallbackWarning));
}
