using System.ComponentModel.DataAnnotations;

namespace AlNeda.Core.Entities;

/// <summary>
/// قاعدة عرض "اشتري X واحصل على Y مجاناً" (BOGO)
/// </summary>
public class BogoOfferRule : IEntity
{
    public int Id { get; set; }

    /// <summary>العرض المرتبط بهذه القاعدة</summary>
    public int MarketingOfferId { get; set; }

    /// <summary>عدد القطع التي يجب شراؤها (Buy X)</summary>
    public int BuyQuantity { get; set; } = 2;

    /// <summary>عدد القطع المجانية (Get Y Free)</summary>
    public int FreeQuantity { get; set; } = 1;

    /// <summary>هل الخصم على أرخص منتج في السلة (أم على منتج معين)</summary>
    public bool ApplyToCheapestItem { get; set; } = true;

    /// <summary>معرف المنتج المعني (إذا كان null، ينطبق على أي منتج في السلة)</summary>
    public int? TargetProductId { get; set; }

    /// <summary>الحد الأقصى لعدد مرات تطبيق القاعدة في الطلب الواحد</summary>
    public int MaxApplicationsPerOrder { get; set; } = 1;

    /// <summary>وصف مختصر للعرض (للعرض على الواجهة)</summary>
    [MaxLength(200)]
    public string DisplayLabel { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    // Navigation
    public MarketingOffer? Offer { get; set; }
    public Product? TargetProduct { get; set; }
}
