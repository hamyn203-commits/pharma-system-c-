using AlNeda.Core.Entities;

namespace AlNeda.Core.Models;

/// <summary>
/// 🗺️ Mapper مركزي لتحويل MarketingOffer إلى DTOs
/// يستخدم من قبل Controllers و Services — لا تكرار ولا اعتماد دائري
/// </summary>
public static class MarketingOfferMapper
{
    /// <summary>تحويل كيان العرض إلى DTO</summary>
    public static MarketingOfferDto ToDto(MarketingOffer offer, DateTime now) => new()
    {
        Id = offer.Id,
        Title = offer.Title,
        Subtitle = offer.Subtitle,
        Description = offer.Description,
        ImageUrl = offer.ImageUrl,
        ImageUrls = OfferImageUrls(offer).ToList(),
        OfferType = offer.OfferType,
        AudienceRule = offer.AudienceRule,
        StartsAt = offer.StartsAt,
        EndsAt = offer.EndsAt,
        Status = offer.Status,
        QuantityLimit = offer.QuantityLimit,
        RemainingQuantity = offer.RemainingQuantity,
        OldPrice = offer.OldPrice,
        NewPrice = offer.NewPrice,
        DiscountPercent = offer.DiscountPercent,
        UpdatedAt = offer.UpdatedAt,
        IsActive = offer.Status == "published"
            && offer.StartsAt <= now
            && now < offer.EndsAt
            && (!offer.RemainingQuantity.HasValue || offer.RemainingQuantity.Value > 0),
        TargetPharmacyIds = offer.PharmacyTargets.Select(t => t.PharmacyId).ToList(),
        OfferProducts = offer.OfferProducts.OrderBy(p => p.Id).Select(ToProductDto).ToList(),
        AudienceLabel = offer.AudienceRule == "selected_pharmacies"
            ? $"صيدليات محددة ({offer.PharmacyTargets.Count})"
            : offer.AudienceRule == "active_pharmacies_last_30d"
                ? "الصيدليات النشطة آخر 30 يوم"
                : offer.AudienceRule.StartsWith("geo:", StringComparison.OrdinalIgnoreCase)
                    ? $"منطقة: {offer.AudienceRule[4..]}"
                    : "كل الصيدليات"
    };

    /// <summary>تحويل منتج العرض إلى DTO</summary>
    public static MarketingOfferProductDto ToProductDto(MarketingOfferProduct offerProduct) => new()
    {
        Id = offerProduct.Id,
        ProductId = offerProduct.ProductId,
        ProductName = offerProduct.Product?.Name ?? string.Empty,
        AvailableQuantity = offerProduct.Product?.Quantity ?? 0,
        OfferQuantity = offerProduct.OfferQuantity,
        MinimumOrder = offerProduct.MinimumOrder,
        OldPrice = offerProduct.OldPrice,
        NewPrice = offerProduct.NewPrice,
        GiftProduct = offerProduct.GiftProduct
    };

    /// <summary>استخراج جميع روابط الصور من العرض</summary>
    private static IEnumerable<string> OfferImageUrls(MarketingOffer offer)
    {
        // الصورة الرئيسية
        if (!string.IsNullOrWhiteSpace(offer.ImageUrl))
            yield return offer.ImageUrl;

        // الصور الإضافية
        if (!string.IsNullOrWhiteSpace(offer.AdditionalImageUrls))
        {
            foreach (var url in offer.AdditionalImageUrls.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                if (!string.IsNullOrWhiteSpace(url))
                    yield return url;
            }
        }
    }
}
