using AlNeda.Core.Entities;
using AlNeda.Core.Models;
using AlNeda.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AlNeda.Services;

/// <summary>
/// 🎫 خدمة إدارة الكوبونات والأكواد الترويجية — منفصلة تماماً
/// </summary>
public interface ICouponService
{
    /// <summary>إنشاء كوبون جديد</summary>
    Task<CouponDto> CreateAsync(CreateCouponRequest request);

    /// <summary>تحديث كوبون</summary>
    Task<CouponDto> UpdateAsync(int id, UpdateCouponRequest request);

    /// <summary>الحصول على كوبون بالمعرف</summary>
    Task<CouponDto?> GetByIdAsync(int id);

    /// <summary>البحث عن كوبون بالكود</summary>
    Task<CouponDto?> GetByCodeAsync(string code);

    /// <summary>قائمة الكوبونات</summary>
    Task<List<CouponDto>> GetAllAsync(int page = 1, int pageSize = 20);

    /// <summary>التحقق من صحة الكوبون وتطبيقه</summary>
    Task<CouponValidationResult> ValidateAndApplyAsync(ApplyCouponRequest request);

    /// <summary>تسجيل استخدام كوبون</summary>
    Task RecordUsageAsync(int couponId);

    /// <summary>تعطيل/تفعيل كوبون</summary>
    Task SetStatusAsync(int id, string status);

    /// <summary>حذف كوبون</summary>
    Task DeleteAsync(int id);
}

