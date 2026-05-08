using AlNeda.Core.Entities;
using AlNeda.Data;
using AlNeda.Services.Export;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AlNeda.API.Controllers;

[ApiController]
[Route("api/reports")]
[Authorize]
public class ReportsController : ControllerBase
{
    private readonly IReportExporter _reporter;
    private readonly IDbContextFactory<AppDbContext> _contextFactory;
    private readonly ILogger<ReportsController> _logger;

    public ReportsController(
        IReportExporter reporter,
        IDbContextFactory<AppDbContext> contextFactory,
        ILogger<ReportsController> logger)
    {
        _reporter = reporter;
        _contextFactory = contextFactory;
        _logger = logger;
    }

    [HttpGet("daily-sales")]
    public async Task<IActionResult> GetDailySalesCsv()
    {
        var data = await GenerateDailySalesAsync();
        var ms = new MemoryStream();
        await _reporter.WriteCsvToStreamAsync(data, ms);
        ms.Position = 0;
        return File(ms, "text/csv", $"daily_sales_{DateTime.Now:yyyyMMdd}.csv");
    }

    [HttpGet("daily-sales/excel")]
    public async Task<IActionResult> GetDailySalesExcel()
    {
        var data = await GenerateDailySalesAsync();
        var ms = new MemoryStream();
        await _reporter.WriteExcelToStreamAsync(data, ms);
        ms.Position = 0;
        return File(ms, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"daily_sales_{DateTime.Now:yyyyMMdd}.xlsx");
    }

    [HttpGet("monthly-sales")]
    public async Task<IActionResult> GetMonthlySalesCsv()
    {
        var data = await GenerateMonthlySalesAsync();
        var ms = new MemoryStream();
        await _reporter.WriteCsvToStreamAsync(data, ms);
        ms.Position = 0;
        return File(ms, "text/csv", $"monthly_sales_{DateTime.Now:yyyyMM}.csv");
    }

    [HttpGet("monthly-sales/excel")]
    public async Task<IActionResult> GetMonthlySalesExcel()
    {
        var data = await GenerateMonthlySalesAsync();
        var ms = new MemoryStream();
        await _reporter.WriteExcelToStreamAsync(data, ms);
        ms.Position = 0;
        return File(ms, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"monthly_sales_{DateTime.Now:yyyyMM}.xlsx");
    }

    [HttpGet("top-products")]
    public async Task<IActionResult> GetTopProductsCsv()
    {
        var data = await GenerateTopProductsAsync();
        var ms = new MemoryStream();
        await _reporter.WriteCsvToStreamAsync(data, ms);
        ms.Position = 0;
        return File(ms, "text/csv", $"top_products_{DateTime.Now:yyyyMMdd}.csv");
    }

    [HttpGet("top-products/excel")]
    public async Task<IActionResult> GetTopProductsExcel()
    {
        var data = await GenerateTopProductsAsync();
        var ms = new MemoryStream();
        await _reporter.WriteExcelToStreamAsync(data, ms);
        ms.Position = 0;
        return File(ms, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"top_products_{DateTime.Now:yyyyMMdd}.xlsx");
    }

    [HttpGet("stock")]
    public async Task<IActionResult> GetStockCsv()
    {
        var data = await GenerateStockAsync();
        var ms = new MemoryStream();
        await _reporter.WriteCsvToStreamAsync(data, ms);
        ms.Position = 0;
        return File(ms, "text/csv", $"stock_{DateTime.Now:yyyyMMdd}.csv");
    }

    [HttpGet("stock/excel")]
    public async Task<IActionResult> GetStockExcel()
    {
        var data = await GenerateStockAsync();
        var ms = new MemoryStream();
        await _reporter.WriteExcelToStreamAsync(data, ms);
        ms.Position = 0;
        return File(ms, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"stock_{DateTime.Now:yyyyMMdd}.xlsx");
    }

    [HttpGet("debts")]
    public async Task<IActionResult> GetDebtsCsv()
    {
        var data = await GenerateDebtsAsync();
        var ms = new MemoryStream();
        await _reporter.WriteCsvToStreamAsync(data, ms);
        ms.Position = 0;
        return File(ms, "text/csv", $"debts_{DateTime.Now:yyyyMMdd}.csv");
    }

