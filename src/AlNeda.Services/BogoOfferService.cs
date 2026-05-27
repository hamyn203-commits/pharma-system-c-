using AlNeda.Core.Entities;
using AlNeda.Core.Models;
using AlNeda.Data;
using AlNeda.DomainLogic;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AlNeda.Services;

/// <summary>
/// 🛒 خدمة عروض "اشتري X واحصل على Y مجاناً" (BOGO) — منفصلة تماماً
/// </summary>
public interface IBogoOfferService
{
    /// <summary>إنشاء قاعدة BOGO لعرض موجود</summary>
    Task<BogoRuleDto> CreateRuleAsync(CreateBogoRuleRequest request);

    /// <summary>تحديث قاعدة BOGO</summary>
    Task<BogoRuleDto> UpdateRuleAsync(int id, UpdateBogoRuleRequest request);

    /// <summary>الحصول على قاعدة BOGO</summary>
    Task<BogoRuleDto?> GetRuleByIdAsync(int id);

    /// <summary>الحصول على قاعدة BOGO لعرض معين</summary>
    Task<BogoRuleDto?> GetRuleByOfferIdAsync(int offerId);

    /// <summary>تطبيق BOGO على عناصر السلة</summary>
    Task<BogoCalculationResult> ApplyBogoAsync(int offerId, List<BogoCartItem> cartItems);

    /// <summary>حذف قاعدة BOGO</summary>
    Task DeleteRuleAsync(int id);

    /// <summary>قائمة بكل قواعد BOGO</summary>
    Task<List<BogoRuleDto>> GetAllRulesAsync(int page = 1, int pageSize = 20);
}

/// <summary>
/// عنصر في السلة لحساب BOGO
/// </summary>
public class BogoCartItem
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
}

