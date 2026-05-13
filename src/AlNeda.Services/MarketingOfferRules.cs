using AlNeda.Core.Entities;
using AlNeda.Data;
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
        // The mobile/API active window is exclusive at the end: StartsAt <= now < EndsAt.
        return offer.Status == Published
            && offer.StartsAt <= now
            && now < offer.EndsAt
            && offer.Status is not Paused and not Cancelled and not Depleted
            && (offer.RemainingQuantity is null or > 0);
    }

    public static string EventDateKey(DateTime occurredAt) => occurredAt.ToString("yyyy-MM-dd");

    public static string DeviceKey(string? deviceId) =>
        string.IsNullOrWhiteSpace(deviceId) ? "pharmacy" : deviceId.Trim();

    public static bool UsesDeviceFallback(string? deviceId) => string.IsNullOrWhiteSpace(deviceId);

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
