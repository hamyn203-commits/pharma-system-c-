using AlNeda.API.Authorization;
using AlNeda.Core.Models;
using AlNeda.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AlNeda.API.Controllers;

/// <summary>
/// 🔔 التحكم بالإشعارات الفورية — منفصل تماماً
/// </summary>
[ApiController]
[Route("api/admin/notifications")]
[Tags("Admin Notifications")]
public class NotificationsController : ControllerBase
{
    private readonly INotificationsService _notifications;
    private readonly IAuditService _audit;

    public NotificationsController(INotificationsService notifications, IAuditService audit)
    {
        _notifications = notifications;
        _audit = audit;
    }

    /// <summary>تسجيل جهاز للإشعارات (يستخدمه تطبيق الصيدلي)</summary>
    [HttpPost("devices/register")]
    [AllowAnonymous]
    public async Task<IActionResult> RegisterDevice([FromBody] RegisterDeviceRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.DeviceToken))
            return BadRequest(new ApiError { Message = "رمز الجهاز مطلوب", Code = "VALIDATION_ERROR" });

        var device = await _notifications.RegisterDeviceAsync(request);
        return Ok(new { id = device.Id, message = "تم تسجيل الجهاز بنجاح" });
    }

    /// <summary>إلغاء تسجيل جهاز</summary>
    [HttpPost("devices/unregister")]
    [AllowAnonymous]
    public async Task<IActionResult> UnregisterDevice([FromBody] UnregisterDeviceRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.DeviceToken))
            return BadRequest(new ApiError { Message = "رمز الجهاز مطلوب", Code = "VALIDATION_ERROR" });

        await _notifications.UnregisterDeviceAsync(request.DeviceToken);
        return Ok(new { message = "تم إلغاء تسجيل الجهاز" });
    }

    /// <summary>إرسال إشعار للجميع</summary>
    [HttpPost("send/all")]
    [Authorize(Policy = AuthPolicies.AdminOnly)]
    public async Task<IActionResult> SendToAll([FromBody] SendNotificationRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.Body))
            return BadRequest(new ApiError { Message = "عنوان ونص الإشعار مطلوبان", Code = "VALIDATION_ERROR" });

        var log = await _notifications.SendToAllAsync(request, User.Identity?.Name ?? "admin");
        await _audit.LogAsync(User.Identity?.Name ?? "system", "notify-all", "Notification", log.Id.ToString(),
            $"Sent notification to all: '{request.Title}'");

        return Ok(ToLogDto(log));
    }

    /// <summary>إرسال إشعار لصيدلية محددة</summary>
    [HttpPost("send/pharmacy/{pharmacyId:int}")]
    [Authorize(Policy = AuthPolicies.AdminOnly)]
    public async Task<IActionResult> SendToPharmacy(int pharmacyId, [FromBody] SendNotificationRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.Body))
            return BadRequest(new ApiError { Message = "عنوان ونص الإشعار مطلوبان", Code = "VALIDATION_ERROR" });

        var log = await _notifications.SendToPharmacyAsync(pharmacyId, request, User.Identity?.Name ?? "admin");
        await _audit.LogAsync(User.Identity?.Name ?? "system", "notify-pharmacy", "Notification", log.Id.ToString(),
            $"Sent notification to pharmacy #{pharmacyId}: '{request.Title}'");

        return Ok(ToLogDto(log));
    }

    /// <summary>إرسال إشعار بنشر عرض</summary>
    [HttpPost("send/offer/{offerId:int}")]
    [Authorize(Policy = AuthPolicies.AdminOnly)]
    public async Task<IActionResult> SendOfferNotification(int offerId, [FromBody] SendOfferNotificationRequest? request)
    {
        var log = await _notifications.SendOfferPublishedNotificationAsync(
            offerId,
            request?.CustomTitle,
            request?.CustomBody);

        await _audit.LogAsync(User.Identity?.Name ?? "system", "notify-offer", "Notification", log.Id.ToString(),
            $"Sent offer notification for offer #{offerId}");

        return Ok(ToLogDto(log));
    }

    /// <summary>إرسال إشعار بكوبون</summary>
    [HttpPost("send/coupon/{couponId:int}")]
    [Authorize(Policy = AuthPolicies.AdminOnly)]
    public async Task<IActionResult> SendCouponNotification(int couponId, [FromQuery] int? pharmacyId = null)
    {
        var log = await _notifications.SendCouponAssignedNotificationAsync(couponId, pharmacyId);
        await _audit.LogAsync(User.Identity?.Name ?? "system", "notify-coupon", "Notification", log.Id.ToString(),
            $"Sent coupon notification for coupon #{couponId}");

        return Ok(ToLogDto(log));
    }

    /// <summary>سجل الإشعارات المرسلة</summary>
    [HttpGet("logs")]
    [Authorize(Policy = AuthPolicies.AdminOnly)]
    public async Task<IActionResult> GetLogs([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var logs = await _notifications.GetNotificationLogsAsync(page, pageSize);
        return Ok(logs);
    }

    /// <summary>إحصائيات الإشعارات</summary>
    [HttpGet("stats")]
    [Authorize(Policy = AuthPolicies.AdminOnly)]
    public async Task<IActionResult> GetStats()
    {
        var stats = await _notifications.GetNotificationStatsAsync();
        return Ok(stats);
    }

    /// <summary>عدد الأجهزة النشطة</summary>
    [HttpGet("devices/count")]
    [Authorize(Policy = AuthPolicies.AdminOnly)]
    public async Task<IActionResult> GetDeviceCount()
    {
        var count = await _notifications.GetActiveDeviceCountAsync();
        return Ok(new { activeDevices = count });
    }

    private static NotificationLogDto ToLogDto(Core.Entities.NotificationLog log)
    {
        return new NotificationLogDto
        {
            Id = log.Id,
            NotificationType = log.NotificationType,
            Title = log.Title,
            Body = log.Body,
            TargetType = log.TargetType,
            TargetId = log.TargetId,
            DeviceCount = log.DeviceCount,
            SuccessCount = log.SuccessCount,
            FailureCount = log.FailureCount,
            Status = log.Status,
            SentBy = log.SentBy,
            RelatedOfferId = log.RelatedOfferId,
            RelatedCouponId = log.RelatedCouponId,
            SentAt = log.SentAt
        };
    }
}

public class UnregisterDeviceRequest
{
    public string DeviceToken { get; set; } = string.Empty;
}
