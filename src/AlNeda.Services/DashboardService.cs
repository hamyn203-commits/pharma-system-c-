using AlNeda.Core.Models;
using AlNeda.Data;
using Microsoft.EntityFrameworkCore;

namespace AlNeda.Services;

public class DashboardService
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;

    public DashboardService(IDbContextFactory<AppDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<DashboardStats> GetStatsAsync()
    {
        await using var db = await _contextFactory.CreateDbContextAsync();

        var stats = new DashboardStats();

        // 1. Total Receivables (Balance of all pharmacies)
        stats.TotalReceivables = await db.Pharmacies.SumAsync(p => p.Balance);

        // 2. Active Orders (not delivered and not cancelled)
        stats.ActiveOrders = await db.Orders.CountAsync(o => o.Status != "delivered" && o.Status != "cancelled");

        // 3. Total Pharmacies
        stats.TotalPharmacies = await db.Pharmacies.CountAsync();

        // 4. Total Products
        stats.TotalProducts = await db.Products.CountAsync();

        // 5. Daily Sales (Orders created today)
        var today = DateTime.Today;
        stats.DailySales = await db.Orders
            .Where(o => o.CreatedAt >= today && o.Status != "cancelled")
            .SumAsync(o => o.FinalTotal);

        // 6. Monthly Sales (Orders created this month)
        var firstDayOfMonth = new DateTime(today.Year, today.Month, 1);
        stats.MonthlySales = await db.Orders
            .Where(o => o.CreatedAt >= firstDayOfMonth && o.Status != "cancelled")
            .SumAsync(o => o.FinalTotal);

        return stats;
    }

    public async Task<List<SalesDataPoint>> GetLast30DaysSalesAsync()
    {
        await using var db = await _contextFactory.CreateDbContextAsync();
        var thirtyDaysAgo = DateTime.Today.AddDays(-30);

        var rawData = await db.Orders
            .Where(o => o.CreatedAt >= thirtyDaysAgo && o.Status != "cancelled")
            .GroupBy(o => o.CreatedAt.Date)
            .Select(g => new 
            {
                Date = g.Key,
                Total = g.Sum(o => o.FinalTotal)
            })
            .ToListAsync();

        return rawData
            .Select(x => new SalesDataPoint
            {
                Label = x.Date.ToString("MM/dd"),
                Value = x.Total
            })
            .OrderBy(x => x.Label)
            .ToList();
    }
}