    [HttpGet("debts/excel")]
    public async Task<IActionResult> GetDebtsExcel()
    {
        var data = await GenerateDebtsAsync();
        var ms = new MemoryStream();
        await _reporter.WriteExcelToStreamAsync(data, ms);
        ms.Position = 0;
        return File(ms, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"debts_{DateTime.Now:yyyyMMdd}.xlsx");
    }

    [HttpGet("top-pharmacies")]
    public async Task<IActionResult> GetTopPharmaciesCsv()
    {
        var data = await GenerateTopPharmaciesAsync();
        var ms = new MemoryStream();
        await _reporter.WriteCsvToStreamAsync(data, ms);
        ms.Position = 0;
        return File(ms, "text/csv", $"top_pharmacies_{DateTime.Now:yyyyMMdd}.csv");
    }

    [HttpGet("top-pharmacies/excel")]
    public async Task<IActionResult> GetTopPharmaciesExcel()
    {
        var data = await GenerateTopPharmaciesAsync();
        var ms = new MemoryStream();
        await _reporter.WriteExcelToStreamAsync(data, ms);
        ms.Position = 0;
        return File(ms, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"top_pharmacies_{DateTime.Now:yyyyMMdd}.xlsx");
    }

    [HttpGet("expiry")]
    public async Task<IActionResult> GetExpiryCsv()
    {
        var data = await GenerateExpiryAsync();
        var ms = new MemoryStream();
        await _reporter.WriteCsvToStreamAsync(data, ms);
        ms.Position = 0;
        return File(ms, "text/csv", $"expiry_{DateTime.Now:yyyyMMdd}.csv");
    }

    [HttpGet("expiry/excel")]
    public async Task<IActionResult> GetExpiryExcel()
    {
        var data = await GenerateExpiryAsync();
        var ms = new MemoryStream();
        await _reporter.WriteExcelToStreamAsync(data, ms);
        ms.Position = 0;
        return File(ms, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"expiry_{DateTime.Now:yyyyMMdd}.xlsx");
    }

    [HttpPost("export-all")]
    public async Task<IActionResult> ExportAll()
    {
        if (_reporter is ReportExporterService exporter)
        {
            await exporter.AutoExportAllReportsAsync();
            return Ok(new { message = "تم تصدير جميع التقارير بنجاح" });
        }
        return BadRequest(new { message = "خدمة التصدير غير متوفرة" });
    }

    // ── Data Generators ──

    private async Task<System.Data.DataTable> GenerateDailySalesAsync()
    {
        await using var db = await _contextFactory.CreateDbContextAsync();
        var today = DateTime.Today;
        var orders = await db.Orders.Include(o => o.Pharmacy)
            .Where(o => o.CreatedAt >= today && o.CreatedAt < today.AddDays(1) && o.Status != "cancelled").ToListAsync();

        var dt = CreateTable("م", "رقم الطلب", "الصيدلية", "الإجمالي", "الحالة", "التاريخ");
        int i = 1;
        foreach (var o in orders)
            dt.Rows.Add(i++, o.OrderNumber, o.Pharmacy?.Name ?? "-", o.FinalTotal, o.Status, o.CreatedAt.ToString("yyyy-MM-dd"));
        return dt;
    }

    private async Task<System.Data.DataTable> GenerateMonthlySalesAsync()
    {
        await using var db = await _contextFactory.CreateDbContextAsync();
        var start = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        var end = start.AddMonths(1);
        var orders = await db.Orders.Include(o => o.Pharmacy)
            .Where(o => o.CreatedAt >= start && o.CreatedAt < end && o.Status != "cancelled").ToListAsync();

        var dt = CreateTable("م", "رقم الطلب", "الصيدلية", "الإجمالي", "الحالة", "التاريخ");
        int i = 1;
        foreach (var o in orders)
            dt.Rows.Add(i++, o.OrderNumber, o.Pharmacy?.Name ?? "-", o.FinalTotal, o.Status, o.CreatedAt.ToString("yyyy-MM-dd"));
        return dt;
    }

