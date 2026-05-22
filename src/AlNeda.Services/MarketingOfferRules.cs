using AlNeda.Core.Entities;
using AlNeda.Data;
using AlNeda.DomainLogic;
using Microsoft.EntityFrameworkCore;

namespace AlNeda.Services;

public static class MarketingOfferRules
{
    public const string Published = "published";
    public const string Paused = "paused";
    public const string Cancelled = "cancelled";
    public const string Depleted = "depleted";
    public const int ConversionWindowHours = 48;

    public static bool IsActive(MarketingOffer offer, DateTime now)
    {
        return MarketingOffers.isActive(
            offer.Status,
            offer.StartsAt,
            offer.EndsAt,
            offer.RemainingQuantity.HasValue ? new int?(offer.RemainingQuantity.Value) : null,
            now);
    }

    public static string EventDateKey(DateTime occurredAt) => MarketingOffers.eventDateKey(occurredAt);

    public static string DeviceKey(string? deviceId) => MarketingOffers.deviceKey(deviceId);

    public static bool UsesDeviceFallback(string? deviceId) => MarketingOffers.usesDeviceFallback(deviceId);

    public static string NormalizeStatus(string? value) => MarketingOffers.normalizeStatus(value ?? string.Empty);

    public static string NormalizeOfferType(string? value) => MarketingOffers.normalizeOfferType(value ?? string.Empty);

    public static string NormalizeAudienceRule(string? value) => MarketingOffers.normalizeAudienceRule(value ?? string.Empty);

    public static string ValidateSchedule(DateTime startsAt, DateTime endsAt) =>
        MarketingOffers.validateSchedule(startsAt, endsAt);

    public static string ValidatePrices(decimal? oldPrice, decimal? newPrice, int? discountPercent) =>
        MarketingOffers.validatePrices(
            oldPrice.HasValue ? new decimal?(oldPrice.Value) : null,
            newPrice.HasValue ? new decimal?(newPrice.Value) : null,
            discountPercent.HasValue ? new int?(discountPercent.Value) : null);

    public static string ValidateQuantity(int? quantityLimit, int? remainingQuantity) =>
        MarketingOffers.validateQuantity(
            quantityLimit.HasValue ? new int?(quantityLimit.Value) : null,
            remainingQuantity.HasValue ? new int?(remainingQuantity.Value) : null);

    public static string NextStatusAfterQuantity(string? requestedStatus, int? remainingQuantity) =>
        MarketingOffers.nextStatusAfterQuantity(
            requestedStatus ?? string.Empty,
            remainingQuantity.HasValue ? new int?(remainingQuantity.Value) : null);

    public static async Task<bool> MatchesAudienceAsync(AppDbContext db, MarketingOffer offer, Pharmacy pharmacy, DateTime now)
    {
        var rule = (offer.AudienceRule ?? "all").Trim();
        if (rule.Length == 0 || rule.Equals("all", StringComparison.OrdinalIgnoreCase))
            return true;

        if (rule.Equals("selected_pharmacies", StringComparison.OrdinalIgnoreCase))
            return await db.MarketingOfferPharmacyTargets.AnyAsync(t => t.MarketingOfferId == offer.Id && t.PharmacyId == pharmacy.Id);

        if (rule.Equals("active_pharmacies_last_30d", StringComparison.OrdinalIgnoreCase))
        {
            var since = now.AddDays(-30);
            return await db.Orders.AnyAsync(o => o.PharmacyId == pharmacy.Id && o.CreatedAt >= since);
        }

        if (rule.StartsWith("geo:", StringComparison.OrdinalIgnoreCase))
        {
            var location = rule[4..].Trim();
            return !string.IsNullOrEmpty(location)
                && pharmacy.Address.Contains(location, StringComparison.OrdinalIgnoreCase);
        }

        return false;
    }
}
