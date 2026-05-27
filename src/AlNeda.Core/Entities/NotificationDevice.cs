using System.ComponentModel.DataAnnotations;

namespace AlNeda.Core.Entities;

/// <summary>
/// جهاز مسجل للإشعارات الفورية (FCM)
/// </summary>
public class NotificationDevice : IEntity
{
    public int Id { get; set; }

    /// <summary>معرف الجهاز من Firebase</summary>
    [Required, MaxLength(500)]
    public string DeviceToken { get; set; } = string.Empty;

    /// <summary>نوع الجهاز: android, ios, web</summary>
    [MaxLength(20)]
    public string Platform { get; set; } = "android";

    /// <summary>معرف المستخدم (اختياري)</summary>
    public int? UserId { get; set; }

    /// <summary>معرف الصيدلية (اختياري)</summary>
    public int? PharmacyId { get; set; }

    /// <summary>هل الجهاز نشط</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>آخر مرة تم فيها تحديث التوكن</summary>
    public DateTime LastSeenAt { get; set; } = DateTime.Now;

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    // Navigation
    public User? User { get; set; }
    public Pharmacy? Pharmacy { get; set; }
}
