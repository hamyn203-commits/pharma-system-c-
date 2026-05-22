namespace AlNeda.Core.Models;

public class MarketingOfferDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public List<string> ImageUrls { get; set; } = [];
    public string OfferType { get; set; } = "discount";
    public string AudienceRule { get; set; } = "all";
    public DateTime StartsAt { get; set; }
    public DateTime EndsAt { get; set; }
    public string Status { get; set; } = "draft";
    public int? QuantityLimit { get; set; }
    public int? RemainingQuantity { get; set; }
    public decimal? OldPrice { get; set; }
    public decimal? NewPrice { get; set; }
    public int? DiscountPercent { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsActive { get; set; }
    public List<int> TargetPharmacyIds { get; set; } = [];
    public List<MarketingOfferProductDto> OfferProducts { get; set; } = [];
    public string AudienceLabel { get; set; } = "كل الصيدليات";
}

public class CreateMarketingOfferRequest
{
    public string Title { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public List<string> ImageUrls { get; set; } = [];
    public string OfferType { get; set; } = "discount";
    public string AudienceRule { get; set; } = "all";
    public DateTime StartsAt { get; set; } = DateTime.Today;
    public DateTime EndsAt { get; set; } = DateTime.Today.AddDays(7);
    public int? QuantityLimit { get; set; }
    public int? RemainingQuantity { get; set; }
    public decimal? OldPrice { get; set; }
    public decimal? NewPrice { get; set; }
    public int? DiscountPercent { get; set; }
    public List<int> TargetPharmacyIds { get; set; } = [];
    public List<MarketingOfferProductRequest> OfferProducts { get; set; } = [];
}

public class MarketingOfferProductDto
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int AvailableQuantity { get; set; }
    public int OfferQuantity { get; set; }
    public int MinimumOrder { get; set; } = 1;
    public decimal OldPrice { get; set; }
    public decimal NewPrice { get; set; }
    public string GiftProduct { get; set; } = string.Empty;
}

public class MarketingOfferProductRequest
{
    public int ProductId { get; set; }
    public int OfferQuantity { get; set; }
    public int MinimumOrder { get; set; } = 1;
    public decimal OldPrice { get; set; }
    public decimal NewPrice { get; set; }
    public string GiftProduct { get; set; } = string.Empty;
}

public class OfferImageUploadResponse
{
    public string ImageUrl { get; set; } = string.Empty;
}

public class UpdateMarketingOfferRequest : CreateMarketingOfferRequest
{
    public string Status { get; set; } = "draft";
}

public class OfferEventRequest
{
    public string Type { get; set; } = string.Empty;
    public string? DeviceId { get; set; }
}

public class OfferEventResponse
{
    public bool Accepted { get; set; }
    public bool Counted { get; set; }
    public bool UsedDeviceFallback { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class OfferTestSendRequest
{
    public int PharmacyId { get; set; }
    public string? DeviceId { get; set; }
}

public class OfferTestSendResponse
{
    public bool Sent { get; set; }
    public bool WouldAppearOnMobile { get; set; }
    public string Message { get; set; } = string.Empty;
    public int OfferId { get; set; }
    public string OfferTitle { get; set; } = string.Empty;
    public int PharmacyId { get; set; }
    public string PharmacyName { get; set; } = string.Empty;
}

public class OfferAnalyticsDto
{
    public int OfferId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int UniqueViewedPharmacies { get; set; }
    public int UniqueDailyImpressions { get; set; }
    public int Clicks { get; set; }
    public int Orders { get; set; }
    public double ClickRate { get; set; }
    public double ConversionRate { get; set; }
    public bool HasDeviceFallbackEvents { get; set; }
    public string ConversionDenominator { get; set; } = "unique_viewed_pharmacies";
}

public class OffersAnalyticsSummaryDto
{
    public int ActiveOffers { get; set; }
    public int PublishedOffers { get; set; }
    public int UniqueViewedPharmacies { get; set; }
    public int UniqueDailyImpressions { get; set; }
    public int Clicks { get; set; }
    public int OrdersFromOffers { get; set; }
    public double ConversionRate { get; set; }
    public bool HasDeviceFallbackEvents { get; set; }
    public string ActiveDefinition { get; set; } = "published, not paused/cancelled/depleted, StartsAt <= now < EndsAt, and remaining quantity is not zero";
    public string ImpressionRule { get; set; } = "one counted impression per OfferId + PharmacyId + DeviceId + calendar day; missing DeviceId falls back to PharmacyId";
    public string ConversionDefinition { get; set; } = "orders with SourceOfferId divided by unique pharmacies that viewed the offer";
    public List<OfferAnalyticsDto> Offers { get; set; } = [];
}
