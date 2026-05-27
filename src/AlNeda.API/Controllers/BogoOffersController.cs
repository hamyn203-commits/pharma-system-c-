using AlNeda.API.Authorization;
using AlNeda.Core.Models;
using AlNeda.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AlNeda.API.Controllers;

/// <summary>
/// 🛒 التحكم بعروض "اشتري X واحصل على Y مجاناً" (BOGO) — منفصل تماماً
/// </summary>
[ApiController]
[Route("api/admin/bogo")]
[Authorize(Policy = AuthPolicies.AdminOnly)]
[Tags("Admin BOGO Offers")]
public class BogoOffersController : ControllerBase
{
    private readonly IBogoOfferService _bogoService;
    private readonly IAuditService _audit;

    public BogoOffersController(IBogoOfferService bogoService, IAuditService audit)
    {
        _bogoService = bogoService;
        _audit = audit;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var rules = await _bogoService.GetAllRulesAsync(page, pageSize);
        return Ok(rules);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var rule = await _bogoService.GetRuleByIdAsync(id);
        if (rule == null)
            return NotFound(new ApiError { Message = "قاعدة BOGO غير موجودة", Code = "BOGO_NOT_FOUND" });
        return Ok(rule);
    }

    [HttpGet("by-offer/{offerId:int}")]
    public async Task<IActionResult> GetByOfferId(int offerId)
    {
        var rule = await _bogoService.GetRuleByOfferIdAsync(offerId);
        if (rule == null)
            return NotFound(new ApiError { Message = "لا توجد قاعدة BOGO لهذا العرض", Code = "BOGO_NOT_FOUND" });
        return Ok(rule);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateBogoRuleRequest request)
    {
        if (request.MarketingOfferId <= 0)
            return BadRequest(new ApiError { Message = "معرف العرض مطلوب", Code = "VALIDATION_ERROR" });
        if (request.BuyQuantity < 1 || request.FreeQuantity < 1)
            return BadRequest(new ApiError { Message = "عدد القطع المشتراة والمجانية يجب أن يكون 1 على الأقل", Code = "VALIDATION_ERROR" });

        try
        {
            var rule = await _bogoService.CreateRuleAsync(request);
            await _audit.LogAsync(User.Identity?.Name ?? "system", "create", "BogoRule", rule.Id.ToString(),
                $"Created BOGO rule: Buy {rule.BuyQuantity} Get {rule.FreeQuantity} Free for offer #{rule.MarketingOfferId}");

            return Ok(rule);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ApiError { Message = ex.Message, Code = "VALIDATION_ERROR" });
        }
    }

    [HttpPatch("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateBogoRuleRequest request)
    {
        try
        {
            var rule = await _bogoService.UpdateRuleAsync(id, request);
            await _audit.LogAsync(User.Identity?.Name ?? "system", "update", "BogoRule", id.ToString(),
                $"Updated BOGO rule #{id}");

            return Ok(rule);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new ApiError { Message = ex.Message, Code = "BOGO_NOT_FOUND" });
        }
    }

    [HttpPost("calculate/{offerId:int}")]
    [AllowAnonymous] // يستخدم من التطبيق
    public async Task<IActionResult> Calculate(int offerId, [FromBody] BogoCalculationRequest request)
    {
        if (request.CartItems.Count == 0)
            return BadRequest(new ApiError { Message = "السلة فارغة", Code = "VALIDATION_ERROR" });

        var result = await _bogoService.ApplyBogoAsync(offerId, request.CartItems);
        return Ok(result);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            await _bogoService.DeleteRuleAsync(id);
            await _audit.LogAsync(User.Identity?.Name ?? "system", "delete", "BogoRule", id.ToString(),
                $"Deleted BOGO rule #{id}");

            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new ApiError { Message = ex.Message, Code = "BOGO_NOT_FOUND" });
        }
    }
}

public class BogoCalculationRequest
{
    public List<BogoCartItem> CartItems { get; set; } = [];
}
