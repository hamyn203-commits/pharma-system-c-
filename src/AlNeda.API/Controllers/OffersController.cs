using AlNeda.API.Authorization;
using AlNeda.Core.Entities;
using AlNeda.Core.Models;
using AlNeda.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IO;

namespace AlNeda.API.Controllers;

[ApiController]
[Route("api/admin/offers")]
[Authorize(Policy = AuthPolicies.AdminOnly)]
[Tags("Admin Offers")]
public class OffersController : ControllerBase
{
    private readonly IDbContextFactory<Data.AppDbContext> _contextFactory;
    private readonly IAuditService _audit;
    private readonly IWebHostEnvironment _environment;

    public OffersController(IDbContextFactory<Data.AppDbContext> contextFactory, IAuditService audit, IWebHostEnvironment environment)
    {
        _contextFactory = contextFactory;
        _audit = audit;
        _environment = environment;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        await using var db = await _contextFactory.CreateDbContextAsync();
        var now = DateTime.Now;
        var offers = await db.MarketingOffers.Include(o => o.PharmacyTargets).OrderByDescending(o => o.UpdatedAt).ToListAsync();
        return Ok(offers.Select(o => ToDto(o, now)).ToList());
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        await using var db = await _contextFactory.CreateDbContextAsync();
        var offer = await db.MarketingOffers.Include(o => o.PharmacyTargets).FirstOrDefaultAsync(o => o.Id == id);
        if (offer == null)
            return NotFound(new ApiError { Message = "العرض غير موجود", Code = "OFFER_NOT_FOUND" });

        return Ok(ToDto(offer, DateTime.Now));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateMarketingOfferRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
            return BadRequest(new ApiError { Message = "عنوان العرض مطلوب", Code = "VALIDATION_ERROR" });
        if (request.EndsAt <= request.StartsAt)
            return BadRequest(new ApiError { Message = "تاريخ نهاية العرض يجب أن يكون بعد تاريخ البداية", Code = "VALIDATION_ERROR" });

        await using var db = await _contextFactory.CreateDbContextAsync();
        var now = DateTime.Now;
        var offer = new MarketingOffer
        {
            Title = request.Title.Trim(),
            Subtitle = request.Subtitle.Trim(),
            Description = request.Description,
            ImageUrl = request.ImageUrl.Trim(),
            OfferType = NormalizeOfferType(request.OfferType),
            AudienceRule = NormalizeAudienceRule(request.AudienceRule),
            StartsAt = request.StartsAt,
            EndsAt = request.EndsAt,
            Status = "draft",
            QuantityLimit = request.QuantityLimit,
            RemainingQuantity = request.RemainingQuantity ?? request.QuantityLimit,
            OldPrice = request.OldPrice,
            NewPrice = request.NewPrice,
            DiscountPercent = request.DiscountPercent,
            CreatedAt = now,
            UpdatedAt = now
        };

        db.MarketingOffers.Add(offer);
        await db.SaveChangesAsync();
        await SyncTargetsAsync(db, offer.Id, offer.AudienceRule, request.TargetPharmacyIds);
        await db.SaveChangesAsync();
        await db.Entry(offer).Collection(o => o.PharmacyTargets).LoadAsync();

        await _audit.LogAsync(User.Identity?.Name ?? "system", "create", "MarketingOffer", offer.Id.ToString(),
            $"Created marketing offer '{offer.Title}'");

        return Ok(ToDto(offer, now));
    }

