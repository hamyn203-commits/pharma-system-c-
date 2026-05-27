using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AlNeda.Core.Entities;

/// <summary>
/// كود خصم ترويجي يمكن تطبيقه على الطلبات
/// </summary>
public class Coupon : IEntity
{
    public int Id { get; set; }

    /// <summary>الكود الفريد (مثل: SAVE20)</summary>
    [Required, MaxLength(50)]
    public string Code { get; set; } = string.Empty;

    /// <summary>وصف الكوبون</summary>
    [MaxLength(300)]
    public string Description { get; set; } = string.Empty;

    /// <summary>نسبة الخصم (مثلاً 20 = 20%)</summary>
    public int? DiscountPercent { get; set; }

    /// <summary>قيمة الخصم الثابتة</summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal? DiscountValue { get; set; }

    /// <summary>الحد الأدنى للطلب لتطبيق الكوبون</summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal? MinOrderAmount { get; set; }

    /// <summary>أقصى خصم مسموح</summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal? MaxDiscountAmount { get; set; }

    /// <summary>هل الكوبون قابل للاستخدام مرة واحدة فقط أم متعدد</summary>
    public bool IsSingleUse { get; set; } = true;

    /// <summary>عدد مرات الاستخدام المسموحة (للكوبونات متعددة الاستخدام)</summary>
    public int? UsageLimit { get; set; }

    /// <summary>عدد مرات الاستخدام الحالية</summary>
    public int CurrentUsageCount { get; set; }

    /// <summary>هل الكوبون موجه لصيدلية معينة</summary>
    public int? TargetPharmacyId { get; set; }

    /// <summary>هل الكوبون موجه لعرض معين</summary>
    public int? TargetOfferId { get; set; }

    /// <summary>تاريخ بدء الصلاحية</summary>
    public DateTime ValidFrom { get; set; } = DateTime.Now;

    /// <summary>تاريخ انتهاء الصلاحية</summary>
    public DateTime ValidUntil { get; set; } = DateTime.Now.AddDays(30);

    /// <summary>حالة الكوبون: active, expired, disabled</summary>
    [MaxLength(20)]
    public string Status { get; set; } = "active";

    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    // Navigation
    public Pharmacy? TargetPharmacy { get; set; }
    public MarketingOffer? TargetOffer { get; set; }
}
