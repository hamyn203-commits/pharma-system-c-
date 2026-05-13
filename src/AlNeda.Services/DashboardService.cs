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
        var startDate = DateTime.Today.AddDays(-29);
        var tomorrow = DateTime.Today.AddDays(1);

        var rawData = await db.Orders
            .Where(o => o.CreatedAt >= startDate && o.CreatedAt < tomorrow && o.Status != "cancelled")
            .GroupBy(o => o.CreatedAt.Date)
            .Select(g => new 
            {
                Date = g.Key,
                Total = g.Sum(o => o.FinalTotal)
            })
            .ToListAsync();

        var lookup = rawData.ToDictionary(x => x.Date, x => x.Total);

        return Enumerable.Range(0, 30)
            .Select(offset => startDate.AddDays(offset))
            .Select(date => new SalesDataPoint
            {
                Label = date.ToString("MM/dd"),
                Value = lookup.TryGetValue(date, out var total) ? total : 0
            })
            .ToList();
    }

    public async Task<DashboardAnalyticsDto> GetAnalyticsAsync()
    {
        await using var db = await _contextFactory.CreateDbContextAsync();

        var today = DateTime.Today;
        var tomorrow = today.AddDays(1);
        var currentStart = today.AddDays(-29);
        var previousStart = today.AddDays(-59);

        var currentOrdersQuery = db.Orders
            .Where(o => o.CreatedAt >= currentStart && o.CreatedAt < tomorrow && o.Status != "cancelled");

        var previousOrdersQuery = db.Orders
            .Where(o => o.CreatedAt >= previousStart && o.CreatedAt < currentStart && o.Status != "cancelled");

        var currentSales = await currentOrdersQuery.SumAsync(o => o.FinalTotal);
        var previousSales = await previousOrdersQuery.SumAsync(o => o.FinalTotal);
        var ordersCount = await currentOrdersQuery.CountAsync();
        var activeBuyingPharmacies = await currentOrdersQuery
            .Select(o => o.PharmacyId)
            .Distinct()
            .CountAsync();

        var dailyTotals = await currentOrdersQuery
            .GroupBy(o => o.CreatedAt.Date)
            .Select(g => new { Date = g.Key, Total = g.Sum(o => o.FinalTotal) })
            .ToListAsync();

        var bestDay = dailyTotals
            .OrderByDescending(x => x.Total)
            .FirstOrDefault();

        var growth = previousSales == 0
            ? currentSales > 0 ? 100 : 0
            : (double)((currentSales - previousSales) / previousSales * 100);

        var topProducts = await db.OrderItems
            .Where(i => i.Order.CreatedAt >= currentStart && i.Order.CreatedAt < tomorrow && i.Order.Status != "cancelled")
            .GroupBy(i => new { i.ProductId, i.Product.Name, i.Product.Category })
            .Select(g => new TopProductInsight
            {
                ProductName = g.Key.Name,
                Category = g.Key.Category,
                QuantitySold = g.Sum(i => i.Quantity),
                Revenue = g.Sum(i => i.TotalPrice),
                OrdersCount = g.Select(i => i.OrderId).Distinct().Count()
            })
            .ToListAsync();

        topProducts = topProducts
            .OrderByDescending(x => x.QuantitySold)
            .ThenByDescending(x => x.Revenue)
            .Take(5)
            .ToList();

        var topProduct = topProducts.FirstOrDefault() ?? new TopProductInsight();

        var topPharmacies = await currentOrdersQuery
            .GroupBy(o => new { o.PharmacyId, o.Pharmacy.Name, o.Pharmacy.Phone })
            .Select(g => new TopPharmacyInsight
            {
                PharmacyName = g.Key.Name,
                Phone = g.Key.Phone,
                TotalPurchases = g.Sum(o => o.FinalTotal),
                OrdersCount = g.Count(),
                LastOrderDate = g.Max(o => (DateTime?)o.CreatedAt)
            })
            .ToListAsync();

        topPharmacies = topPharmacies
            .OrderByDescending(x => x.TotalPurchases)
            .Take(5)
            .ToList();

        var topPharmacy = topPharmacies.FirstOrDefault() ?? new TopPharmacyInsight();

        var pharmacies = await db.Pharmacies
            .Select(p => new
            {
                p.Id,
                p.Name,
                p.Phone,
                LastOrderDate = p.Orders
                    .Where(o => o.Status != "cancelled")
                    .OrderByDescending(o => o.CreatedAt)
                    .Select(o => (DateTime?)o.CreatedAt)
                    .FirstOrDefault(),
                LastOrderValue = p.Orders
                    .Where(o => o.Status != "cancelled")
                    .OrderByDescending(o => o.CreatedAt)
                    .Select(o => o.FinalTotal)
                    .FirstOrDefault(),
                LastProductName = p.Orders
                    .Where(o => o.Status != "cancelled")
                    .OrderByDescending(o => o.CreatedAt)
                    .SelectMany(o => o.Items.OrderByDescending(i => i.Quantity).Select(i => i.Product.Name))
                    .FirstOrDefault()
            })
            .ToListAsync();

        var inactivePharmacies = pharmacies
            .Select(p => new InactivePharmacyInsight
            {
                PharmacyName = p.Name,
                Phone = p.Phone,
                LastOrderDate = p.LastOrderDate,
                DaysSinceLastOrder = p.LastOrderDate.HasValue
                    ? Math.Max(0, (today - p.LastOrderDate.Value.Date).Days)
                    : 999,
                InactivityLabel = p.LastOrderDate.HasValue
                    ? $"{Math.Max(0, (today - p.LastOrderDate.Value.Date).Days)} يوم"
                    : "لم تشتر",
                LastProductName = string.IsNullOrWhiteSpace(p.LastProductName) ? "لا يوجد" : p.LastProductName,
                LastOrderValue = p.LastOrderValue
            })
            .Where(p => !p.LastOrderDate.HasValue || p.DaysSinceLastOrder >= 30)
            .OrderByDescending(p => p.DaysSinceLastOrder)
            .ThenBy(p => p.PharmacyName)
            .Take(8)
            .ToList();

        return new DashboardAnalyticsDto
        {
            SalesMovement = new SalesMovementInsight
            {
                CurrentPeriodSales = currentSales,
                PreviousPeriodSales = previousSales,
                GrowthPercentage = Math.Round(growth, 1),
                OrdersCount = ordersCount,
                AverageOrderValue = ordersCount == 0 ? 0 : currentSales / ordersCount,
                ActiveBuyingPharmacies = activeBuyingPharmacies,
                SalesPerActivePharmacy = activeBuyingPharmacies == 0 ? 0 : currentSales / activeBuyingPharmacies,
                BestSalesDay = bestDay?.Date.ToString("dd MMM", new System.Globalization.CultureInfo("ar-EG")) ?? "-",
                BestSalesDayValue = bestDay?.Total ?? 0,
                TrendLabel = growth switch
                {
                    > 15 => "نمو قوي في حركة البيع",
                    > 0 => "نمو مستقر في المبيعات",
                    < -15 => "هبوط يحتاج متابعة عاجلة",
                    < 0 => "تراجع بسيط قابل للتحسين",
                    _ => "حركة ثابتة بدون تغير واضح"
                }
            },
            TopProduct = topProduct,
            TopProducts = topProducts,
            TopPharmacy = topPharmacy,
            TopPharmacies = topPharmacies,
            InactivePharmacies = inactivePharmacies
        };
    }
}
