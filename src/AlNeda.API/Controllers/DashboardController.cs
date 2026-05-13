using AlNeda.API.Authorization;
using AlNeda.Core.Models;
using AlNeda.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AlNeda.API.Controllers;

[ApiController]
[Route("api/dashboard")]
[Authorize(Policy = AuthPolicies.Staff)]
[Tags("Dashboard")]
public class DashboardController : ControllerBase
{
    private readonly DashboardService _dashboardService;
    private readonly AlertService _alertService;

    public DashboardController(DashboardService dashboardService, AlertService alertService)
    {
        _dashboardService = dashboardService;
        _alertService = alertService;
    }

    [HttpGet("stats")]
    public async Task<IActionResult> GetStats()
    {
        var stats = await _dashboardService.GetStatsAsync();
        return Ok(stats);
    }

    [HttpGet("sales/last30")]
    public async Task<IActionResult> GetLast30DaysSales()
    {
        var sales = await _dashboardService.GetLast30DaysSalesAsync();
        return Ok(sales);
    }

    [HttpGet("analytics")]
    public async Task<IActionResult> GetAnalytics()
    {
        var analytics = await _dashboardService.GetAnalyticsAsync();
        return Ok(analytics);
    }

    [HttpGet("alerts/low-stock")]
    public async Task<IActionResult> GetLowStock()
    {
        var products = await _alertService.GetLowStockProductsAsync();
        var dto = products.Select(p => new ProductAlertDto
        {
            Id = p.Id,
            Name = p.Name,
            Quantity = p.Quantity,
            ExpiryDate = p.ExpiryDate,
            Price = p.UnitPrice
        }).ToList();
        return Ok(dto);
    }

    [HttpGet("alerts/expiring")]
    public async Task<IActionResult> GetExpiring()
    {
        var products = await _alertService.GetExpiringProductsAsync();
        var dto = products.Select(p => new ProductAlertDto
        {
            Id = p.Id,
            Name = p.Name,
            Quantity = p.Quantity,
            ExpiryDate = p.ExpiryDate,
            Price = p.UnitPrice
        }).ToList();
        return Ok(dto);
    }

    [HttpGet("alerts/expired")]
    public async Task<IActionResult> GetExpired()
    {
        var products = await _alertService.GetExpiredProductsAsync();
        var dto = products.Select(p => new ProductAlertDto
        {
            Id = p.Id,
            Name = p.Name,
            Quantity = p.Quantity,
            ExpiryDate = p.ExpiryDate,
            Price = p.UnitPrice
        }).ToList();
        return Ok(dto);
    }
}

public class ProductAlertDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public string? ExpiryDate { get; set; }
    public decimal Price { get; set; }
}