    private async Task<System.Data.DataTable> GenerateTopProductsAsync()
    {
        await using var db = await _contextFactory.CreateDbContextAsync();
        var items = await db.OrderItems.Include(i => i.Product).Include(i => i.Order)
            .Where(i => i.Order != null && i.Order.Status != "cancelled").ToListAsync();

        var grouped = items.GroupBy(i => new { i.ProductId, Name = i.Product?.Name ?? "غير محدد" })
            .Select(g => new { g.Key.ProductId, g.Key.Name, TotalQuantity = g.Sum(i => i.Quantity), TotalRevenue = g.Sum(i => i.TotalPrice) })
            .OrderByDescending(p => p.TotalQuantity).ToList();

        var dt = CreateTable("م", "المنتج", "الكمية", "الإيرادات");
        int i = 1;
        foreach (var p in grouped)
            dt.Rows.Add(i++, p.Name, p.TotalQuantity, p.TotalRevenue);
        return dt;
    }

    private async Task<System.Data.DataTable> GenerateStockAsync()
    {
        await using var db = await _contextFactory.CreateDbContextAsync();
        var products = await db.Products.OrderBy(p => p.Quantity).ToListAsync();
        var dt = CreateTable("م", "المنتج", "الفئة", "الكمية", "سعر الوحدة", "القيمة", "الحالة");
        int i = 1;
        foreach (var p in products)
        {
            var status = p.Quantity == 0 ? "نفذ" : p.Quantity <= 10 ? "منخفض" : "متوفر";
            dt.Rows.Add(i++, p.Name, p.Category ?? "-", p.Quantity, p.UnitPrice, p.Quantity * p.UnitPrice, status);
        }
        return dt;
    }

    private async Task<System.Data.DataTable> GenerateDebtsAsync()
    {
        await using var db = await _contextFactory.CreateDbContextAsync();
        var pharmacies = await db.Pharmacies.Where(p => p.Balance > 0).OrderByDescending(p => p.Balance).ToListAsync();
        var dt = CreateTable("م", "الاسم", "الرصيد", "الحالة");
        int i = 1;
        foreach (var p in pharmacies)
            dt.Rows.Add(i++, p.Name, p.Balance, p.AccountStatus);
        return dt;
    }

    private async Task<System.Data.DataTable> GenerateTopPharmaciesAsync()
    {
        await using var db = await _contextFactory.CreateDbContextAsync();
        var pharmacies = await db.Pharmacies.Include(p => p.Orders).ToListAsync();
        var grouped = pharmacies.Select(p => new
        {
            p.Name,
            OrderCount = p.Orders?.Count(o => o.Status != "cancelled") ?? 0,
            TotalRevenue = p.Orders?.Where(o => o.Status != "cancelled").Sum(o => o.FinalTotal) ?? 0
        }).OrderByDescending(p => p.TotalRevenue).ToList();

        var dt = CreateTable("م", "الصيدلية", "عدد الطلبات", "الإيرادات");
        int i = 1;
        foreach (var p in grouped)
            dt.Rows.Add(i++, p.Name, p.OrderCount, p.TotalRevenue);
        return dt;
    }

    private async Task<System.Data.DataTable> GenerateExpiryAsync()
    {
        await using var db = await _contextFactory.CreateDbContextAsync();
        var threeMonthsFromNow = DateTime.Now.AddMonths(3);
        var products = await db.Products
            .Where(p => !string.IsNullOrEmpty(p.ExpiryDate))
            .ToListAsync();

        var withExpiry = products.Select(p =>
        {
            DateTime.TryParse(p.ExpiryDate, out var expiry);
            return new { Product = p, Expiry = expiry };
        }).Where(x => x.Expiry != default).OrderBy(x => x.Expiry).ToList();

        var dt = CreateTable("م", "المنتج", "تاريخ انتهاء الصلاحية", "الكمية", "الحالة");
        int i = 1;
        foreach (var x in withExpiry)
        {
            var status = x.Expiry <= DateTime.Now ? "منتهي" :
                         x.Expiry <= DateTime.Now.AddMonths(1) ? "ينتهي قريباً" :
                         x.Expiry <= threeMonthsFromNow ? "سينتهي" : "ساري";
            dt.Rows.Add(i++, x.Product.Name, x.Expiry.ToString("yyyy-MM-dd"), x.Product.Quantity, status);
        }
        return dt;
    }

    private static System.Data.DataTable CreateTable(params string[] columns)
    {
        var dt = new System.Data.DataTable();
        foreach (var col in columns)
            dt.Columns.Add(col);
        return dt;
    }
}
