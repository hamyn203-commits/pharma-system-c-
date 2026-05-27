using System.Text.Json;
using AlNeda.Core.Entities;
using AlNeda.Core.Models;
using AlNeda.Data;
using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AlNeda.Services;

/// <summary>
/// 🔔 خدمة الإشعارات الفورية (FCM) — منفصلة تماماً
/// </summary>
public interface INotificationsService
{
    /// <summary>تسجيل جهاز جديد للإشعارات</summary>
    Task<NotificationDevice> RegisterDeviceAsync(RegisterDeviceRequest request);

    /// <summary>إلغاء تسجيل جهاز</summary>
    Task UnregisterDeviceAsync(string deviceToken);

    /// <summary>إرسال إشعار لجميع الصيدليات</summary>
    Task<NotificationLog> SendToAllAsync(SendNotificationRequest request, string sentBy = "system");

    /// <summary>إرسال إشعار لصيدلية محددة</summary>
    Task<NotificationLog> SendToPharmacyAsync(int pharmacyId, SendNotificationRequest request, string sentBy = "system");

    /// <summary>إرسال إشعار عند نشر عرض جديد</summary>
    Task<NotificationLog> SendOfferPublishedNotificationAsync(int offerId, string? customTitle = null, string? customBody = null);

    /// <summary>إرسال إشعار عند إسناد كوبون</summary>
    Task<NotificationLog> SendCouponAssignedNotificationAsync(int couponId, int? pharmacyId = null);

    /// <summary>الحصول على سجل الإشعارات</summary>
    Task<List<NotificationLogDto>> GetNotificationLogsAsync(int page = 1, int pageSize = 20);

    /// <summary>إحصائيات الإشعارات</summary>
    Task<NotificationStatsDto> GetNotificationStatsAsync();

    /// <summary>عدد الأجهزة النشطة</summary>
    Task<int> GetActiveDeviceCountAsync();
}