    [HttpPatch("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateMarketingOfferRequest request)
    {
        await using var db = await _contextFactory.CreateDbContextAsync();
        var offer = await db.MarketingOffers.Include(o => o.PharmacyTargets).FirstOrDefaultAsync(o => o.Id == id);
        if (offer == null)
            return NotFound(new ApiError { Message = "العرض غير موجود", Code = "OFFER_NOT_FOUND" });
        if (request.EndsAt <= request.StartsAt)
            return BadRequest(new ApiError { Message = "تاريخ نهاية العرض يجب أن يكون بعد تاريخ البداية", Code = "VALIDATION_ERROR" });

        var oldDates = $"{offer.StartsAt:s}..{offer.EndsAt:s}";
        offer.Title = request.Title.Trim();
        offer.Subtitle = request.Subtitle.Trim();
        offer.Description = request.Description;
        offer.ImageUrl = request.ImageUrl.Trim();
        offer.OfferType = NormalizeOfferType(request.OfferType);
        offer.AudienceRule = NormalizeAudienceRule(request.AudienceRule);
        offer.StartsAt = request.StartsAt;
        offer.EndsAt = request.EndsAt;
        offer.Status = NormalizeStatus(request.Status);
        offer.QuantityLimit = request.QuantityLimit;
        offer.RemainingQuantity = request.RemainingQuantity ?? request.QuantityLimit;
        offer.OldPrice = request.OldPrice;
        offer.NewPrice = request.NewPrice;
        offer.DiscountPercent = request.DiscountPercent;
        offer.UpdatedAt = DateTime.Now;

        if (offer.RemainingQuantity == 0)
            offer.Status = MarketingOfferRules.Depleted;

        await SyncTargetsAsync(db, offer.Id, offer.AudienceRule, request.TargetPharmacyIds);
        await db.SaveChangesAsync();
        await db.Entry(offer).Collection(o => o.PharmacyTargets).LoadAsync();

        await _audit.LogAsync(User.Identity?.Name ?? "system", "update", "MarketingOffer", offer.Id.ToString(),
            $"Updated marketing offer '{offer.Title}', dates {oldDates} -> {offer.StartsAt:s}..{offer.EndsAt:s}");

        return Ok(ToDto(offer, DateTime.Now));
    }

    [HttpPost("{id:int}/publish")]
    public async Task<IActionResult> Publish(int id)
    {
        return await ChangeStatus(id, MarketingOfferRules.Published, "publish");
    }

    [HttpPost("{id:int}/pause")]
    public async Task<IActionResult> Pause(int id)
    {
        return await ChangeStatus(id, MarketingOfferRules.Paused, "pause");
    }

    [HttpPost("{id:int}/cancel")]
    public async Task<IActionResult> Cancel(int id)
    {
        return await ChangeStatus(id, MarketingOfferRules.Cancelled, "cancel");
    }

    [HttpPost("upload-image")]
    [RequestSizeLimit(10_000_000)]
    public async Task<IActionResult> UploadImage(IFormFile file)
    {
        if (file.Length == 0)
            return BadRequest(new ApiError { Message = "صورة العرض مطلوبة", Code = "VALIDATION_ERROR" });

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (extension is not ".png" and not ".jpg" and not ".jpeg" and not ".webp")
            return BadRequest(new ApiError { Message = "صيغة الصورة غير مدعومة", Code = "VALIDATION_ERROR" });

        var webRoot = _environment.WebRootPath;
        if (string.IsNullOrWhiteSpace(webRoot))
            webRoot = Path.Combine(AppContext.BaseDirectory, "wwwroot");

        var uploadDir = Path.Combine(webRoot, "uploads", "offers");
        Directory.CreateDirectory(uploadDir);

        var fileName = $"{Guid.NewGuid():N}{extension}";
        var fullPath = Path.Combine(uploadDir, fileName);
        await using (var stream = System.IO.File.Create(fullPath))
            await file.CopyToAsync(stream);

        return Ok(new OfferImageUploadResponse { ImageUrl = $"/uploads/offers/{fileName}" });
    }

    [HttpGet("analytics")]
    public async Task<IActionResult> Analytics()
    {
        await using var db = await _contextFactory.CreateDbContextAsync();
        var now = DateTime.Now;
        var offers = await db.MarketingOffers.OrderByDescending(o => o.UpdatedAt).ToListAsync();
        var offerIds = offers.Select(o => o.Id).ToList();
        var events = await db.OfferEvents.Where(e => offerIds.Contains(e.MarketingOfferId) && !e.IsRejected).ToListAsync();
        var orders = await db.Orders.Where(o => o.SourceOfferId != null && offerIds.Contains(o.SourceOfferId.Value)).ToListAsync();

        var rows = offers.Select(o =>
        {
            var offerEvents = events.Where(e => e.MarketingOfferId == o.Id).ToList();
            var uniqueViewed = offerEvents.Where(e => e.IsUniqueDailyImpression).Select(e => e.PharmacyId).Distinct().Count();
            var uniqueDaily = offerEvents.Count(e => e.IsUniqueDailyImpression);
            var clicks = offerEvents.Count(e => e.EventType == "click");
            var offerOrders = orders.Count(ord => ord.SourceOfferId == o.Id);
            return new OfferAnalyticsDto
            {
                OfferId = o.Id,
                Title = o.Title,
                Status = o.Status,
                UniqueViewedPharmacies = uniqueViewed,
                UniqueDailyImpressions = uniqueDaily,
                Clicks = clicks,
                Orders = offerOrders,
                ClickRate = uniqueDaily == 0 ? 0 : (double)clicks / uniqueDaily,
                ConversionRate = uniqueViewed == 0 ? 0 : (double)offerOrders / uniqueViewed,
                HasDeviceFallbackEvents = offerEvents.Any(e => e.UsedDeviceFallback)
            };
        }).ToList();

        var totalUniqueViewed = events.Where(e => e.IsUniqueDailyImpression).Select(e => e.PharmacyId).Distinct().Count();
        return Ok(new OffersAnalyticsSummaryDto
        {
            ActiveOffers = offers.Count(o => MarketingOfferRules.IsActive(o, now)),
            PublishedOffers = offers.Count(o => o.Status == MarketingOfferRules.Published),
            UniqueViewedPharmacies = totalUniqueViewed,
            UniqueDailyImpressions = events.Count(e => e.IsUniqueDailyImpression),
            Clicks = events.Count(e => e.EventType == "click"),
            OrdersFromOffers = orders.Count,
            ConversionRate = totalUniqueViewed == 0 ? 0 : (double)orders.Count / totalUniqueViewed,
            HasDeviceFallbackEvents = events.Any(e => e.UsedDeviceFallback),
            Offers = rows
        });
    }

    private async Task<IActionResult> ChangeStatus(int id, string status, string action)
    {
        await using var db = await _contextFactory.CreateDbContextAsync();
        var offer = await db.MarketingOffers.Include(o => o.PharmacyTargets).FirstOrDefaultAsync(o => o.Id == id);
        if (offer == null)
            return NotFound(new ApiError { Message = "العرض غير موجود", Code = "OFFER_NOT_FOUND" });

        offer.Status = status;
        offer.UpdatedAt = DateTime.Now;
        await db.SaveChangesAsync();

        await _audit.LogAsync(User.Identity?.Name ?? "system", action, "MarketingOffer", offer.Id.ToString(),
            $"Changed offer '{offer.Title}' status to {status}");

        return Ok(ToDto(offer, DateTime.Now));
    }

    private static MarketingOfferDto ToDto(MarketingOffer offer, DateTime now) => new()
    {
        Id = offer.Id,
        Title = offer.Title,
        Subtitle = offer.Subtitle,
        Description = offer.Description,
        ImageUrl = offer.ImageUrl,
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
        IsActive = MarketingOfferRules.IsActive(offer, now),
        TargetPharmacyIds = offer.PharmacyTargets.Select(t => t.PharmacyId).ToList(),
        AudienceLabel = offer.AudienceRule == "selected_pharmacies"
            ? $"صيدليات محددة ({offer.PharmacyTargets.Count})"
            : "كل الصيدليات"
    };

    private static string NormalizeOfferType(string value) =>
        string.IsNullOrWhiteSpace(value) ? "discount" : value.Trim().ToLowerInvariant();

    private static string NormalizeAudienceRule(string value)
    {
        var rule = string.IsNullOrWhiteSpace(value) ? "all" : value.Trim();
        return rule is "all" or "selected_pharmacies" or "active_pharmacies_last_30d" || rule.StartsWith("geo:", StringComparison.OrdinalIgnoreCase)
            ? rule
            : "all";
    }

    private static async Task SyncTargetsAsync(Data.AppDbContext db, int offerId, string audienceRule, List<int> targetPharmacyIds)
    {
        var oldTargets = await db.MarketingOfferPharmacyTargets.Where(t => t.MarketingOfferId == offerId).ToListAsync();
        db.MarketingOfferPharmacyTargets.RemoveRange(oldTargets);

        if (audienceRule != "selected_pharmacies")
            return;

        var ids = targetPharmacyIds.Distinct().ToList();
        var existingPharmacyIds = await db.Pharmacies.Where(p => ids.Contains(p.Id)).Select(p => p.Id).ToListAsync();
        foreach (var pharmacyId in existingPharmacyIds)
        {
            db.MarketingOfferPharmacyTargets.Add(new MarketingOfferPharmacyTarget
            {
                MarketingOfferId = offerId,
                PharmacyId = pharmacyId
            });
        }
    }

    private static string NormalizeStatus(string value)
    {
        var status = string.IsNullOrWhiteSpace(value) ? "draft" : value.Trim().ToLowerInvariant();
        return status is "draft" or "scheduled" or "published" or "paused" or "ended" or "cancelled" or "depleted"
            ? status
            : "draft";
    }
}
