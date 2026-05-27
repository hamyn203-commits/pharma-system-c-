namespace AlNeda.Core.Models;

// ─── طلبات واستجابات الكوبونات ─────────────────────────────────────

public class CreateCouponRequest
{
    public string Code { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int? DiscountPercent { get; set; }
    public decimal? DiscountValue { get; set; }
    public decimal? MinOrderAmount { get; set; }
    public decimal? MaxDiscountAmount { get; set; }
    public bool IsSingleUse { get; set; } = true;
    public int? UsageLimit { get; set; }
    public int? TargetPharmacyId { get; set; }
    public int? TargetOfferId { get; set; }
    public DateTime ValidFrom { get; set; } = DateTime.Now;
    public DateTime ValidUntil { get; set; } = DateTime.Now.AddDays(30);
}

public class UpdateCouponRequest
{
    public string? Description { get; set; }
    public int? DiscountPercent { get; set; }
    public decimal? DiscountValue { get; set; }
    public decimal? MinOrderAmount { get; set; }
    public decimal? MaxDiscountAmount { get; set; }
    public int? UsageLimit { get; set; }
    public DateTime? ValidFrom { get; set; }
    public DateTime? ValidUntil { get; set; }
    public string? Status { get; set; }
}

public class ApplyCouponRequest
{
    public string Code { get; set; } = string.Empty;
    public decimal OrderTotal { get; set; }
    public int? PharmacyId { get; set; }
    public int? OfferId { get; set; }
}

public class CouponDto
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int? DiscountPercent { get; set; }
    public decimal? DiscountValue { get; set; }
    public decimal? MinOrderAmount { get; set; }
    public decimal? MaxDiscountAmount { get; set; }
    public bool IsSingleUse { get; set; }
    public int? UsageLimit { get; set; }
    public int CurrentUsageCount { get; set; }
    public int? TargetPharmacyId { get; set; }
    public string? TargetPharmacyName { get; set; }
    public int? TargetOfferId { get; set; }
    public string? TargetOfferTitle { get; set; }
    public DateTime ValidFrom { get; set; }
    public DateTime ValidUntil { get; set; }
    public string Status { get; set; } = string.Empty;
    public bool IsValid { get; set; }
    public string ValidationMessage { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class CouponValidationResult
{
    public bool IsValid { get; set; }
    public string Message { get; set; } = string.Empty;
    public decimal? DiscountAmount { get; set; }
    public decimal? FinalTotal { get; set; }
    public CouponDto? Coupon { get; set; }
}
