using AlNeda.Core.Entities;
using AlNeda.Data;
using AlNeda.Services;
using Microsoft.EntityFrameworkCore;

namespace AlNeda.API.BackgroundServices;

/// <summary>
/// ⏰ خدمة الخلفية للنشر التلقائي للعروض المجدولة
/// - تحويل العروض من "scheduled" إلى "published" عند وقت البدء
/// - تحويل العروض المنتهية إلى "ended"
/// - إرسال إشعارات عند النشر التلقائي
/// </summary>
public class OfferAutoPublishService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OfferAutoPublishService> _logger;
    private const int CheckIntervalSeconds = 60; // فحص كل دقيقة

    public OfferAutoPublishService(
        IServiceProvider serviceProvider,
        ILogger<OfferAutoPublishService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("🚀 OfferAutoPublishService started — checking every {Interval}s", CheckIntervalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PublishScheduledOffersAsync(stoppingToken);
                await EndExpiredOffersAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Error in OfferAutoPublishService execution");
            }

            await Task.Delay(TimeSpan.FromSeconds(CheckIntervalSeconds), stoppingToken);
        }
    }

    /// <summary>نشر العروض المجدولة التي حان وقتها</summary>
    private async Task PublishScheduledOffersAsync(CancellationToken ct)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>();
        var notifications = scope.ServiceProvider.GetRequiredService<INotificationsService>();

        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var now = DateTime.Now;

        // البحث عن العروض المجدولة التي حان وقتها
        var scheduledOffers = await db.MarketingOffers
            .Include(o => o.OfferProducts)
            .Where(o => o.Status == "scheduled" && o.StartsAt <= now && o.EndsAt > now)
            .ToListAsync(ct);

        if (scheduledOffers.Count == 0)
            return;

        foreach (var offer in scheduledOffers)
        {
            offer.Status = "published";
            offer.UpdatedAt = now;
            _logger.LogInformation(
                "📢 Auto-published scheduled offer #{OfferId}: '{Title}'",
                offer.Id, offer.Title);
        }

        await db.SaveChangesAsync(ct);

        // إرسال إشعارات لكل عرض تم نشره
        foreach (var offer in scheduledOffers)
        {
            try
            {
                await notifications.SendOfferPublishedNotificationAsync(offer.Id);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send notification for auto-published offer #{OfferId}", offer.Id);
            }
        }
    }

    /// <summary>إنهاء العروض التي انتهت مدتها</summary>
    private async Task EndExpiredOffersAsync(CancellationToken ct)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>();

        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var now = DateTime.Now;

        // البحث عن العروض النشطة التي انتهت
        var expiredOffers = await db.MarketingOffers
            .Where(o => (o.Status == "published" || o.Status == "scheduled") && o.EndsAt <= now)
            .ToListAsync(ct);

        if (expiredOffers.Count == 0)
            return;

        foreach (var offer in expiredOffers)
        {
            offer.Status = "ended";
            offer.UpdatedAt = now;
            _logger.LogInformation(
                "⏰ Auto-ended expired offer #{OfferId}: '{Title}' (ended at {End})",
                offer.Id, offer.Title, offer.EndsAt);
        }

        await db.SaveChangesAsync(ct);
    }
}