public class BogoOfferService : IBogoOfferService
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;
    private readonly ILogger<BogoOfferService> _logger;

    public BogoOfferService(
        IDbContextFactory<AppDbContext> contextFactory,
        ILogger<BogoOfferService> logger)
    {
        _contextFactory = contextFactory;
        _logger = logger;
    }

    public async Task<BogoRuleDto> CreateRuleAsync(CreateBogoRuleRequest request)
    {
        await using var db = await _contextFactory.CreateDbContextAsync();

        // التحقق من وجود العرض
        var offer = await db.MarketingOffers.FindAsync(request.MarketingOfferId);
        if (offer == null)
            throw new InvalidOperationException($"العرض {request.MarketingOfferId} غير موجود");

        // التحقق من عدم وجود قاعدة مكررة
        var existing = await db.BogoOfferRules.AnyAsync(b => b.MarketingOfferId == request.MarketingOfferId);
        if (existing)
            throw new InvalidOperationException("هذا العرض لديه قاعدة BOGO بالفعل");

        // التحقق من صحة المدخلات
        if (request.BuyQuantity < 1)
            throw new InvalidOperationException("عدد القطع المشتراة يجب أن يكون 1 على الأقل");
        if (request.FreeQuantity < 1)
            throw new InvalidOperationException("عدد القطع المجانية يجب أن يكون 1 على الأقل");

        var rule = new BogoOfferRule
        {
            MarketingOfferId = request.MarketingOfferId,
            BuyQuantity = request.BuyQuantity,
            FreeQuantity = request.FreeQuantity,
            ApplyToCheapestItem = request.ApplyToCheapestItem,
            TargetProductId = request.TargetProductId,
            MaxApplicationsPerOrder = request.MaxApplicationsPerOrder,
            DisplayLabel = string.IsNullOrWhiteSpace(request.DisplayLabel)
                ? $"اشتر {request.BuyQuantity} واحصل على {request.FreeQuantity} مجاناً"
                : request.DisplayLabel,
            CreatedAt = DateTime.Now
        };

        db.BogoOfferRules.Add(rule);
        await db.SaveChangesAsync();

        _logger.LogInformation(
            "Created BOGO rule: Buy {Buy} Get {Free} Free for offer {OfferId}",
            request.BuyQuantity, request.FreeQuantity, request.MarketingOfferId);

        return await ToDtoAsync(db, rule);
    }

    public async Task<BogoRuleDto> UpdateRuleAsync(int id, UpdateBogoRuleRequest request)
    {
        await using var db = await _contextFactory.CreateDbContextAsync();
        var rule = await db.BogoOfferRules.FindAsync(id);
        if (rule == null)
            throw new InvalidOperationException($"قاعدة BOGO {id} غير موجودة");

        if (request.BuyQuantity.HasValue) rule.BuyQuantity = request.BuyQuantity.Value;
        if (request.FreeQuantity.HasValue) rule.FreeQuantity = request.FreeQuantity.Value;
        if (request.ApplyToCheapestItem.HasValue) rule.ApplyToCheapestItem = request.ApplyToCheapestItem.Value;
        if (request.TargetProductId.HasValue) rule.TargetProductId = request.TargetProductId;
        if (request.MaxApplicationsPerOrder.HasValue) rule.MaxApplicationsPerOrder = request.MaxApplicationsPerOrder.Value;
        if (request.DisplayLabel != null) rule.DisplayLabel = request.DisplayLabel;

        if (string.IsNullOrWhiteSpace(rule.DisplayLabel))
            rule.DisplayLabel = $"اشتر {rule.BuyQuantity} واحصل على {rule.FreeQuantity} مجاناً";

        await db.SaveChangesAsync();

        _logger.LogInformation("Updated BOGO rule {Id}", id);
        return await ToDtoAsync(db, rule);
    }

    public async Task<BogoRuleDto?> GetRuleByIdAsync(int id)
    {
        await using var db = await _contextFactory.CreateDbContextAsync();
        var rule = await db.BogoOfferRules
            .Include(b => b.Offer)
            .Include(b => b.TargetProduct)
            .FirstOrDefaultAsync(b => b.Id == id);

        return rule == null ? null : ToDto(rule);
    }

    public async Task<BogoRuleDto?> GetRuleByOfferIdAsync(int offerId)
    {
        await using var db = await _contextFactory.CreateDbContextAsync();
        var rule = await db.BogoOfferRules
            .Include(b => b.Offer)
            .Include(b => b.TargetProduct)
            .FirstOrDefaultAsync(b => b.MarketingOfferId == offerId);

        return rule == null ? null : ToDto(rule);
    }

    public async Task<BogoCalculationResult> ApplyBogoAsync(int offerId, List<BogoCartItem> cartItems)
    {
        await using var db = await _contextFactory.CreateDbContextAsync();
        var rule = await db.BogoOfferRules
            .Include(b => b.Offer)
            .ThenInclude(o => o.OfferProducts)
            .FirstOrDefaultAsync(b => b.MarketingOfferId == offerId);

        if (rule == null)
        {
            return new BogoCalculationResult
            {
                Applied = false,
                Description = "لا توجد قاعدة BOGO لهذا العرض"
            };
        }

        var result = new BogoCalculationResult
        {
            Applied = false,
            Description = rule.DisplayLabel,
            TimesApplied = 0,
            TotalDiscount = 0,
            DiscountedItems = []
        };

        // تحديد المنتجات المعنية
        var relevantItems = rule.TargetProductId.HasValue
            ? cartItems.Where(c => c.ProductId == rule.TargetProductId.Value).ToList()
            // إذا كان العرض مرتبط بمنتجات، استخدمها
            : (rule.Offer?.OfferProducts.Count > 0
                ? cartItems.Where(c => rule.Offer.OfferProducts.Any(op => op.ProductId == c.ProductId)).ToList()
                : cartItems);

        if (relevantItems.Count == 0)
        {
            result.Description = "لا توجد منتجات مطابقة في السلة";
            return result;
        }

        // حساب عدد مرات التطبيق
        var totalQualifyingQuantity = relevantItems.Sum(i => i.Quantity);
        var maxApplications = totalQualifyingQuantity / rule.BuyQuantity;

        // تحديد الحد الأقصى للتطبيق
        var timesToApply = Math.Min(maxApplications, rule.MaxApplicationsPerOrder);

        if (timesToApply == 0)
        {
            result.Description = $"تحتاج إلى شراء {rule.BuyQuantity} قطع على الأقل لتطبيق العرض";
            return result;
        }

        // حساب الخصم: أرخص المنتجات تحصل مجاناً
        var sortedItems = relevantItems
            .OrderBy(i => i.UnitPrice)
            .ToList();

        var applicationsApplied = 0;
        var itemIndex = 0;

        // تطبيق BOGO على الدفعات
        for (int app = 0; app < timesToApply; app++)
        {
            var freeCount = rule.FreeQuantity;

            for (int f = 0; f < freeCount && itemIndex < sortedItems.Count; f++)
            {
                var item = sortedItems[itemIndex];
                // تقليل الكمية المتاحة
                if (item.Quantity > 0)
                {
                    var discountAmount = Math.Min(item.UnitPrice, item.UnitPrice); // الخصم = السعر كاملاً (مجاناً)
                    result.TotalDiscount += discountAmount;
                    result.DiscountedItems.Add(new BogoDiscountedItem
                    {
                        ProductId = item.ProductId,
                        ProductName = item.ProductName,
                        OriginalPrice = item.UnitPrice,
                        DiscountedPrice = 0,
                        Label = "مجاناً"
                    });
                    applicationsApplied++;
                }
            }

            // إزالة القطع التي تم تطبيق الخصم عليها
            if (rule.ApplyToCheapestItem)
            {
                // إزالة أرخص العناصر التي تم خصمها
                for (int r = 0; r < rule.FreeQuantity && sortedItems.Count > 0; r++)
                {
                    sortedItems.RemoveAt(0);
                }
            }

            itemIndex = 0; // إعادة تعيين للدورة التالية
        }

        result.TimesApplied = applicationsApplied;
        result.Applied = result.TotalDiscount > 0;

        if (result.Applied)
        {
            result.Description = $"تم تطبيق العرض {result.TimesApplied} مرة — خصم {result.TotalDiscount:N2} ج.م";
        }

        _logger.LogInformation(
            "BOGO applied for offer {OfferId}: {TimesApplied} times, discount={Discount}",
            offerId, result.TimesApplied, result.TotalDiscount);

        return result;
    }

    public async Task DeleteRuleAsync(int id)
    {
        await using var db = await _contextFactory.CreateDbContextAsync();
        var rule = await db.BogoOfferRules.FindAsync(id);
        if (rule == null)
            throw new InvalidOperationException($"قاعدة BOGO {id} غير موجودة");

        db.BogoOfferRules.Remove(rule);
        await db.SaveChangesAsync();

        _logger.LogInformation("Deleted BOGO rule {Id}", id);
    }

    public async Task<List<BogoRuleDto>> GetAllRulesAsync(int page = 1, int pageSize = 20)
    {
        await using var db = await _contextFactory.CreateDbContextAsync();
        var rules = await db.BogoOfferRules
            .Include(b => b.Offer)
            .Include(b => b.TargetProduct)
            .OrderByDescending(b => b.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return rules.Select(ToDto).ToList();
    }

    // ─── دوال المساعدة ────────────────────────────────────────────
    private async Task<BogoRuleDto> ToDtoAsync(AppDbContext db, BogoOfferRule rule)
    {
        await db.Entry(rule).Reference(b => b.Offer).LoadAsync();
        await db.Entry(rule).Reference(b => b.TargetProduct).LoadAsync();
        return ToDto(rule);
    }

    private static BogoRuleDto ToDto(BogoOfferRule rule)
    {
        var summary = !string.IsNullOrWhiteSpace(rule.DisplayLabel)
            ? rule.DisplayLabel
            : $"اشتر {rule.BuyQuantity} واحصل على {rule.FreeQuantity} مجاناً";

        return new BogoRuleDto
        {
            Id = rule.Id,
            MarketingOfferId = rule.MarketingOfferId,
            OfferTitle = rule.Offer?.Title ?? string.Empty,
            BuyQuantity = rule.BuyQuantity,
            FreeQuantity = rule.FreeQuantity,
            ApplyToCheapestItem = rule.ApplyToCheapestItem,
            TargetProductId = rule.TargetProductId,
            TargetProductName = rule.TargetProduct?.Name,
            MaxApplicationsPerOrder = rule.MaxApplicationsPerOrder,
            DisplayLabel = rule.DisplayLabel,
            Summary = summary,
            CreatedAt = rule.CreatedAt
        };
    }
}
