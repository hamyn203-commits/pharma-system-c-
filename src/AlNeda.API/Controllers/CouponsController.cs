using AlNeda.API.Authorization;
using AlNeda.Core.Models;
using AlNeda.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AlNeda.API.Controllers;

/// <summary>
/// 🎫 التحكم بالكوبونات والأكواد الترويجية — منفصل تماماً
/// </summary>
[ApiController]
[Route("api/admin/coupons")]
[Authorize(Policy = AuthPolicies.AdminOnly)]
[Tags("Admin Coupons")]
public class CouponsController : ControllerBase
{
    private readonly ICouponService _couponService;
    private readonly IAuditService _audit;

    public CouponsController(ICouponService couponService, IAuditService audit)
    {
        _couponService = couponService;
        _audit = audit;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var coupons = await _couponService.GetAllAsync(page, pageSize);
        return Ok(coupons);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var coupon = await _couponService.GetByIdAsync(id);
        if (coupon == null)
            return NotFound(new ApiError { Message = "الكوبون غير موجود", Code = "COUPON_NOT_FOUND" });
        return Ok(coupon);
    }

    [HttpGet("code/{code}")]
    public async Task<IActionResult> GetByCode(string code)
    {
        var coupon = await _couponService.GetByCodeAsync(code);
        if (coupon == null)
            return NotFound(new ApiError { Message = "الكود غير موجود", Code = "COUPON_NOT_FOUND" });
        return Ok(coupon);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateCouponRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Code))
            return BadRequest(new ApiError { Message = "كود الخصم مطلوب", Code = "VALIDATION_ERROR" });

        try
        {
            var coupon = await _couponService.CreateAsync(request);
            await _audit.LogAsync(User.Identity?.Name ?? "system", "create", "Coupon", coupon.Id.ToString(),
                $"Created coupon '{coupon.Code}' ({coupon.DiscountPercent}% / {coupon.DiscountValue})");

            return Ok(coupon);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ApiError { Message = ex.Message, Code = "VALIDATION_ERROR" });
        }
    }

    [HttpPatch("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateCouponRequest request)
    {
        try
        {
            var coupon = await _couponService.UpdateAsync(id, request);
            await _audit.LogAsync(User.Identity?.Name ?? "system", "update", "Coupon", id.ToString(),
                $"Updated coupon '{coupon.Code}'");

            return Ok(coupon);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new ApiError { Message = ex.Message, Code = "COUPON_NOT_FOUND" });
        }
    }

    [HttpPost("validate")]
    [AllowAnonymous] // يمكن لأي مستخدم التحقق من صحة الكود عبر التطبيق
    public async Task<IActionResult> Validate([FromBody] ApplyCouponRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Code))
            return BadRequest(new ApiError { Message = "كود الخصم مطلوب", Code = "VALIDATION_ERROR" });

        if (request.OrderTotal <= 0)
            return BadRequest(new ApiError { Message = "قيمة الطلب غير صالحة", Code = "VALIDATION_ERROR" });

        var result = await _couponService.ValidateAndApplyAsync(request);
        return Ok(result);
    }

    [HttpPost("{id:int}/apply")]
    [AllowAnonymous]
    public async Task<IActionResult> Apply(int id, [FromBody] ApplyCouponRequest request)
    {
        if (request.OrderTotal <= 0)
            return BadRequest(new ApiError { Message = "قيمة الطلب غير صالحة", Code = "VALIDATION_ERROR" });

        request.Code = (await _couponService.GetByIdAsync(id))?.Code ?? string.Empty;
        if (string.IsNullOrWhiteSpace(request.Code))
            return NotFound(new ApiError { Message = "الكوبون غير موجود", Code = "COUPON_NOT_FOUND" });

        var result = await _couponService.ValidateAndApplyAsync(request);
        if (result.IsValid)
        {
            await _couponService.RecordUsageAsync(id);
            await _audit.LogAsync(User.Identity?.Name ?? "system", "apply", "Coupon", id.ToString(),
                $"Applied coupon '{result.Coupon?.Code}' — discount: {result.DiscountAmount}");
        }

        return Ok(result);
    }

    [HttpPost("{id:int}/status")]
    public async Task<IActionResult> SetStatus(int id, [FromBody] SetCouponStatusRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Status))
            return BadRequest(new ApiError { Message = "الحالة مطلوبة", Code = "VALIDATION_ERROR" });

        try
        {
            await _couponService.SetStatusAsync(id, request.Status);
            await _audit.LogAsync(User.Identity?.Name ?? "system", "status-change", "Coupon", id.ToString(),
                $"Changed coupon status to '{request.Status}'");

            var coupon = await _couponService.GetByIdAsync(id);
            return Ok(coupon);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new ApiError { Message = ex.Message, Code = "COUPON_NOT_FOUND" });
        }
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var coupon = await _couponService.GetByIdAsync(id);
            await _couponService.DeleteAsync(id);
            await _audit.LogAsync(User.Identity?.Name ?? "system", "delete", "Coupon", id.ToString(),
                $"Deleted coupon '{coupon?.Code}'");

            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new ApiError { Message = ex.Message, Code = "COUPON_NOT_FOUND" });
        }
    }
}

public class SetCouponStatusRequest
{
    public string Status { get; set; } = string.Empty;
}
