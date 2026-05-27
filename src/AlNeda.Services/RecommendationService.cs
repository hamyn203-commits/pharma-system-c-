using AlNeda.Core.Entities;
using AlNeda.Core.Models;
using AlNeda.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using static AlNeda.Core.Models.MarketingOfferMapper;

namespace AlNeda.Services;

/// <summary>
/// 🤖 خدمة التوصيات الذكية للعروض — منفصلة تماماً
/// تحسب "Offer Score" لكل صيدلية بناءً على:
/// - تاريخ المشتريات (الفئة، المنتجات)
/// - المنطقة الجغرافية
/// - العروض السابقة التي تفاعل معها
/// - سلوك النقر والانطباع
/// </summary>
public interface IRecommendationService
{
    /// <summary>الحصول على العروض الموصى بها لصيدلية معينة</summary>
    Task<List<MarketingOfferDto>> GetRecommendedOffersAsync(int pharmacyId, int count = 10);

    /// <summary>الحصول على العروض الأكثر صلة بصيدلية (مع ترتيب ذكي)</summary>
    Task<List<MarketingOfferDto>> GetPersonalizedOffersAsync(int pharmacyId, string? deviceId = null);

    /// <summary>ترتيب العروض بناءً على السجل التاريخي للصيدلية</summary>
    Task<List<MarketingOfferDto>> RankOffersForPharmacyAsync(int pharmacyId, List<MarketingOfferDto> offers);

    /// <summary>الحصول على توصيات لصفحة العروض الرئيسية (مزيج من الأكثر مشاهدة والأحدث)</summary>
    Task<RecommendedOffersResult> GetHomePageRecommendationsAsync(int pharmacyId);
}

public class RecommendedOffersResult
{
    public List<MarketingOfferDto> TopPicks { get; set; } = [];      // مخصصة للصيدلية
    public List<MarketingOfferDto> Trending { get; set; } = [];       // الأكثر تفاعلاً
    public List<MarketingOfferDto> NewArrivals { get; set; } = [];    // الأحدث
    public List<MarketingOfferDto> EndingSoon { get; set; } = [];     // تنتهي قريباً
}

/// <summary>
/// سجل تفضيلات الصيدلية — يُستخدم مؤقتاً للتوصيات
/// </summary>
public class PharmacyPreferenceProfile
{
    public int PharmacyId { get; set; }
    public string? Region { get; set; }
    public HashSet<int> PurchasedCategoryIds { get; set; } = [];
    public HashSet<int> PurchasedProductIds { get; set; } = [];
    public HashSet<int> ViewedOfferIds { get; set; } = [];
    public HashSet<int> ClickedOfferIds { get; set; } = [];
    public int TotalOrders { get; set; }
    public decimal AverageOrderValue { get; set; }
}

