using System.Text;
using AlNeda.Core.Entities;
using AlNeda.Core.Models;
using Microsoft.Extensions.Logging;

namespace AlNeda.Services;

/// <summary>
/// 🖼️ خدمة توليد QR Code ومشاركة العروض — منفصلة تماماً
/// </summary>
public interface IQrCodeService
{
    /// <summary>توليد رابط QR Code للعرض</summary>
    Task<string> GenerateOfferQrCodeUrlAsync(int offerId);

    /// <summary>توليد نص QR Code (رابط عميق)</summary>
    string GenerateOfferDeepLink(int offerId, string? couponCode = null);

    /// <summary>إنشاء نص للمشاركة</summary>
    string GenerateShareText(MarketingOffer offer, string deepLink);

    /// <summary>التحقق من رابط QR صالح</summary>
    Task<OfferQrValidationResult> ValidateQrScanAsync(string scannedData);
}

public class QrCodeService : IQrCodeService
{
    private readonly ILogger<QrCodeService> _logger;
    private const string BaseUrl = "alneda://offer/"; // deep link scheme
    private const string WebBaseUrl = "https://alneda.app/offer/"; // web fallback

    public QrCodeService(ILogger<QrCodeService> logger)
    {
        _logger = logger;
    }

    public Task<string> GenerateOfferQrCodeUrlAsync(int offerId)
    {
        // سيتم تحويلها إلى رابط صورة QR حقيقي لاحقاً
        var deepLink = GenerateOfferDeepLink(offerId);
        var qrApiUrl = $"https://api.qrserver.com/v1/create-qr-code/?size=300x300&data={Uri.EscapeDataString(deepLink)}";
        _logger.LogInformation("Generated QR code URL for offer {OfferId}: {Url}", offerId, qrApiUrl);
        return Task.FromResult(qrApiUrl);
    }

    public string GenerateOfferDeepLink(int offerId, string? couponCode = null)
    {
        var link = $"{BaseUrl}{offerId}";
        if (!string.IsNullOrWhiteSpace(couponCode))
            link += $"?coupon={Uri.EscapeDataString(couponCode)}";
        return link;
    }

    public string GenerateShareText(MarketingOffer offer, string deepLink)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"🎉 *{offer.Title}*");

        if (!string.IsNullOrWhiteSpace(offer.Subtitle))
            sb.AppendLine(offer.Subtitle);

        if (offer.DiscountPercent.HasValue && offer.DiscountPercent.Value > 0)
            sb.AppendLine($"🔥 خصم {offer.DiscountPercent}%");

        if (offer.NewPrice.HasValue && offer.NewPrice.Value > 0)
            sb.AppendLine($"💰 السعر: {offer.NewPrice.Value:N2} ج.م");

        sb.AppendLine();
        sb.AppendLine($"📱 حمل التطبيق واستفد من العرض:");
        sb.AppendLine(deepLink);

        if (!string.IsNullOrWhiteSpace(offer.Description))
        {
            sb.AppendLine();
            sb.AppendLine(offer.Description);
        }

        return sb.ToString();
    }

    public Task<OfferQrValidationResult> ValidateQrScanAsync(string scannedData)
    {
        var result = new OfferQrValidationResult
        {
            IsValid = false,
            Message = "رابط غير صالح"
        };

        if (string.IsNullOrWhiteSpace(scannedData))
            return Task.FromResult(result);

        // تحليل الرابط الممسوح
        if (scannedData.StartsWith(BaseUrl, StringComparison.OrdinalIgnoreCase))
        {
            var path = scannedData[BaseUrl.Length..];
            var parts = path.Split('?');
            if (int.TryParse(parts[0], out var offerId))
            {
                result.IsValid = true;
                result.OfferId = offerId;
                result.Message = $"تم التعرف على العرض #{offerId}";

                if (parts.Length > 1)
                {
                    var query = parts[1];
                    if (query.StartsWith("coupon=", StringComparison.OrdinalIgnoreCase))
                    {
                        result.CouponCode = Uri.UnescapeDataString(query[7..]);
                        result.Message += $" مع كود خصم: {result.CouponCode}";
                    }
                }
            }
        }

        return Task.FromResult(result);
    }
}

public class OfferQrValidationResult
{
    public bool IsValid { get; set; }
    public int? OfferId { get; set; }
    public string? CouponCode { get; set; }
    public string Message { get; set; } = string.Empty;
}
