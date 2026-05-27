using System.ComponentModel.DataAnnotations;

namespace AlNeda.Core.Entities;

/// <summary>
/// سجل الإشعارات المرسلة (للتدقيق والتتبع)
/// </summary>
public class NotificationLog : IEntity
{
    public int Id { get; set; }

    /// <summary>نوع الإشعار: offer_published, coupon_assigned, order_status, system</summary>
    [Required, MaxLength(50)]
    public string NotificationType { get; set; } = string.Empty;

    /// <summary>عنوان الإشعار</summary>
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    /// <summary>نص الإشعار</summary>
    [MaxLength(1000)]
    public string Body { get; set; } = string.Empty;

    /// <summary>بيانات إضافية بصيغة JSON</summary>
    public string DataPayload { get; set; } = string.Empty;

    /// <summary>المستقبل (كل الصيدليات / صيدلية محددة)</summary>
    [MaxLength(50)]
    public string TargetType { get; set; } = "all"; // all / pharmacy / user

    /// <summary>معرف المستقبل (إذا كان مستهدفاً)</summary>
    public int? TargetId { get; set; }

    /// <summary>عدد الأجهزة التي أُرسل إليها</summary>
    public int DeviceCount { get; set; }

    /// <summary>عدد الإرسالات الناجحة</summary>
    public int SuccessCount { get; set; }

    /// <summary>عدد الإرسالات الفاشلة</summary>
    public int FailureCount { get; set; }

    /// <summary>حالة الإرسال: pending, sent, failed, partial</summary>
    [MaxLength(20)]
    public string Status { get; set; } = "pending";

    /// <summary>اسم المستخدم الذي أرسل الإشعار</summary>
    [MaxLength(100)]
    public string SentBy { get; set; } = "system";

    /// <summary>معرف العرض المرتبط (اختياري)</summary>
    public int? RelatedOfferId { get; set; }

    /// <summary>معرف الكوبون المرتبط (اختياري)</summary>
    public int? RelatedCouponId { get; set; }

    public DateTime SentAt { get; set; } = DateTime.Now;

    // Navigation
    public MarketingOffer? RelatedOffer { get; set; }
    public Coupon? RelatedCoupon { get; set; }
}
