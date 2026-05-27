using AlNeda.Core.Models;
using AlNeda.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AlNeda.API.Controllers;

/// <summary>
/// 🤖 التحكم بالتوصيات الذكية للعروض — منفصل تماماً
/// </summary>
[ApiController]
[Route("api/mobile/recommendations")]
[Authorize(Roles = "pharmacy")]
[Tags("Mobile Recommendations")]
public class RecommendationsController : ControllerBase
{
    private readonly IRecommendationService _recommendationService;
    private readonly ILogger<RecommendationsController> _logger;

    public RecommendationsController(
        IRecommendationService recommendationService,
        ILogger<RecommendationsController> logger)
    {
        _recommendationService = recommendationService;
        _logger = logger;
    }

    /// <summary>
    /// 🏠 الصفحة الرئيسية: أفضل الاختيارات + الأكثر مشاهدة + الأحدث + تنتهي قريباً
    /// </summary>
    [HttpGet("home")]
    [ProducesResponseType(typeof(RecommendedOffersResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetHomeRecommendations()
    {
        var pharmacyId = GetPharmacyId();
        if (pharmacyId == null)
            return Unauthorized(new ApiError { Message = "الصيدلية غير مرتبطة", Code = "PHARMACY_NOT_LINKED" });

        var recommendations = await _recommendationService.GetHomePageRecommendationsAsync(pharmacyId.Value);

        _logger.LogInformation(
            "Home recommendations for pharmacy {PharmacyId}: {Top} top picks, {Trending} trending, {New} new, {Ending} ending soon",
            pharmacyId.Value,
            recommendations.TopPicks.Count,
            recommendations.Trending.Count,
            recommendations.NewArrivals.Count,
            recommendations.EndingSoon.Count);

        return Ok(recommendations);
    }

    /// <summary>
    /// ⭐ أفضل العروض المخصصة للصيدلية
    /// </summary>
    [HttpGet("personalized")]
    [ProducesResponseType(typeof(List<MarketingOfferDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPersonalized([FromQuery] string? deviceId = null)
    {
        var pharmacyId = GetPharmacyId();
        if (pharmacyId == null)
            return Unauthorized(new ApiError { Message = "الصيدلية غير مرتبطة", Code = "PHARMACY_NOT_LINKED" });

        var offers = await _recommendationService.GetPersonalizedOffersAsync(pharmacyId.Value, deviceId);
        return Ok(offers);
    }

    /// <summary>
    /// 🎯 العروض الموصى بها (عدد محدد)
    /// </summary>
    [HttpGet("top")]
    [ProducesResponseType(typeof(List<MarketingOfferDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTop([FromQuery] int count = 5)
    {
        var pharmacyId = GetPharmacyId();
        if (pharmacyId == null)
            return Unauthorized(new ApiError { Message = "الصيدلية غير مرتبطة", Code = "PHARMACY_NOT_LINKED" });

        var offers = await _recommendationService.GetRecommendedOffersAsync(pharmacyId.Value, count);
        return Ok(offers);
    }

    // ─── Helper ───────────────────────────────────────────────────
    private int? GetPharmacyId()
    {
        var claim = User.FindFirst("PharmacyId");
        if (claim == null || !int.TryParse(claim.Value, out var pharmacyId))
            return null;
        return pharmacyId;
    }
}
