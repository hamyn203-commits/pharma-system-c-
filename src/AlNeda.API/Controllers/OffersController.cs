using AlNeda.API.Authorization;
using AlNeda.Core.Entities;
using AlNeda.Core.Models;
using AlNeda.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IO;
using static AlNeda.Core.Models.MarketingOfferMapper;

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
        var offers = await db.MarketingOffers
            .Include(o => o.PharmacyTargets)
            .Include(o => o.OfferProducts).ThenInclude(p => p.Product)
            .OrderByDescending(o => o.UpdatedAt)
            .ToListAsync();
        return Ok(offers.Select(o => ToDto(o, now)).ToList());
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        await using var db = await _contextFactory.CreateDbContextAsync();
        var offer = await db.MarketingOffers
            .Include(o => o.PharmacyTargets)
            .Include(o => o.OfferProducts).ThenInclude(p => p.Product)
            .FirstOrDefaultAsync(o => o.Id == id);
        if (offer == null)
            return NotFound(new ApiError { Message = "العرض غير موجود", Code = "OFFER_NOT_FOUND" });

        return Ok(ToDto(offer, DateTime.Now));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateMarketingOfferRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
            return BadRequest(new ApiError { Message = "عنوان العرض مطلوب", Code = "VALIDATION_ERROR" });
        var scheduleError = MarketingOfferRules.ValidateSchedule(request.StartsAt, request.EndsAt);
        if (!string.IsNullOrEmpty(scheduleError))
            return BadRequest(new ApiError { Message = scheduleError, Code = "VALIDATION_ERROR" });

        var priceError = MarketingOfferRules.ValidatePrices(request.OldPrice, request.NewPrice, request.DiscountPercent);
        if (!string.IsNullOrEmpty(priceError))
            return BadRequest(new ApiError { Message = priceError, Code = "VALIDATION_ERROR" });

        var quantityError = MarketingOfferRules.ValidateQuantity(request.QuantityLimit, request.RemainingQuantity ?? request.QuantityLimit);
        if (!string.IsNullOrEmpty(quantityError))
            return BadRequest(new ApiError { Message = quantityError, Code = "VALIDATION_ERROR" });

        await using var db = await _contextFactory.CreateDbContextAsync();
        var now = DateTime.Now;
        var offer = new MarketingOffer
        {
            Title = request.Title.Trim(),
            Subtitle = request.Subtitle.Trim(),
            Description = request.Description,
            ImageUrl = PrimaryImageUrl(request),
            AdditionalImageUrls = AdditionalImageUrls(request),
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
        await SyncProductsAsync(db, offer.Id, request.OfferProducts);
        await db.SaveChangesAsync();
        await db.Entry(offer).Collection(o => o.PharmacyTargets).LoadAsync();
        await db.Entry(offer).Collection(o => o.OfferProducts).Query().Include(p => p.Product).LoadAsync();

        await _audit.LogAsync(User.Identity?.Name ?? "system", "create", "MarketingOffer", offer.Id.ToString(),
            $"Created marketing offer '{offer.Title}'");

        return Ok(ToDto(offer, now));
    }

    [HttpPatch("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateMarketingOfferRequest request)
    {
        await using var db = await _contextFactory.CreateDbContextAsync();
        var offer = await db.MarketingOffers
            .Include(o => o.PharmacyTargets)
            .Include(o => o.OfferProducts).ThenInclude(p => p.Product)
            .FirstOrDefaultAsync(o => o.Id == id);
        if (offer == null)
            return NotFound(new ApiError { Message = "العرض غير موجود", Code = "OFFER_NOT_FOUND" });
        var scheduleError = MarketingOfferRules.ValidateSchedule(request.StartsAt, request.EndsAt);
        if (!string.IsNullOrEmpty(scheduleError))
            return BadRequest(new ApiError { Message = scheduleError, Code = "VALIDATION_ERROR" });

        var priceError = MarketingOfferRules.ValidatePrices(request.OldPrice, request.NewPrice, request.DiscountPercent);
        if (!string.IsNullOrEmpty(priceError))
            return BadRequest(new ApiError { Message = priceError, Code = "VALIDATION_ERROR" });

        var quantityError = MarketingOfferRules.ValidateQuantity(request.QuantityLimit, request.RemainingQuantity ?? request.QuantityLimit);
        if (!string.IsNullOrEmpty(quantityError))
            return BadRequest(new ApiError { Message = quantityError, Code = "VALIDATION_ERROR" });

        var oldDates = $"{offer.StartsAt:s}..{offer.EndsAt:s}";
        offer.Title = request.Title.Trim();
        offer.Subtitle = request.Subtitle.Trim();
        offer.Description = request.Description;
        offer.ImageUrl = PrimaryImageUrl(request);
        offer.AdditionalImageUrls = AdditionalImageUrls(request);
        offer.OfferType = NormalizeOfferType(request.OfferType);
        offer.AudienceRule = NormalizeAudienceRule(request.AudienceRule);
        offer.StartsAt = request.StartsAt;
        offer.EndsAt = request.EndsAt;
        offer.Status = MarketingOfferRules.NextStatusAfterQuantity(request.Status, request.RemainingQuantity ?? request.QuantityLimit);
        offer.QuantityLimit = request.QuantityLimit;
        offer.RemainingQuantity = request.RemainingQuantity ?? request.QuantityLimit;
        offer.OldPrice = request.OldPrice;
        offer.NewPrice = request.NewPrice;
        offer.DiscountPercent = request.DiscountPercent;
        offer.UpdatedAt = DateTime.Now;

        await SyncTargetsAsync(db, offer.Id, offer.AudienceRule, request.TargetPharmacyIds);
        await SyncProductsAsync(db, offer.Id, request.OfferProducts);
        await db.SaveChangesAsync();
        await db.Entry(offer).Collection(o => o.PharmacyTargets).LoadAsync();
        await db.Entry(offer).Collection(o => o.OfferProducts).Query().Include(p => p.Product).LoadAsync();

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

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await using var db = await _contextFactory.CreateDbContextAsync();
        var offer = await db.MarketingOffers.FirstOrDefaultAsync(o => o.Id == id);
        if (offer == null)
            return NotFound(new ApiError { Message = "العرض غير موجود", Code = "OFFER_NOT_FOUND" });

        var title = offer.Title;
        db.MarketingOffers.Remove(offer);
        await db.SaveChangesAsync();

        await _audit.LogAsync(User.Identity?.Name ?? "system", "delete", "MarketingOffer", id.ToString(),
            $"Deleted marketing offer '{title}'");

        return NoContent();
    }

    [HttpPost("{id:int}/test-send")]
    public async Task<IActionResult> TestSend(int id, [FromBody] OfferTestSendRequest request)
    {
        if (request.PharmacyId <= 0)
            return BadRequest(new ApiError { Message = "اختار صيدلية لإرسال التجربة", Code = "VALIDATION_ERROR" });

        await using var db = await _contextFactory.CreateDbContextAsync();
        var now = DateTime.Now;
        var offer = await db.MarketingOffers
            .Include(o => o.PharmacyTargets)
            .Include(o => o.OfferProducts).ThenInclude(p => p.Product)
            .FirstOrDefaultAsync(o => o.Id == id);
        if (offer == null)
            return NotFound(new ApiError { Message = "العرض غير موجود", Code = "OFFER_NOT_FOUND" });

        var pharmacy = await db.Pharmacies.FirstOrDefaultAsync(p => p.Id == request.PharmacyId);
        if (pharmacy == null)
            return NotFound(new ApiError { Message = "الصيدلية غير موجودة", Code = "PHARMACY_NOT_FOUND" });

        var wouldAppear = MarketingOfferRules.IsActive(offer, now)
            && await MarketingOfferRules.MatchesAudienceAsync(db, offer, pharmacy, now);
        var deviceKey = MarketingOfferRules.DeviceKey(request.DeviceId);
        db.OfferEvents.Add(new OfferEvent
        {
            MarketingOfferId = offer.Id,
            PharmacyId = pharmacy.Id,
            DeviceId = string.IsNullOrWhiteSpace(request.DeviceId) ? null : request.DeviceId.Trim(),
            DeviceKey = deviceKey,
            EventType = "test_send",
            OccurredAt = now,
            EventDateKey = MarketingOfferRules.EventDateKey(now),
            IsUniqueDailyImpression = false,
            UsedDeviceFallback = MarketingOfferRules.UsesDeviceFallback(request.DeviceId),
            IsRejected = false,
            RejectionReason = wouldAppear ? string.Empty : "test_only_not_currently_visible"
        });
        await db.SaveChangesAsync();

        await _audit.LogAsync(User.Identity?.Name ?? "system", "test-send", "MarketingOffer", offer.Id.ToString(),
            $"Prepared test send for offer '{offer.Title}' to pharmacy '{pharmacy.Name}'");

        return Ok(new OfferTestSendResponse
        {
            Sent = true,
            WouldAppearOnMobile = wouldAppear,
            OfferId = offer.Id,
            OfferTitle = offer.Title,
            PharmacyId = pharmacy.Id,
            PharmacyName = pharmacy.Name,
            Message = wouldAppear
                ? $"تم تجهيز تجربة العرض لصيدلية {pharmacy.Name} وسيظهر لها على تطبيق الصيدلي."
                : $"تم تجهيز التجربة، لكن العرض لن يظهر حالياً لصيدلية {pharmacy.Name} بسبب الحالة أو الاستهداف أو مدة العرض."
        });
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
        var offer = await db.MarketingOffers
            .Include(o => o.PharmacyTargets)
            .Include(o => o.OfferProducts).ThenInclude(p => p.Product)
            .FirstOrDefaultAsync(o => o.Id == id);
        if (offer == null)
            return NotFound(new ApiError { Message = "العرض غير موجود", Code = "OFFER_NOT_FOUND" });

        offer.Status = status;
        offer.UpdatedAt = DateTime.Now;
        await db.SaveChangesAsync();

        await _audit.LogAsync(User.Identity?.Name ?? "system", action, "MarketingOffer", offer.Id.ToString(),
            $"Changed offer '{offer.Title}' status to {status}");

        return Ok(ToDto(offer, DateTime.Now));
    }

    private static string NormalizeOfferType(string value) => MarketingOfferRules.NormalizeOfferType(value);

    private static string NormalizeAudienceRule(string value) => MarketingOfferRules.NormalizeAudienceRule(value);

    private static string PrimaryImageUrl(CreateMarketingOfferRequest request)
    {
        var firstFromList = request.ImageUrls.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x));
        return (firstFromList ?? request.ImageUrl).Trim();
    }

    private static string AdditionalImageUrls(CreateMarketingOfferRequest request)
    {
        var primary = PrimaryImageUrl(request);
        var images = request.ImageUrls
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .Where(x => !string.Equals(x, primary, StringComparison.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase);

        return string.Join('\n', images);
    }

    public static IEnumerable<string> OfferImageUrls(MarketingOffer offer)
    {
        if (!string.IsNullOrWhiteSpace(offer.ImageUrl))
            yield return offer.ImageUrl;

        foreach (var image in (offer.AdditionalImageUrls ?? string.Empty).Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (!string.Equals(image, offer.ImageUrl, StringComparison.OrdinalIgnoreCase))
                yield return image;
        }
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

    private static async Task SyncProductsAsync(Data.AppDbContext db, int offerId, List<MarketingOfferProductRequest> requestProducts)
    {
        var oldProducts = await db.MarketingOfferProducts.Where(p => p.MarketingOfferId == offerId).ToListAsync();
        db.MarketingOfferProducts.RemoveRange(oldProducts);

        var rows = requestProducts
            .Where(p => p.ProductId > 0)
            .GroupBy(p => p.ProductId)
            .Select(g => g.First())
            .ToList();

        if (rows.Count == 0)
            return;

        var ids = rows.Select(p => p.ProductId).ToList();
        var products = await db.Products
            .Where(p => ids.Contains(p.Id))
            .Select(p => new { p.Id, p.UnitPrice })
            .ToDictionaryAsync(p => p.Id);

        foreach (var row in rows)
        {
            if (!products.TryGetValue(row.ProductId, out var product))
                continue;

            var oldPrice = row.OldPrice > 0 ? row.OldPrice : product.UnitPrice;
            var newPrice = row.NewPrice > 0 ? row.NewPrice : oldPrice;
            db.MarketingOfferProducts.Add(new MarketingOfferProduct
            {
                MarketingOfferId = offerId,
                ProductId = row.ProductId,
                OfferQuantity = Math.Max(1, row.OfferQuantity),
                MinimumOrder = Math.Max(1, row.MinimumOrder),
                OldPrice = oldPrice,
                NewPrice = newPrice,
                GiftProduct = (row.GiftProduct ?? string.Empty).Trim()
            });
        }
    }

    private static string NormalizeStatus(string value) => MarketingOfferRules.NormalizeStatus(value);
}