public class RecommendationService : IRecommendationService
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;
    private readonly ILogger<RecommendationService> _logger;

    // أوزان التوصية
    private const double WeightCategoryMatch = 0.35;
    private const double WeightRecentOrders = 0.25;
    private const double WeightRegionMatch = 0.15;
    private const double WeightEngagement = 0.15;
    private const double WeightEndingSoon = 0.10;

    public RecommendationService(
        IDbContextFactory<AppDbContext> contextFactory,
        ILogger<RecommendationService> logger)
    {
        _contextFactory = contextFactory;
        _logger = logger;
    }

    public async Task<List<MarketingOfferDto>> GetRecommendedOffersAsync(int pharmacyId, int count = 10)
    {
        var personalized = await GetPersonalizedOffersAsync(pharmacyId);
        return personalized.Take(count).ToList();
    }

    public async Task<List<MarketingOfferDto>> GetPersonalizedOffersAsync(int pharmacyId, string? deviceId = null)
    {
        await using var db = await _contextFactory.CreateDbContextAsync();
        var now = DateTime.Now;

        // 1. بناء ملف تفضيلات الصيدلية
        var profile = await BuildPharmacyProfileAsync(db, pharmacyId);

        // 2. الحصول على العروض النشطة
        var activeOffers = await db.MarketingOffers
            .Include(o => o.PharmacyTargets)
            .Include(o => o.OfferProducts).ThenInclude(p => p.Product)
            .Where(o => o.Status == MarketingOfferRules.Published
                && o.StartsAt <= now
                && o.EndsAt > now
                && (!o.RemainingQuantity.HasValue || o.RemainingQuantity.Value > 0))
            .ToListAsync();

        // 3. تصفية حسب AudienceRule
        var eligibleOffers = new List<MarketingOffer>();
        foreach (var offer in activeOffers)
        {
            if (await MarketingOfferRules.MatchesAudienceAsync(db, offer, new Pharmacy { Id = pharmacyId, Address = profile.Region ?? string.Empty }, now))
            {
                eligibleOffers.Add(offer);
            }
        }

        // 4. حساب الـ Score لكل عرض
        var scored = eligibleOffers
            .Select(offer => new
            {
                Offer = offer,
                Dto = ToDto(offer, now),
                Score = CalculateOfferScore(offer, profile, now)
            })
            .OrderByDescending(x => x.Score)
            .ToList();

        _logger.LogInformation(
            "Personalized offers for pharmacy {PharmacyId}: {Count} eligible out of {Total}",
            pharmacyId, scored.Count, activeOffers.Count);

        return scored.Select(x => x.Dto).ToList();
    }

    public async Task<List<MarketingOfferDto>> RankOffersForPharmacyAsync(
        int pharmacyId, List<MarketingOfferDto> offers)
    {
        await using var db = await _contextFactory.CreateDbContextAsync();
        var profile = await BuildPharmacyProfileAsync(db, pharmacyId);

        // تحويل الـ DTOs إلى Offers (نحتاج للكيانات الفعلية)
        var offerIds = offers.Select(o => o.Id).ToList();
        var entities = await db.MarketingOffers
            .Include(o => o.PharmacyTargets)
            .Include(o => o.OfferProducts).ThenInclude(p => p.Product)
            .Where(o => offerIds.Contains(o.Id))
            .ToListAsync();

        var now = DateTime.Now;
        var offerMap = entities.ToDictionary(o => o.Id);

        var ranked = offers
            .Select(dto =>
            {
                var entity = offerMap.GetValueOrDefault(dto.Id);
                var score = entity != null ? CalculateOfferScore(entity, profile, now) : 0;
                return new { Dto = dto, Score = score };
            })
            .OrderByDescending(x => x.Score)
            .Select(x => x.Dto)
            .ToList();

        return ranked;
    }

    public async Task<RecommendedOffersResult> GetHomePageRecommendationsAsync(int pharmacyId)
    {
        await using var db = await _contextFactory.CreateDbContextAsync();
        var now = DateTime.Now;

        var personalized = await GetPersonalizedOffersAsync(pharmacyId, null);

        // الأكثر تفاعلاً (Trending) — حسب أحداث الانطباع والنقر
        var trendingQuery = await db.OfferEvents
            .Where(e => e.EventType == "impression" && e.OccurredAt >= now.AddDays(-7))
            .GroupBy(e => e.MarketingOfferId)
            .Select(g => new { OfferId = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Take(5)
            .ToListAsync();

        var trendingIds = trendingQuery.Select(x => x.OfferId).ToList();
        var trending = new List<MarketingOfferDto>();
        if (trendingIds.Count > 0)
        {
            var trendingOffers = await db.MarketingOffers
                .Include(o => o.PharmacyTargets)
                .Include(o => o.OfferProducts).ThenInclude(p => p.Product)
                .Where(o => trendingIds.Contains(o.Id) && o.StartsAt <= now && o.EndsAt > now)
                .ToListAsync();
            trending = trendingOffers.Select(o => ToDto(o, now)).ToList();
        }

        // الأحدث (New Arrivals)
        var newArrivals = (await db.MarketingOffers
            .Include(o => o.PharmacyTargets)
            .Include(o => o.OfferProducts).ThenInclude(p => p.Product)
            .Where(o => o.Status == MarketingOfferRules.Published
                && o.StartsAt <= now
                && o.EndsAt > now
                && o.CreatedAt >= now.AddDays(-14))
            .OrderByDescending(o => o.CreatedAt)
            .Take(5)
            .ToListAsync())
            .Select(o => ToDto(o, now))
            .Where(dto => !personalized.Any(p => p.Id == dto.Id))
            .Take(3)
            .ToList();

        // تنتهي قريباً (Ending Soon)
        var endingSoon = (await db.MarketingOffers
            .Include(o => o.PharmacyTargets)
            .Include(o => o.OfferProducts).ThenInclude(p => p.Product)
            .Where(o => o.Status == MarketingOfferRules.Published
                && o.StartsAt <= now
                && o.EndsAt > now
                && o.EndsAt <= now.AddDays(3))
            .OrderBy(o => o.EndsAt)
            .Take(5)
            .ToListAsync())
            .Select(o => ToDto(o, now))
            .Where(dto => !personalized.Any(p => p.Id == dto.Id) && !trending.Any(t => t.Id == dto.Id))
            .Take(3)
            .ToList();

        return new RecommendedOffersResult
        {
            TopPicks = personalized.Take(5).ToList(),
            Trending = trending.Take(5).ToList(),
            NewArrivals = newArrivals,
            EndingSoon = endingSoon
        };
    }

    // ─── دوال المساعدة الداخلية ───────────────────────────────────

    private async Task<PharmacyPreferenceProfile> BuildPharmacyProfileAsync(AppDbContext db, int pharmacyId)
    {
        var profile = new PharmacyPreferenceProfile
        {
            PharmacyId = pharmacyId,
            Region = null
        };

        // الحصول على منطقة الصيدلية
        var pharmacy = await db.Pharmacies
            .Where(p => p.Id == pharmacyId)
            .Select(p => new { p.Address, p.Name })
            .FirstOrDefaultAsync();

        if (pharmacy != null)
        {
            // استخراج المنطقة من العنوان (آخر كلمة أو قبل رقم الهاتف)
            var address = pharmacy.Address ?? string.Empty;
            profile.Region = address;
        }

        // الحصول على فئات المنتجات التي اشترتها الصيدلية
        var categoryIds = await db.Orders
            .Where(o => o.PharmacyId == pharmacyId && o.Status == "completed")
            .Join(db.OrderItems, o => o.Id, oi => oi.OrderId, (o, oi) => oi.ProductId)
            .Join(db.Products, pi => pi, p => p.Id, (pi, p) => p.CategoryId)
            .Where(cid => cid != null)
            .Distinct()
            .Cast<int>()
            .ToListAsync();
        profile.PurchasedCategoryIds = categoryIds.ToHashSet();

        // الحصول على المنتجات التي اشترتها الصيدلية
        var productIds = await db.Orders
            .Where(o => o.PharmacyId == pharmacyId && o.Status == "completed")
            .Join(db.OrderItems, o => o.Id, oi => oi.OrderId, (o, oi) => oi.ProductId)
            .Distinct()
            .ToListAsync();
        profile.PurchasedProductIds = productIds.ToHashSet();

        // الحصول على العروض التي تفاعلت معها الصيدلية
        var offerEvents = await db.OfferEvents
            .Where(e => e.PharmacyId == pharmacyId && !e.IsRejected)
            .ToListAsync();

        profile.ViewedOfferIds = offerEvents
            .Where(e => e.EventType == "impression")
            .Select(e => e.MarketingOfferId)
            .ToHashSet();

        profile.ClickedOfferIds = offerEvents
            .Where(e => e.EventType == "click")
            .Select(e => e.MarketingOfferId)
            .ToHashSet();

        // إحصائيات الطلبات
        var orderStats = await db.Orders
            .Where(o => o.PharmacyId == pharmacyId && o.Status == "completed")
            .GroupBy(o => 1)
            .Select(g => new
            {
                Count = g.Count(),
                AvgValue = g.Average(o => o.FinalTotal)
            })
            .FirstOrDefaultAsync();

        profile.TotalOrders = orderStats?.Count ?? 0;
        profile.AverageOrderValue = orderStats?.AvgValue ?? 0;

        return profile;
    }

    private double CalculateOfferScore(
        MarketingOffer offer,
        PharmacyPreferenceProfile profile,
        DateTime now)
    {
        double score = 0;

        // 1. مطابقة الفئات (Category Match)
        if (profile.PurchasedCategoryIds.Count > 0 && offer.OfferProducts.Count > 0)
        {
            var offerCategoryIds = offer.OfferProducts
                .Where(op => op.Product?.CategoryId != null)
                .Select(op => op.Product!.CategoryId!.Value)
                .ToHashSet();

            var matchedCategories = profile.PurchasedCategoryIds.Intersect(offerCategoryIds).Count();
            if (matchedCategories > 0)
            {
                score += WeightCategoryMatch * Math.Min(1.0, matchedCategories / 3.0);
            }
        }

        // 2. المنتجات المطابقة (المشتريات السابقة)
        if (profile.PurchasedProductIds.Count > 0 && offer.OfferProducts.Count > 0)
        {
            var offerProductIds = offer.OfferProducts.Select(op => op.ProductId).ToHashSet();
            var matchedProducts = profile.PurchasedProductIds.Intersect(offerProductIds).Count();
            if (matchedProducts > 0)
            {
                score += WeightRecentOrders * Math.Min(1.0, matchedProducts / 2.0);
            }
        }

        // 3. المنطقة الجغرافية (إذا كان العرض موجهاً)
        if (!string.IsNullOrWhiteSpace(profile.Region)
            && !string.IsNullOrWhiteSpace(offer.AudienceRule)
            && offer.AudienceRule.StartsWith("geo:", StringComparison.OrdinalIgnoreCase))
        {
            var offerRegion = offer.AudienceRule[4..].Trim();
            if (profile.Region.Contains(offerRegion, StringComparison.OrdinalIgnoreCase))
            {
                score += WeightRegionMatch;
            }
        }

        // 4. التفاعل السابق (Engagement)
        if (profile.ClickedOfferIds.Contains(offer.Id))
        {
            score += WeightEngagement * 1.2; // النقر يعطي وزن أكبر
        }
        else if (profile.ViewedOfferIds.Contains(offer.Id))
        {
            score += WeightEngagement * 0.5; // مجرد مشاهدة
        }

        // 5. العروض التي تنتهي قريباً تحصل على دفعة
        if (offer.EndsAt <= now.AddDays(3))
        {
            score += WeightEndingSoon * 1.0;
        }
        else if (offer.EndsAt <= now.AddDays(7))
        {
            score += WeightEndingSoon * 0.5;
        }

        // 6. نسبة الخصم (كلما زاد الخصم، زادت الأولوية)
        if (offer.DiscountPercent.HasValue && offer.DiscountPercent.Value > 0)
        {
            score += (offer.DiscountPercent.Value / 100.0) * 0.05;
        }

        return score;
    }
}
