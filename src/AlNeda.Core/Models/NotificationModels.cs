namespace AlNeda.Core.Models;

// ─── نماذج الإشعارات الفورية ──────────────────────────────────────

public class RegisterDeviceRequest
{
    public string DeviceToken { get; set; } = string.Empty;
    public string Platform { get; set; } = "android";
    public int? UserId { get; set; }
    public int? PharmacyId { get; set; }
}

public class SendNotificationRequest
{
    /// <summary>نوع الإشعار: offer_published, coupon_assigned, order_status, system, promo</summary>
    public string NotificationType { get; set; } = "system";

    /// <summary>عنوان الإشعار</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>نص الإشعار</summary>
    public string Body { get; set; } = string.Empty;

    /// <summary>بيانات إضافية (JSON)</summary>
    public Dictionary<string, string>? Data { get; set; }

    /// <summary>نوع المستقبل: all, pharmacy, user</summary>
    public string TargetType { get; set; } = "all";

    /// <summary>معرف المستقبل (إذا كان مستهدفاً)</summary>
    public int? TargetId { get; set; }

    /// <summary>معرف العرض المرتبط (اختياري)</summary>
    public int? RelatedOfferId { get; set; }

    /// <summary>معرف الكوبون المرتبط (اختياري)</summary>
    public int? RelatedCouponId { get; set; }
}

public class SendOfferNotificationRequest
{
    public int OfferId { get; set; }
    public string? CustomTitle { get; set; }
    public string? CustomBody { get; set; }
    public string TargetType { get; set; } = "targeted"; // all / targeted
}

public class NotificationLogDto
{
    public int Id { get; set; }
    public string NotificationType { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string TargetType { get; set; } = string.Empty;
    public int? TargetId { get; set; }
    public int DeviceCount { get; set; }
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
    public string Status { get; set; } = string.Empty;
    public string SentBy { get; set; } = string.Empty;
    public int? RelatedOfferId { get; set; }
    public string? RelatedOfferTitle { get; set; }
    public int? RelatedCouponId { get; set; }
    public string? RelatedCouponCode { get; set; }
    public DateTime SentAt { get; set; }
}

public class NotificationStatsDto
{
    public int TotalSent { get; set; }
    public int TotalDevices { get; set; }
    public int ActiveDevices { get; set; }
    public double SuccessRate { get; set; }
    public Dictionary<string, int> NotificationsByType { get; set; } = [];
    public List<NotificationLogDto> RecentLogs { get; set; } = [];
}