public class CouponService : ICouponService
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;
    private readonly ILogger<CouponService> _logger;

    public CouponService(
        IDbContextFactory<AppDbContext> contextFactory,
        ILogger<CouponService> logger)
    {
        _contextFactory = contextFactory;
        _logger = logger;
    }

    public async Task<CouponDto> CreateAsync(CreateCouponRequest request)
    {
        await using var db = await _contextFactory.CreateDbContextAsync();

        // التحقق من عدم وجود كود مكرر
        var existing = await db.Coupons.FirstOrDefaultAsync(c => c.Code == request.Code.Trim().ToUpperInvariant());
        if (existing != null)
            throw new InvalidOperationException($"الكود '{request.Code}' موجود بالفعل");

        var coupon = new Coupon
        {
            Code = request.Code.Trim().ToUpperInvariant(),
            Description = request.Description ?? string.Empty,
            DiscountPercent = request.DiscountPercent,
            DiscountValue = request.DiscountValue,
            MinOrderAmount = request.MinOrderAmount,
            MaxDiscountAmount = request.MaxDiscountAmount,
            IsSingleUse = request.IsSingleUse,
            UsageLimit = request.UsageLimit,
            TargetPharmacyId = request.TargetPharmacyId,
            TargetOfferId = request.TargetOfferId,
            ValidFrom = request.ValidFrom,
            ValidUntil = request.ValidUntil,
            Status = "active",
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };

        db.Coupons.Add(coupon);
        await db.SaveChangesAsync();

        _logger.LogInformation("Created coupon {Code} (id: {Id})", coupon.Code, coupon.Id);
        return await ToDtoAsync(db, coupon);
    }

    public async Task<CouponDto> UpdateAsync(int id, UpdateCouponRequest request)
    {
        await using var db = await _contextFactory.CreateDbContextAsync();
        var coupon = await db.Coupons.FindAsync(id);
        if (coupon == null)
            throw new InvalidOperationException($"الكوبون {id} غير موجود");

        if (request.Description != null) coupon.Description = request.Description;
        if (request.DiscountPercent.HasValue) coupon.DiscountPercent = request.DiscountPercent;
        if (request.DiscountValue.HasValue) coupon.DiscountValue = request.DiscountValue;
        if (request.MinOrderAmount.HasValue) coupon.MinOrderAmount = request.MinOrderAmount;
        if (request.MaxDiscountAmount.HasValue) coupon.MaxDiscountAmount = request.MaxDiscountAmount;
        if (request.UsageLimit.HasValue) coupon.UsageLimit = request.UsageLimit;
        if (request.ValidFrom.HasValue) coupon.ValidFrom = request.ValidFrom.Value;
        if (request.ValidUntil.HasValue) coupon.ValidUntil = request.ValidUntil.Value;
        if (request.Status != null) coupon.Status = request.Status;

        coupon.UpdatedAt = DateTime.Now;
        await db.SaveChangesAsync();

        _logger.LogInformation("Updated coupon {Code} (id: {Id})", coupon.Code, coupon.Id);
        return await ToDtoAsync(db, coupon);
    }

    public async Task<CouponDto?> GetByIdAsync(int id)
    {
        await using var db = await _contextFactory.CreateDbContextAsync();
        var coupon = await db.Coupons
            .Include(c => c.TargetPharmacy)
            .Include(c => c.TargetOffer)
            .FirstOrDefaultAsync(c => c.Id == id);

        return coupon == null ? null : ToDto(coupon);
    }

    public async Task<CouponDto?> GetByCodeAsync(string code)
    {
        await using var db = await _contextFactory.CreateDbContextAsync();
        var coupon = await db.Coupons
            .Include(c => c.TargetPharmacy)
            .Include(c => c.TargetOffer)
            .FirstOrDefaultAsync(c => c.Code == code.Trim().ToUpperInvariant());

        return coupon == null ? null : ToDto(coupon);
    }

    public async Task<List<CouponDto>> GetAllAsync(int page = 1, int pageSize = 20)
    {
        await using var db = await _contextFactory.CreateDbContextAsync();
        var coupons = await db.Coupons
            .Include(c => c.TargetPharmacy)
            .Include(c => c.TargetOffer)
            .OrderByDescending(c => c.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return coupons.Select(ToDto).ToList();
    }

    public async Task<CouponValidationResult> ValidateAndApplyAsync(ApplyCouponRequest request)
    {
        await using var db = await _contextFactory.CreateDbContextAsync();
        var coupon = await db.Coupons
            .Include(c => c.TargetPharmacy)
            .Include(c => c.TargetOffer)
            .FirstOrDefaultAsync(c => c.Code == request.Code.Trim().ToUpperInvariant());

        var result = new CouponValidationResult
        {
            IsValid = false,
            Message = "الكود غير صالح"
        };

        if (coupon == null)
        {
            result.Message = "كود الخصم غير موجود";
            return result;
        }

        // تحقق من الحالة
        if (coupon.Status != "active")
        {
            result.Message = "كود الخصم غير نشط";
            return result;
        }

        // تحقق من تاريخ الصلاحية
        var now = DateTime.Now;
        if (now < coupon.ValidFrom)
        {
            result.Message = "كود الخصم لم يبدأ بعد";
            return result;
        }
        if (now > coupon.ValidUntil)
        {
            result.Message = "كود الخصم منتهي الصلاحية";
            return result;
        }

        // تحقق من حد الاستخدام
        if (coupon.UsageLimit.HasValue && coupon.CurrentUsageCount >= coupon.UsageLimit.Value)
        {
            result.Message = "تم استنفاذ عدد استخدامات كود الخصم";
            return result;
        }

        // تحقق من الصيدلية المستهدفة
        if (coupon.TargetPharmacyId.HasValue && request.PharmacyId.HasValue)
        {
            if (coupon.TargetPharmacyId.Value != request.PharmacyId.Value)
            {
                result.Message = "كود الخصم غير صالح لهذه الصيدلية";
                return result;
            }
        }

        // تحقق من العرض المستهدف
        if (coupon.TargetOfferId.HasValue && request.OfferId.HasValue)
        {
            if (coupon.TargetOfferId.Value != request.OfferId.Value)
            {
                result.Message = "كود الخصم غير صالح لهذا العرض";
                return result;
            }
        }

        // تحقق من الحد الأدنى للطلب
        if (coupon.MinOrderAmount.HasValue && request.OrderTotal < coupon.MinOrderAmount.Value)
        {
            result.Message = $"الحد الأدنى للطلب لتطبيق الكود هو {coupon.MinOrderAmount.Value:N2} ج.م";
            return result;
        }

        // حساب الخصم
        decimal discountAmount = 0;

        // الخصم بالنسبة المئوية
        if (coupon.DiscountPercent.HasValue && coupon.DiscountPercent.Value > 0)
        {
            discountAmount = request.OrderTotal * coupon.DiscountPercent.Value / 100m;
        }

        // الخصم بالقيمة الثابتة (إذا كان موجوداً، يطبق بدلاً من النسبة)
        if (coupon.DiscountValue.HasValue && coupon.DiscountValue.Value > 0)
        {
            discountAmount = coupon.DiscountValue.Value;
        }

        // تطبيق حد الخصم الأقصى
        if (coupon.MaxDiscountAmount.HasValue && discountAmount > coupon.MaxDiscountAmount.Value)
        {
            discountAmount = coupon.MaxDiscountAmount.Value;
        }

        var finalTotal = request.OrderTotal - discountAmount;
        if (finalTotal < 0) finalTotal = 0;

        result.IsValid = true;
        result.Message = $"تم تطبيق الخصم: {discountAmount:N2} ج.م";
        result.DiscountAmount = discountAmount;
        result.FinalTotal = finalTotal;
        result.Coupon = ToDto(coupon);

        _logger.LogInformation(
            "Coupon {Code} applied: discount={Discount}, total={Total}->{Final}",
            coupon.Code, discountAmount, request.OrderTotal, finalTotal);

        return result;
    }

    public async Task RecordUsageAsync(int couponId)
    {
        await using var db = await _contextFactory.CreateDbContextAsync();
        var coupon = await db.Coupons.FindAsync(couponId);
        if (coupon != null)
        {
            coupon.CurrentUsageCount++;
            coupon.UpdatedAt = DateTime.Now;
            await db.SaveChangesAsync();

            // إذا كان الكوبون للاستخدام مرة واحدة، قم بتعطيله
            if (coupon.IsSingleUse)
            {
                coupon.Status = "used";
                await db.SaveChangesAsync();
            }
            // إذا تم استنفاذ العدد المسموح، قم بتعطيله
            else if (coupon.UsageLimit.HasValue && coupon.CurrentUsageCount >= coupon.UsageLimit.Value)
            {
                coupon.Status = "depleted";
                await db.SaveChangesAsync();
            }
        }
    }

    public async Task SetStatusAsync(int id, string status)
    {
        await using var db = await _contextFactory.CreateDbContextAsync();
        var coupon = await db.Coupons.FindAsync(id);
        if (coupon == null)
            throw new InvalidOperationException($"الكوبون {id} غير موجود");

        coupon.Status = status;
        coupon.UpdatedAt = DateTime.Now;
        await db.SaveChangesAsync();

        _logger.LogInformation("Coupon {Code} status changed to {Status}", coupon.Code, status);
    }

    public async Task DeleteAsync(int id)
    {
        await using var db = await _contextFactory.CreateDbContextAsync();
        var coupon = await db.Coupons.FindAsync(id);
        if (coupon == null)
            throw new InvalidOperationException($"الكوبون {id} غير موجود");

        db.Coupons.Remove(coupon);
        await db.SaveChangesAsync();

        _logger.LogInformation("Deleted coupon {Code} (id: {Id})", coupon.Code, coupon.Id);
    }

    // ─── دوال المساعدة ────────────────────────────────────────────
    private async Task<CouponDto> ToDtoAsync(AppDbContext db, Coupon coupon)
    {
        await db.Entry(coupon).Reference(c => c.TargetPharmacy).LoadAsync();
        await db.Entry(coupon).Reference(c => c.TargetOffer).LoadAsync();
        return ToDto(coupon);
    }

    private static CouponDto ToDto(Coupon coupon)
    {
        var now = DateTime.Now;
        var isExpired = now > coupon.ValidUntil;
        var notStarted = now < coupon.ValidFrom;
        var usageExhausted = coupon.UsageLimit.HasValue && coupon.CurrentUsageCount >= coupon.UsageLimit.Value;

        string validationMessage;
        if (coupon.Status != "active")
            validationMessage = coupon.Status == "used" ? "تم استخدام الكود" : "الكود غير نشط";
        else if (isExpired)
            validationMessage = "الكود منتهي الصلاحية";
        else if (notStarted)
            validationMessage = "الكود لم يبدأ بعد";
        else if (usageExhausted)
            validationMessage = "تم استنفاذ عدد الاستخدامات";
        else
            validationMessage = "الكود صالح";

        return new CouponDto
        {
            Id = coupon.Id,
            Code = coupon.Code,
            Description = coupon.Description,
            DiscountPercent = coupon.DiscountPercent,
            DiscountValue = coupon.DiscountValue,
            MinOrderAmount = coupon.MinOrderAmount,
            MaxDiscountAmount = coupon.MaxDiscountAmount,
            IsSingleUse = coupon.IsSingleUse,
            UsageLimit = coupon.UsageLimit,
            CurrentUsageCount = coupon.CurrentUsageCount,
            TargetPharmacyId = coupon.TargetPharmacyId,
            TargetPharmacyName = coupon.TargetPharmacy?.Name,
            TargetOfferId = coupon.TargetOfferId,
            TargetOfferTitle = coupon.TargetOffer?.Title,
            ValidFrom = coupon.ValidFrom,
            ValidUntil = coupon.ValidUntil,
            Status = coupon.Status,
            IsValid = coupon.Status == "active" && !isExpired && !notStarted && !usageExhausted,
            ValidationMessage = validationMessage,
            CreatedAt = coupon.CreatedAt
        };
    }
}