public class NotificationsService : INotificationsService
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;
    private readonly ILogger<NotificationsService> _logger;
    private readonly IConfiguration _configuration;

    private static bool _firebaseInitialized = false;
    private static readonly object _firebaseLock = new();

    public NotificationsService(
        IDbContextFactory<AppDbContext> contextFactory,
        ILogger<NotificationsService> logger,
        IConfiguration configuration)
    {
        _contextFactory = contextFactory;
        _logger = logger;
        _configuration = configuration;
        InitializeFirebase();
    }

    /// <summary>تهيئة Firebase مرة واحدة فقط</summary>
    private static void InitializeFirebase()
    {
        if (_firebaseInitialized) return;
        lock (_firebaseLock)
        {
            if (_firebaseInitialized) return;
            try
            {
                // محاولة التهيئة من متغير البيئة GOOGLE_APPLICATION_CREDENTIALS أولاً
                var credPath = Environment.GetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS");
                if (!string.IsNullOrWhiteSpace(credPath) && File.Exists(credPath))
                {
                    FirebaseApp.Create(new AppOptions
                    {
                        Credential = GoogleCredential.FromFile(credPath)
                    });
                }
                else
                {
                    // محاولة التهيئة من ملف serviceAccountKey.json في المجلد الحالي
                    var localKey = Path.Combine(Directory.GetCurrentDirectory(), "serviceAccountKey.json");
                    if (File.Exists(localKey))
                    {
                        FirebaseApp.Create(new AppOptions
                        {
                            Credential = GoogleCredential.FromFile(localKey)
                        });
                    }
                    else
                    {
                        // بدون ملف — سيتم تسجيل التحذير فقط واستخدام المحاكاة كـ fallback
                        Console.WriteLine("⚠️ Firebase: لم يتم العثور على serviceAccountKey.json — سيتم استخدام المحاكاة للإشعارات");
                    }
                }
                _firebaseInitialized = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Firebase initialization failed: {ex.Message} — using simulation mode");
            }
        }
    }

    public async Task<NotificationDevice> RegisterDeviceAsync(RegisterDeviceRequest request)
    {
        await using var db = await _contextFactory.CreateDbContextAsync();

        // البحث عن جهاز موجود بنفس التوكن
        var existing = await db.NotificationDevices
            .FirstOrDefaultAsync(d => d.DeviceToken == request.DeviceToken);

        if (existing != null)
        {
            existing.Platform = request.Platform;
            existing.UserId = request.UserId;
            existing.PharmacyId = request.PharmacyId;
            existing.IsActive = true;
            existing.LastSeenAt = DateTime.Now;
            await db.SaveChangesAsync();
            _logger.LogInformation("Updated existing device: {Token}", request.DeviceToken);
            return existing;
        }

        var device = new NotificationDevice
        {
            DeviceToken = request.DeviceToken,
            Platform = request.Platform,
            UserId = request.UserId,
            PharmacyId = request.PharmacyId,
            IsActive = true,
            LastSeenAt = DateTime.Now,
            CreatedAt = DateTime.Now
        };

        db.NotificationDevices.Add(device);
        await db.SaveChangesAsync();
        _logger.LogInformation("Registered new device: {Token} ({Platform})", request.DeviceToken, request.Platform);
        return device;
    }

    public async Task UnregisterDeviceAsync(string deviceToken)
    {
        await using var db = await _contextFactory.CreateDbContextAsync();
        var device = await db.NotificationDevices
            .FirstOrDefaultAsync(d => d.DeviceToken == deviceToken);

        if (device != null)
        {
            device.IsActive = false;
            await db.SaveChangesAsync();
            _logger.LogInformation("Unregistered device: {Token}", deviceToken);
        }
    }

    public async Task<NotificationLog> SendToAllAsync(SendNotificationRequest request, string sentBy = "system")
    {
        await using var db = await _contextFactory.CreateDbContextAsync();
        var devices = await db.NotificationDevices
            .Where(d => d.IsActive)
            .ToListAsync();

        return await SendNotificationAsync(db, request, devices, "all", null, sentBy);
    }

    public async Task<NotificationLog> SendToPharmacyAsync(int pharmacyId, SendNotificationRequest request, string sentBy = "system")
    {
        await using var db = await _contextFactory.CreateDbContextAsync();
        var devices = await db.NotificationDevices
            .Where(d => d.IsActive && d.PharmacyId == pharmacyId)
            .ToListAsync();

        return await SendNotificationAsync(db, request, devices, "pharmacy", pharmacyId, sentBy);
    }

    public async Task<NotificationLog> SendOfferPublishedNotificationAsync(
        int offerId, string? customTitle = null, string? customBody = null)
    {
        await using var db = await _contextFactory.CreateDbContextAsync();
        var offer = await db.MarketingOffers
            .Include(o => o.PharmacyTargets)
            .FirstOrDefaultAsync(o => o.Id == offerId);

        if (offer == null)
        {
            _logger.LogWarning("Offer {OfferId} not found for notification", offerId);
            throw new InvalidOperationException($"العرض {offerId} غير موجود");
        }

        var title = customTitle ?? $"🎉 عرض جديد: {offer.Title}";
        var body = customBody ?? $"خصم {offer.DiscountPercent}% على {offer.OfferProducts.Count} منتج — تصفح الآن!";

        var data = new Dictionary<string, string>
        {
            ["type"] = "offer_published",
            ["offerId"] = offerId.ToString(),
            ["offerType"] = offer.OfferType,
            ["title"] = offer.Title
        };

        var request = new SendNotificationRequest
        {
            NotificationType = "offer_published",
            Title = title,
            Body = body,
            Data = data,
            TargetType = "targeted",
            RelatedOfferId = offerId
        };

        // تحديد المستهدفين
        if (offer.PharmacyTargets.Count > 0)
        {
            // إرسال للصيدليات المستهدفة فقط
            var targetPharmacyIds = offer.PharmacyTargets.Select(t => t.PharmacyId).ToList();
            await using var db2 = await _contextFactory.CreateDbContextAsync();
            var devices = await db2.NotificationDevices
                .Where(d => d.IsActive && d.PharmacyId != null && targetPharmacyIds.Contains(d.PharmacyId.Value))
                .ToListAsync();

            return await SendNotificationAsync(db2, request, devices, "targeted", null, "system");
        }
        else
        {
            // إرسال للكل (AudienceRule = all)
            return await SendToAllAsync(request, "system");
        }
    }

    public async Task<NotificationLog> SendCouponAssignedNotificationAsync(int couponId, int? pharmacyId = null)
    {
        await using var db = await _contextFactory.CreateDbContextAsync();
        var coupon = await db.Coupons
            .Include(c => c.TargetPharmacy)
            .FirstOrDefaultAsync(c => c.Id == couponId);

        if (coupon == null)
        {
            _logger.LogWarning("Coupon {CouponId} not found for notification", couponId);
            throw new InvalidOperationException($"الكوبون {couponId} غير موجود");
        }

        var data = new Dictionary<string, string>
        {
            ["type"] = "coupon_assigned",
            ["couponCode"] = coupon.Code,
            ["couponId"] = couponId.ToString()
        };

        var title = "🎫 كود خصم جديد لك!";
        var discountText = coupon.DiscountPercent.HasValue
            ? $"{coupon.DiscountPercent}% خصم"
            : $"{coupon.DiscountValue:N2} ج.م خصم";
        var body = $"كود {coupon.Code}: {discountText} — استخدمه في طلبك القادم!";

        var request = new SendNotificationRequest
        {
            NotificationType = "coupon_assigned",
            Title = title,
            Body = body,
            Data = data,
            RelatedCouponId = couponId
        };

        if (pharmacyId.HasValue || coupon.TargetPharmacyId.HasValue)
        {
            var targetId = pharmacyId ?? coupon.TargetPharmacyId.Value;
            return await SendToPharmacyAsync(targetId, request, "system");
        }

        return await SendToAllAsync(request, "system");
    }

    public async Task<List<NotificationLogDto>> GetNotificationLogsAsync(int page = 1, int pageSize = 20)
    {
        await using var db = await _contextFactory.CreateDbContextAsync();
        var logs = await db.NotificationLogs
            .OrderByDescending(n => n.SentAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(n => new NotificationLogDto
            {
                Id = n.Id,
                NotificationType = n.NotificationType,
                Title = n.Title,
                Body = n.Body,
                TargetType = n.TargetType,
                TargetId = n.TargetId,
                DeviceCount = n.DeviceCount,
                SuccessCount = n.SuccessCount,
                FailureCount = n.FailureCount,
                Status = n.Status,
                SentBy = n.SentBy,
                RelatedOfferId = n.RelatedOfferId,
                RelatedOfferTitle = n.RelatedOffer != null ? n.RelatedOffer.Title : null,
                RelatedCouponId = n.RelatedCouponId,
                RelatedCouponCode = n.RelatedCoupon != null ? n.RelatedCoupon.Code : null,
                SentAt = n.SentAt
            })
            .ToListAsync();

        return logs;
    }

    public async Task<NotificationStatsDto> GetNotificationStatsAsync()
    {
        await using var db = await _contextFactory.CreateDbContextAsync();

        var totalSent = await db.NotificationLogs.CountAsync();
        var totalDevices = await db.NotificationDevices.CountAsync();
        var activeDevices = await db.NotificationDevices.CountAsync(d => d.IsActive);
        var totalSuccess = await db.NotificationLogs.SumAsync(n => n.SuccessCount);
        var totalAttempts = await db.NotificationLogs.SumAsync(n => n.DeviceCount);

        var byType = await db.NotificationLogs
            .GroupBy(n => n.NotificationType)
            .Select(g => new { Type = g.Key, Count = g.Count() })
            .ToDictionaryAsync(g => g.Type, g => g.Count);

        var recentLogs = await GetNotificationLogsAsync(1, 10);

        return new NotificationStatsDto
        {
            TotalSent = totalSent,
            TotalDevices = totalDevices,
            ActiveDevices = activeDevices,
            SuccessRate = totalAttempts > 0 ? (double)totalSuccess / totalAttempts * 100 : 0,
            NotificationsByType = byType,
            RecentLogs = recentLogs
        };
    }

    public async Task<int> GetActiveDeviceCountAsync()
    {
        await using var db = await _contextFactory.CreateDbContextAsync();
        return await db.NotificationDevices.CountAsync(d => d.IsActive);
    }

    // ─── الطريقة الأساسية للإرسال ─────────────────────────────────
    private async Task<NotificationLog> SendNotificationAsync(
        AppDbContext db,
        SendNotificationRequest request,
        List<NotificationDevice> devices,
        string targetType,
        int? targetId,
        string sentBy)
    {
        var deviceCount = devices.Count;
        var successCount = 0;
        var failureCount = 0;

        _logger.LogInformation(
            "Sending notification '{Title}' to {Count} devices (type: {TargetType})",
            request.Title, deviceCount, targetType);

        // بناء البيانات الإضافية
        var data = new Dictionary<string, string>
        {
            ["type"] = request.NotificationType,
            ["title"] = request.Title,
            ["body"] = request.Body,
            ["click_action"] = "FLUTTER_NOTIFICATION_CLICK"
        };

        if (request.RelatedOfferId.HasValue)
            data["offerId"] = request.RelatedOfferId.Value.ToString();
        if (request.RelatedCouponId.HasValue)
            data["couponId"] = request.RelatedCouponId.Value.ToString();

        // دمج البيانات المخصصة
        if (request.Data != null)
        {
            foreach (var kv in request.Data)
            {
                data[kv.Key] = kv.Value;
            }
        }

        // بناء رسالة الإشعار
        var message = new MulticastMessage
        {
            Notification = new FirebaseAdmin.Messaging.Notification
            {
                Title = request.Title,
                Body = request.Body
            },
            Data = data.ToDictionary(kv => kv.Key, kv => kv.Value),
            Tokens = devices.Select(d => d.DeviceToken).ToList(),
            Android = new AndroidConfig
            {
                Notification = new AndroidNotification
                {
                    Sound = "default",
                    ChannelId = "offers_channel",
                    Priority = NotificationPriority.HIGH,
                    ClickAction = "FLUTTER_NOTIFICATION_CLICK"
                }
            },
            Apns = new ApnsConfig
            {
                Aps = new Aps
                {
                    Sound = "default",
                    ContentAvailable = true
                }
            }
        };

        // إرسال عبر Firebase إذا كان مهيئاً، وإلا محاكاة
        foreach (var device in devices)
        {
            try
            {
                if (_firebaseInitialized)
                {
                    // إرسال فردي لكل جهاز (للتتبع الدقيق)
                    var singleMessage = new Message
                    {
                        Token = device.DeviceToken,
                        Notification = message.Notification,
                        Data = message.Data,
                        Android = message.Android,
                        Apns = message.Apns
                    };
                    await FirebaseMessaging.DefaultInstance.SendAsync(singleMessage);
                }
                else
                {
                    // محاكاة الإرسال
                    _logger.LogDebug("Simulated send to device {Token}: {Title}", device.DeviceToken, request.Title);
                }
                successCount++;
            }
            catch (FirebaseMessagingException ex)
            {
                _logger.LogError(ex, "Firebase failed to send to device {Token}: {Error}", device.DeviceToken, ex.ErrorCode);
                failureCount++;

                // إذا كان التوكن غير صالح، ضع علامة على الجهاز
                var code = ex.ErrorCode.ToString();
                if (code == "Unregistered" || code == "InvalidArgument" || code == "SenderIdMismatch")
                {
                    device.IsActive = false;
                    _logger.LogWarning("Device {Token} marked as inactive ({Code})", device.DeviceToken, code);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send to device {Token}", device.DeviceToken);
                failureCount++;
            }
        }

        // حفظ تغييرات الأجهزة غير النشطة
        await db.SaveChangesAsync();

        var status = failureCount == 0 ? "sent" : (successCount > 0 ? "partial" : "failed");

        var log = new NotificationLog
        {
            NotificationType = request.NotificationType,
            Title = request.Title,
            Body = request.Body,
            DataPayload = request.Data != null ? JsonSerializer.Serialize(request.Data) : string.Empty,
            TargetType = targetType,
            TargetId = targetId,
            DeviceCount = deviceCount,
            SuccessCount = successCount,
            FailureCount = failureCount,
            Status = status,
            SentBy = sentBy,
            RelatedOfferId = request.RelatedOfferId,
            RelatedCouponId = request.RelatedCouponId,
            SentAt = DateTime.Now
        };

        db.NotificationLogs.Add(log);
        await db.SaveChangesAsync();

        _logger.LogInformation(
            "Notification '{Title}' sent: {Success}/{Total} devices ({Status})",
            request.Title, successCount, deviceCount, status);

        return log;
    }
}
