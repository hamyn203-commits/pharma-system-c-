using AlNeda.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace AlNeda.Services;

public class AlertService
{
    private readonly IDbContextFactory<Data.AppDbContext> _contextFactory;
    private const int MinStockThreshold = 10;
    private const int ExpiryDaysThreshold = 30;

    public AlertService(IDbContextFactory<Data.AppDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<List<Product>> GetLowStockProductsAsync()
    {
        await using var db = await _contextFactory.CreateDbContextAsync();
        return await db.Products
            .Where(p => p.Quantity < MinStockThreshold && p.IsActive == 1)
            .OrderBy(p => p.Quantity)
            .ToListAsync();
    }

    public async Task<List<Product>> GetExpiringProductsAsync()
    {
        await using var db = await _contextFactory.CreateDbContextAsync();
        var threshold = DateTime.Now.AddDays(ExpiryDaysThreshold);
        var now = DateTime.Now;

        var products = await db.Products
            .Where(p => p.ExpiryDate != null && p.ExpiryDate != "" && p.IsActive == 1)
            .ToListAsync();

        return products
            .Select(p => new { Product = p, IsValid = DateTime.TryParse(p.ExpiryDate, out var d), Date = d })
            .Where(x => x.IsValid && x.Date <= threshold && x.Date >= now)
            .OrderBy(x => x.Date)
            .Select(x => x.Product)
            .ToList();
    }

    public async Task<List<Product>> GetExpiredProductsAsync()
    {
        await using var db = await _contextFactory.CreateDbContextAsync();
        var now = DateTime.Now;

        var products = await db.Products
            .Where(p => p.ExpiryDate != null && p.ExpiryDate != "" && p.IsActive == 1)
            .ToListAsync();

        return products
            .Select(p => new { Product = p, IsValid = DateTime.TryParse(p.ExpiryDate, out var d), Date = d })
            .Where(x => x.IsValid && x.Date < now)
            .OrderBy(x => x.Date)
            .Select(x => x.Product)
            .ToList();
    }

    public async Task<(int lowStock, int expiring, int expired)> GetAlertCountsAsync()
    {
        var lowStock = await GetLowStockProductsAsync();
        var expiring = await GetExpiringProductsAsync();
        var expired = await GetExpiredProductsAsync();

        return (lowStock.Count, expiring.Count, expired.Count);
    }

    public async Task<string> GenerateAlertSummaryAsync()
    {
        var (lowStock, expiring, expired) = await GetAlertCountsAsync();

        if (lowStock == 0 && expiring == 0 && expired == 0)
            return "";

        var summary = new List<string>();
        if (lowStock > 0) summary.Add($"مخزون منخفض: {lowStock} منتج");
        if (expiring > 0) summary.Add($"تنتهي قريباً: {expiring} منتج");
        if (expired > 0) summary.Add($"منتهي الصلاحية: {expired} منتج");

        return string.Join(" | ", summary);
    }
}