using AlNeda.Core.Models;
using AlNeda.Data;
using AlNeda.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using static AlNeda.Core.Models.MarketingOfferMapper;

namespace AlNeda.API.Controllers;

/// <summary>
/// 📱 التحكم بمسح الباركود للبحث عن العروض — منفصل تماماً
/// </summary>
[ApiController]
[Route("api/mobile/offers")]
[Authorize(Roles = "pharmacy")]
[Tags("Mobile Barcode Offers")]
public class BarcodeOffersController : ControllerBase
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;
    private readonly IQrCodeService _qrCodeService;
    private readonly ILogger<BarcodeOffersController> _logger;

    public BarcodeOffersController(
        IDbContextFactory<AppDbContext> contextFactory,
        IQrCodeService qrCodeService,
        ILogger<BarcodeOffersController> logger)
    {
        _contextFactory = contextFactory;
        _qrCodeService = qrCodeService;
        _logger = logger;
    }

    /// <summary>
    /// 🔍 البحث عن العروض النشطة عبر باركود المنتج
    /// </summary>
    [HttpGet("barcode/{barcode}")]
    [ProducesResponseType(typeof(BarcodeSearchResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> SearchByBarcode(string barcode)
    {
        if (string.IsNullOrWhiteSpace(barcode))
            return BadRequest(new ApiError { Message = "الباركود مطلوب", Code = "VALIDATION_ERROR" });

        await using var db = await _contextFactory.CreateDbContextAsync();
        var now = DateTime.Now;

        // البحث عن المنتج بالباركود
        var product = await db.Products
            .Include(p => p.CategoryObj)
            .FirstOrDefaultAsync(p => p.Barcode == barcode.Trim() && p.IsActive == 1);

        if (product == null)
        {
            return Ok(new BarcodeSearchResult
            {
                Found = false,
                Barcode = barcode,
                Message = "لم يتم العثور على منتج بهذا الباركود"
            });
        }

        // البحث عن العروض النشطة التي تحتوي على هذا المنتج
        var activeOffers = await db.MarketingOffers
            .Include(o => o.OfferProducts).ThenInclude(p => p.Product)
            .Include(o => o.PharmacyTargets)
            .Where(o => o.Status == MarketingOfferRules.Published
                && o.StartsAt <= now
                && o.EndsAt > now
                && (!o.RemainingQuantity.HasValue || o.RemainingQuantity.Value > 0)
                && o.OfferProducts.Any(op => op.ProductId == product.Id))
            .OrderByDescending(o => o.DiscountPercent)
            .ThenBy(o => o.EndsAt)
            .ToListAsync();

        var offers = activeOffers
            .Select(o => ToDto(o, now))
            .ToList();

        // إضافة QR code link للعرض الأول
        string? qrCodeUrl = null;
        string? deepLink = null;
        if (offers.Count > 0)
        {
            qrCodeUrl = await _qrCodeService.GenerateOfferQrCodeUrlAsync(offers[0].Id);
            deepLink = _qrCodeService.GenerateOfferDeepLink(offers[0].Id);
        }

        _logger.LogInformation(
            "Barcode search '{Barcode}': found product #{ProductId} '{ProductName}' with {OfferCount} offers",
            barcode, product.Id, product.Name, offers.Count);

        return Ok(new BarcodeSearchResult
        {
            Found = true,
            Barcode = barcode,
            ProductId = product.Id,
            ProductName = product.Name,
            ProductPrice = (double)product.UnitPrice,
            ProductCategory = product.CategoryObj?.Name,
            ProductImageUrl = product.ImageUrl,
            AvailableQuantity = product.Quantity,
            Offers = offers,
            OfferCount = offers.Count,
            QrCodeUrl = qrCodeUrl,
            DeepLink = deepLink,
            Message = offers.Count > 0
                ? $"تم العثور على {offers.Count} عرض لهذا المنتج"
                : "المنتج متوفر لكن لا توجد عروض نشطة حالياً"
        });
    }

    /// <summary>
    /// 📷 مسح QR Code للعرض
    /// </summary>
    [HttpGet("qr/{scannedData}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(QrScanResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> ScanQrCode(string scannedData)
    {
        if (string.IsNullOrWhiteSpace(scannedData))
            return BadRequest(new ApiError { Message = "البيانات الممسوحة مطلوبة", Code = "VALIDATION_ERROR" });

        var validation = await _qrCodeService.ValidateQrScanAsync(scannedData);

        if (!validation.IsValid || !validation.OfferId.HasValue)
        {
            return Ok(new QrScanResult
            {
                IsValid = false,
                Message = "رمز QR غير صالح"
            });
        }

        await using var db = await _contextFactory.CreateDbContextAsync();
        var now = DateTime.Now;

        var offer = await db.MarketingOffers
            .Include(o => o.OfferProducts).ThenInclude(p => p.Product)
            .Include(o => o.PharmacyTargets)
            .FirstOrDefaultAsync(o => o.Id == validation.OfferId.Value);

        if (offer == null)
        {
            return Ok(new QrScanResult
            {
                IsValid = false,
                OfferId = validation.OfferId,
                Message = "العرض غير موجود"
            });
        }

        var dto = ToDto(offer, now);
        var shareText = _qrCodeService.GenerateShareText(offer, scannedData);

        _logger.LogInformation("QR scan for offer #{OfferId}: '{Title}'", offer.Id, offer.Title);

        return Ok(new QrScanResult
        {
            IsValid = true,
            OfferId = offer.Id,
            Offer = dto,
            CouponCode = validation.CouponCode,
            ShareText = shareText,
            Message = $"تم التعرف على العرض: {offer.Title}"
        });
    }

    /// <summary>
    /// 🔗 الحصول على رابط مشاركة العرض
    /// </summary>
    [HttpGet("{offerId:int}/share")]
    [AllowAnonymous]
    public async Task<IActionResult> GetShareLink(int offerId, [FromQuery] string? couponCode = null)
    {
        await using var db = await _contextFactory.CreateDbContextAsync();
        var offer = await db.MarketingOffers.FindAsync(offerId);

        if (offer == null)
            return NotFound(new ApiError { Message = "العرض غير موجود", Code = "OFFER_NOT_FOUND" });

        var deepLink = _qrCodeService.GenerateOfferDeepLink(offerId, couponCode);
        var qrCodeUrl = await _qrCodeService.GenerateOfferQrCodeUrlAsync(offerId);
        var shareText = _qrCodeService.GenerateShareText(offer, deepLink);

        return Ok(new OfferShareResult
        {
            OfferId = offerId,
            OfferTitle = offer.Title,
            DeepLink = deepLink,
            QrCodeUrl = qrCodeUrl,
            ShareText = shareText
        });
    }
}

// ─── نماذج الاستجابة ──────────────────────────────────────────────

public class BarcodeSearchResult
{
    public bool Found { get; set; }
    public string Barcode { get; set; } = string.Empty;
    public int? ProductId { get; set; }
    public string? ProductName { get; set; }
    public double? ProductPrice { get; set; }
    public string? ProductCategory { get; set; }
    public string? ProductImageUrl { get; set; }
    public int? AvailableQuantity { get; set; }
    public List<MarketingOfferDto> Offers { get; set; } = [];
    public int OfferCount { get; set; }
    public string? QrCodeUrl { get; set; }
    public string? DeepLink { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class QrScanResult
{
    public bool IsValid { get; set; }
    public int? OfferId { get; set; }
    public MarketingOfferDto? Offer { get; set; }
    public string? CouponCode { get; set; }
    public string? ShareText { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class OfferShareResult
{
    public int OfferId { get; set; }
    public string OfferTitle { get; set; } = string.Empty;
    public string DeepLink { get; set; } = string.Empty;
    public string QrCodeUrl { get; set; } = string.Empty;
    public string ShareText { get; set; } = string.Empty;
}
