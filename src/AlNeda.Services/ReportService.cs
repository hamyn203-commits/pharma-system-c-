using AlNeda.Core.Entities;
using AlNeda.Core.Models;
using AlNeda.Data;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Text;

namespace AlNeda.Services;

public class ReportService
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;

    public ReportService(IDbContextFactory<AppDbContext> contextFactory) => _contextFactory = contextFactory;

    public async Task<string> GetDailySalesReportAsync(DateTime? date = null)
    {
        date ??= DateTime.Today;
        await using var db = await _contextFactory.CreateDbContextAsync();
        var start = date.Value.Date;
        var end = start.AddDays(1);

        var orders = await db.Orders
            .Where(o => o.CreatedAt >= start && o.CreatedAt < end && o.Status != "cancelled")
            .Select(o => new
            {
                o.OrderNumber,
                o.FinalTotal,
                o.Status,
                o.CreatedAt,
                PharmacyName = o.Pharmacy != null ? o.Pharmacy.Name : "-",
                TotalQuantity = o.Items.Sum(i => i.Quantity)
            })
            .ToListAsync();

        var payments = await db.Payments.Where(p => p.Date >= start && p.Date < end).ToListAsync();

        var sb = new StringBuilder();
        sb.AppendLine($"╔══════════════════════════════════════════════════════════╗");
        sb.AppendLine($"║          تقرير المبيعات اليومية - {date:yyyy-MM-dd}              ║");
        sb.AppendLine($"╠══════════════════════════════════════════════════════════╣");

        if (!orders.Any())
        {
            sb.AppendLine($"║  لا توجد طلبات في هذا اليوم                                 ║");
        }
        else
        {
            var totalSales = orders.Sum(o => o.FinalTotal);
            var totalOrders = orders.Count;
            var totalItems = orders.Sum(o => o.TotalQuantity);
            var avgOrder = totalOrders > 0 ? totalSales / totalOrders : 0;

            sb.AppendLine($"║  عدد الطلبات:        {totalOrders,10}                        ║");
            sb.AppendLine($"║  إجمالي المبيعات:   {totalSales,10:N2}                     ║");
            sb.AppendLine($"║  عدد المنتجات:      {totalItems,10}                        ║");
            sb.AppendLine($"║  متوسط الطلب:       {avgOrder,10:N2}                     ║");

            sb.AppendLine($"╠══════════════════════════════════════════════════════════╣");
            sb.AppendLine($"║  الطلبات:                                                ║");
            foreach (var order in orders.Take(20))
            {
                sb.AppendLine($"║  #{order.OrderNumber,-20} {order.FinalTotal,10:N2}             ║");
            }
            if (orders.Count > 20)
                sb.AppendLine($"║  ... و {orders.Count - 20} طلبات أخرى                               ║");
        }

        sb.AppendLine($"╠══════════════════════════════════════════════════════════╣");
        sb.AppendLine($"║  التحصيلات:                                               ║");
        var totalPayments = payments.Sum(p => p.Amount);
        sb.AppendLine($"║  إجمالي التحصيل:   {totalPayments,10:N2}                     ║");
        sb.AppendLine($"║  عدد التحصيلات:   {payments.Count,10}                        ║");

        sb.AppendLine($"╚══════════════════════════════════════════════════════════╝");
        return sb.ToString();
    }

    public async Task<string> GetMonthlySalesReportAsync(int? year = null, int? month = null)
    {
        year ??= DateTime.Today.Year;
        month ??= DateTime.Today.Month;
        await using var db = await _contextFactory.CreateDbContextAsync();

        var start = new DateTime(year.Value, month.Value, 1);
        var end = start.AddMonths(1);

        var orders = await db.Orders
            .Where(o => o.CreatedAt >= start && o.CreatedAt < end && o.Status != "cancelled")
            .Select(o => new
            {
                o.FinalTotal,
                o.Status
            })
            .ToListAsync();

        var sb = new StringBuilder();
        var monthName = new DateTime(year.Value, month.Value, 1).ToString("MMMM yyyy", new System.Globalization.CultureInfo("ar-SA"));
        sb.AppendLine($"╔══════════════════════════════════════════════════════════╗");
        sb.AppendLine($"║          تقرير المبيعات الشهرية - {monthName}           ║");
        sb.AppendLine($"╠══════════════════════════════════════════════════════════╣");

        var totalSales = orders.Sum(o => o.FinalTotal);
        var totalOrders = orders.Count;
        var avgDaily = totalOrders > 0 ? totalSales / DateTime.DaysInMonth(year.Value, month.Value) : 0;

        sb.AppendLine($"║  إجمالي المبيعات:   {totalSales,10:N2}                     ║");
        sb.AppendLine($"║  عدد الطلبات:      {totalOrders,10}                        ║");
        sb.AppendLine($"║  متوسط يومي:      {avgDaily,10:N2}                     ║");

        sb.AppendLine($"╠══════════════════════════════════════════════════════════╣");
        sb.AppendLine($"║  المبيعات حسب الحالة:                                    ║");

        var byStatus = orders.GroupBy(o => o.Status).Select(g => new { Status = g.Key, Count = g.Count(), Total = g.Sum(o => o.FinalTotal) });
        foreach (var s in byStatus)
        {
            var statusName = GetStatusName(s.Status);
            sb.AppendLine($"║  {statusName,-20} {s.Count,5} ({s.Total:N2})          ║");
        }

        sb.AppendLine($"╚══════════════════════════════════════════════════════════╝");
        return sb.ToString();
    }

    public async Task<string> GetTopProductsReportAsync(int top = 20)
    {
        await using var db = await _contextFactory.CreateDbContextAsync();
        var productSalesData = await db.OrderItems
            .Where(i => i.Order != null && i.Order.Status != "cancelled")
            .Select(i => new
            {
                i.ProductId,
                ProductName = i.Product != null ? i.Product.Name : "غير محدد",
                ProductCategory = i.Product != null ? i.Product.Category : "غير محدد",
                i.Quantity,
                i.TotalPrice
            })
            .ToListAsync();

        var productSales = productSalesData
            .GroupBy(i => new { i.ProductId, Name = i.ProductName, Category = i.ProductCategory ?? "غير محدد" })
            .Select(g => new
            {
                g.Key.ProductId,
                g.Key.Name,
                g.Key.Category,
                TotalQuantity = g.Sum(i => i.Quantity),
                TotalRevenue = g.Sum(i => i.TotalPrice)
            })
            .OrderByDescending(p => p.TotalQuantity)
            .Take(top)
            .ToList();

        var sb = new StringBuilder();
        sb.AppendLine($"╔══════════════════════════════════════════════════════════╗");
        sb.AppendLine($"║          المنتجات الأكثر مبيعاً                             ║");
        sb.AppendLine($"╠════╦══════════════════════════════╦═══════╦═══════════╣");
        sb.AppendLine($"║ م  ║         اسم المنتج           ║ الكمية ║   الإيرادات ║");
        sb.AppendLine($"╠════╬══════════════════════════════╬═══════╬═══════════╣");

        int i = 1;
        foreach (var p in productSales)
        {
            sb.AppendLine($"║ {i,2} ║ {p.Name,-24} ║ {p.TotalQuantity,5} ║ {p.TotalRevenue,9:N2} ║");
            i++;
        }

        sb.AppendLine($"╚════╩══════════════════════════════╩═══════╩═══════════╝");
        return sb.ToString();
    }

    public async Task<string> GetTopPharmaciesReportAsync(int top = 20)
    {
        await using var db = await _contextFactory.CreateDbContextAsync();
        var pharmacyStats = db.Pharmacies
            .Select(p => new
            {
                p.Id,
                p.Name,
                p.Balance,
                p.AccountStatus,
                OrderCount = p.Orders.Where(o => o.Status != "cancelled").Count(),
                TotalSales = p.Orders.Where(o => o.Status != "cancelled").Sum(o => (decimal?)o.FinalTotal) ?? 0
            })
            .OrderByDescending(p => p.TotalSales)
            .Take(top)
            .ToList();

        var sb = new StringBuilder();
        sb.AppendLine($"╔══════════════════════════════════════════════════════════╗");
        sb.AppendLine($"║          الصيدليات الأعلى مبيعات                           ║");
        sb.AppendLine($"╠════╦══════════════════════════════╦═══════╦═══════════╦═══════════╣");
        sb.AppendLine($"║ م  ║           الاسم              ║ الطلبات║  المبيعات  ║   الرصيد    ║");
        sb.AppendLine($"╠════╬══════════════════════════════╬═══════╬═══════════╬═══════════╣");

        int i = 1;
        foreach (var p in pharmacyStats)
        {
            var balanceStr = p.Balance >= 0 ? $"{p.Balance:N2}" : $"({Math.Abs(p.Balance):N2})";
            sb.AppendLine($"║ {i,2} ║ {p.Name,-24} ║ {p.OrderCount,5} ║ {p.TotalSales,9:N2} ║ {balanceStr,9} ║");
            i++;
        }

        sb.AppendLine($"╚════╩══════════════════════════════╩═══════╩═══════════╩═══════════╝");
        return sb.ToString();
    }

    public async Task<string> GetExpiryReportAsync()
    {
        await using var db = await _contextFactory.CreateDbContextAsync();
        var today = DateTime.Today;
        var thirtyDays = today.AddDays(30);

        var products = db.Products
            .Where(p => !string.IsNullOrEmpty(p.ExpiryDate))
            .ToList()
            .Where(p => DateTime.TryParse(p.ExpiryDate, out var exp) && exp <= thirtyDays)
            .OrderBy(p => DateTime.TryParse(p.ExpiryDate, out var d) ? d : DateTime.MaxValue)
            .ToList();

        var sb = new StringBuilder();
        sb.AppendLine($"╔══════════════════════════════════════════════════════════╗");
        sb.AppendLine($"║          تقرير انتهاء الصلاحية                             ║");
        sb.AppendLine($"╠════╦══════════════════════════════╦═══════════════╦═══════╣");
        sb.AppendLine($"║ م  ║           المنتج              ║  تاريخ الانتهاء ║ الكمية ║");
        sb.AppendLine($"╠════╬══════════════════════════════╬═══════════════╬═══════╣");

        int i = 1;
        foreach (var p in products)
        {
            DateTime.TryParse(p.ExpiryDate, out var exp);
            var daysLeft = (exp - today).Days;
            var warning = daysLeft <= 0 ? "منتهي" : daysLeft <= 7 ? "عاجل" : "";
            sb.AppendLine($"║ {i,2} ║ {p.Name,-24} ║ {p.ExpiryDate,-13} ║ {p.Quantity,5} {warning} ║");
            i++;
        }

        sb.AppendLine($"╚════╩══════════════════════════════╩═══════════════╩═══════╝");
        return sb.ToString();
    }

    public async Task<string> GetDebtsReportAsync()
    {
        await using var db = await _contextFactory.CreateDbContextAsync();
        var pharmacies = db.Pharmacies
            .Where(p => p.Balance > 0 && p.AccountStatus == "active")
            .OrderByDescending(p => p.Balance)
            .ToList();

        var totalDebt = pharmacies.Sum(p => p.Balance);

        var sb = new StringBuilder();
        sb.AppendLine($"╔══════════════════════════════════════════════════════════╗");
        sb.AppendLine($"║          تقرير الصيدليات المدينة                            ║");
        sb.AppendLine($"╠════╦══════════════════════════════╦═══════════════╦═══════════╣");
        sb.AppendLine($"║ م  ║           الاسم              ║    الحالة      ║   الرصيد    ║");
        sb.AppendLine($"╠════╬══════════════════════════════╬═══════════════╬═══════════╣");

        int i = 1;
        foreach (var p in pharmacies)
        {
            var statusName = GetAccountStatusName(p.AccountStatus);
            sb.AppendLine($"║ {i,2} ║ {p.Name,-24} ║ {statusName,-11} ║ {p.Balance,9:N2} ║");
            i++;
        }

        sb.AppendLine($"╠════╩══════════════════════════════╩═══════════════╩═══════════╣");
        sb.AppendLine($"║                    الإجمالي: {totalDebt,17:N2} ║");
        sb.AppendLine($"╚══════════════════════════════════════════════════════════╝");
        return sb.ToString();
    }

    public async Task<string> GetStockReportAsync()
    {
        await using var db = await _contextFactory.CreateDbContextAsync();
        var products = db.Products.OrderBy(p => p.Quantity).ToList();

        var outOfStock = products.Count(p => p.Quantity == 0);
        var lowStock = products.Count(p => p.Quantity > 0 && p.Quantity <= 10);
        var totalValue = products.Sum(p => p.Quantity * p.UnitPrice);

        var sb = new StringBuilder();
        sb.AppendLine($"╔══════════════════════════════════════════════════════════╗");
        sb.AppendLine($"║          تقرير المخزون                                    ║");
        sb.AppendLine($"╠══════════════════════════════════════════════════════════╣");
        sb.AppendLine($"║  إجمالي المنتجات:        {products.Count,10}                   ║");
        sb.AppendLine($"║  نفذت من المخزون:      {outOfStock,10}                   ║");
        sb.AppendLine($"║  كمية منخفضة (<=10):   {lowStock,10}                   ║");
        sb.AppendLine($"║  القيمة الإجمالية:      {totalValue,10:N2}                   ║");
        sb.AppendLine($"╠══════════════════════════════════════════════════════════╣");
        sb.AppendLine($"║  المنتجات المنتهية أو منخفضة:                            ║");

        var critical = products.Where(p => p.Quantity <= 10).Take(30);
        foreach (var p in critical)
        {
            var status = p.Quantity == 0 ? "نفذ" : "منخفض";
            sb.AppendLine($"║  {p.Name,-28} {p.Quantity,5} ({status})        ║");
        }

        sb.AppendLine($"╚══════════════════════════════════════════════════════════╝");
        return sb.ToString();
    }

    public async Task<string> GetAccountStatementAsync(int pharmacyId)
    {
        await using var db = await _contextFactory.CreateDbContextAsync();
        var pharmacy = await db.Pharmacies.FindAsync(pharmacyId);
        if (pharmacy == null) return "الصيدلية غير موجودة";

        var orders = db.Orders.Where(o => o.PharmacyId == pharmacyId && o.Status != "cancelled")
            .OrderBy(o => o.CreatedAt).ToList();
        var payments = db.Payments.Where(p => p.PharmacyId == pharmacyId)
            .OrderBy(p => p.Date).ToList();
        var returns = db.Returns.Where(r => r.PharmacyId == pharmacyId)
            .OrderBy(r => r.CreatedAt).ToList();

        var sb = new StringBuilder();
        sb.AppendLine($"╔════════════════════════════════════════════════════════════════╗");
        sb.AppendLine($"║                    كشف حساب pharmacy: {pharmacy.Name}               ║");
        sb.AppendLine($"╠════════════════════════════════════════════════════════════════╣");

        decimal balance = 0;
        sb.AppendLine($"║  الرصيد الافتتاحي:                         {balance,15:N2} ║");
        sb.AppendLine($"╠════╦══════════╦══════════════════╦═════════╦════════════════╣");
        sb.AppendLine($"║ م  ║ التاريخ   ║      الوصف      ║  مدين  ║     دائن     ║");
        sb.AppendLine($"╠════╬══════════╬══════════════════╬═════════╬════════════════╣");

        var allTransactions = new List<(DateTime Date, string Type, string Desc, decimal Debit, decimal Credit, decimal RunningBalance)>();

        foreach (var o in orders)
        {
            balance += o.FinalTotal;
            allTransactions.Add((o.CreatedAt, "طلب", $"#{o.OrderNumber}", o.FinalTotal, 0, balance));
        }
        foreach (var p in payments)
        {
            balance -= p.Amount;
            allTransactions.Add((p.Date, "تحصيل", $"#{p.OrderId}", 0, p.Amount, balance));
        }
        foreach (var r in returns)
        {
            balance -= r.TotalAmount;
            allTransactions.Add((r.CreatedAt, "مرتجع", $"#{r.ReturnNumber}", 0, r.TotalAmount, balance));
        }

        allTransactions = allTransactions.OrderBy(t => t.Date).ToList();

        int i = 1;
        foreach (var t in allTransactions.Take(100))
        {
            var debitStr = t.Debit > 0 ? $"{t.Debit:N2}" : "-";
            var creditStr = t.Credit > 0 ? $"{t.Credit:N2}" : "-";
            sb.AppendLine($"║ {i,2} ║ {t.Date:yyyy-MM-dd} ║ {t.Desc,-14} ║ {debitStr,7} ║ {creditStr,10} ║");
            i++;
        }

        if (allTransactions.Count > 100)
            sb.AppendLine($"║ ... و {allTransactions.Count - 100} معاملة أخرى                                 ║");

        sb.AppendLine($"╠════╩══════════╩══════════════════╩═════════╩════════════════╣");
        sb.AppendLine($"║                    الرصيد النهائي: {balance,21:N2} ║");
        sb.AppendLine($"╚════════════════════════════════════════════════════════════════╝");

        return sb.ToString();
    }

    private string GetStatusName(string status) => status switch
    {
        "pending" => "قيد الانتظار",
        "reviewed" => "تم المراجعة",
        "in_store" => "في المخزن",
        "with_driver" => "مع المندوب",
        "on_the_way" => "في الطريق",
        "delivered" => "تم التسليم",
        "postponed" => "مؤجل",
        "cancelled" => "ملغي",
        _ => status
    };

    private string GetAccountStatusName(string status) => status switch
    {
        "pending" => "قيد الانتظار",
        "active" => "نشط",
        "blocked" => "محظور",
        "deleted" => "محذوف",
        _ => status
    };

    public async Task<DataTable> GetDailySalesDataTableAsync(DateTime? date = null)
    {
        date ??= DateTime.Today;
        await using var db = await _contextFactory.CreateDbContextAsync();
        var start = date.Value.Date;
        var end = start.AddDays(1);

        var orders = await db.Orders
            .Where(o => o.CreatedAt >= start && o.CreatedAt < end && o.Status != "cancelled")
            .Select(o => new
            {
                o.OrderNumber,
                PharmacyName = o.Pharmacy != null ? o.Pharmacy.Name : "-",
                o.FinalTotal,
                o.Status,
                o.CreatedAt
            })
            .ToListAsync();

        var dt = new DataTable();
        dt.Columns.Add("رقم الطلب", typeof(string));
        dt.Columns.Add("الصيدلية", typeof(string));
        dt.Columns.Add("الإجمالي", typeof(decimal));
        dt.Columns.Add("الحالة", typeof(string));
        dt.Columns.Add("التاريخ", typeof(DateTime));

        foreach (var o in orders)
        {
            dt.Rows.Add(o.OrderNumber, o.PharmacyName, o.FinalTotal, GetStatusName(o.Status), o.CreatedAt);
        }

        return dt;
    }

    public async Task<DataTable> GetMonthlySalesDataTableAsync(int? year = null, int? month = null)
    {
        year ??= DateTime.Today.Year;
        month ??= DateTime.Today.Month;
        await using var db = await _contextFactory.CreateDbContextAsync();

        var start = new DateTime(year.Value, month.Value, 1);
        var end = start.AddMonths(1);

        var orders = await db.Orders
            .Where(o => o.CreatedAt >= start && o.CreatedAt < end && o.Status != "cancelled")
            .Select(o => new
            {
                o.OrderNumber,
                PharmacyName = o.Pharmacy != null ? o.Pharmacy.Name : "-",
                o.FinalTotal,
                o.Status,
                o.CreatedAt
            })
            .ToListAsync();

        var dt = new DataTable();
        dt.Columns.Add("رقم الطلب", typeof(string));
        dt.Columns.Add("الصيدلية", typeof(string));
        dt.Columns.Add("الإجمالي", typeof(decimal));
        dt.Columns.Add("الحالة", typeof(string));
        dt.Columns.Add("التاريخ", typeof(DateTime));

        foreach (var o in orders)
        {
            dt.Rows.Add(o.OrderNumber, o.PharmacyName, o.FinalTotal, GetStatusName(o.Status), o.CreatedAt);
        }

        return dt;
    }

    public async Task<DataTable> GetTopProductsDataTableAsync(int top = 20)
    {
        await using var db = await _contextFactory.CreateDbContextAsync();
        var productSalesData = await db.OrderItems
            .Where(i => i.Order != null && i.Order.Status != "cancelled")
            .Select(i => new
            {
                i.ProductId,
                ProductName = i.Product != null ? i.Product.Name : "غير محدد",
                ProductCategory = i.Product != null ? i.Product.Category : "غير محدد",
                i.Quantity,
                i.TotalPrice
            })
            .ToListAsync();

        var productSales = productSalesData
            .GroupBy(i => new { i.ProductId, Name = i.ProductName, Category = i.ProductCategory ?? "غير محدد" })
            .Select(g => new
            {
                g.Key.ProductId,
                g.Key.Name,
                g.Key.Category,
                TotalQuantity = g.Sum(i => i.Quantity),
                TotalRevenue = g.Sum(i => i.TotalPrice)
            })
            .OrderByDescending(p => p.TotalQuantity)
            .Take(top)
            .ToList();

        var dt = new DataTable();
        dt.Columns.Add("م", typeof(int));
        dt.Columns.Add("اسم المنتج", typeof(string));
        dt.Columns.Add("الفئة", typeof(string));
        dt.Columns.Add("الكمية", typeof(int));
        dt.Columns.Add("الإيرادات", typeof(decimal));

        int i = 1;
        foreach (var p in productSales)
        {
            dt.Rows.Add(i, p.Name, p.Category, p.TotalQuantity, p.TotalRevenue);
            i++;
        }

        return dt;
    }

    public async Task<DataTable> GetTopPharmaciesDataTableAsync(int top = 20)
    {
        await using var db = await _contextFactory.CreateDbContextAsync();
        var pharmacyStats = db.Pharmacies
            .Select(p => new
            {
                p.Id,
                p.Name,
                p.Balance,
                p.AccountStatus,
                OrderCount = p.Orders.Where(o => o.Status != "cancelled").Count(),
                TotalSales = p.Orders.Where(o => o.Status != "cancelled").Sum(o => (decimal?)o.FinalTotal) ?? 0
            })
            .OrderByDescending(p => p.TotalSales)
            .Take(top)
            .ToList();

        var dt = new DataTable();
        dt.Columns.Add("م", typeof(int));
        dt.Columns.Add("الاسم", typeof(string));
        dt.Columns.Add("عدد الطلبات", typeof(int));
        dt.Columns.Add("إجمالي المبيعات", typeof(decimal));
        dt.Columns.Add("الرصيد", typeof(decimal));
        dt.Columns.Add("الحالة", typeof(string));

        int i = 1;
        foreach (var p in pharmacyStats)
        {
            dt.Rows.Add(i, p.Name, p.OrderCount, p.TotalSales, p.Balance, GetAccountStatusName(p.AccountStatus));
            i++;
        }

        return dt;
    }

    public async Task<DataTable> GetExpiryDataTableAsync()
    {
        await using var db = await _contextFactory.CreateDbContextAsync();
        var today = DateTime.Today;
        var thirtyDays = today.AddDays(30);

        var products = db.Products
            .Where(p => !string.IsNullOrEmpty(p.ExpiryDate))
            .ToList()
            .Where(p => DateTime.TryParse(p.ExpiryDate, out var exp) && exp <= thirtyDays)
            .OrderBy(p => DateTime.TryParse(p.ExpiryDate, out var d) ? d : DateTime.MaxValue)
            .ToList();

        var dt = new DataTable();
        dt.Columns.Add("م", typeof(int));
        dt.Columns.Add("المنتج", typeof(string));
        dt.Columns.Add("تاريخ الانتهاء", typeof(string));
        dt.Columns.Add("الكمية", typeof(int));
        dt.Columns.Add("الأيام المتبقية", typeof(int));
        dt.Columns.Add("الحالة", typeof(string));

        int i = 1;
        foreach (var p in products)
        {
            DateTime.TryParse(p.ExpiryDate, out var exp);
            var daysLeft = (exp - today).Days;
            var warning = daysLeft <= 0 ? "منتهي" : daysLeft <= 7 ? "عاجل" : daysLeft <= 30 ? "تنبيه" : "OK";
            dt.Rows.Add(i, p.Name, p.ExpiryDate, p.Quantity, daysLeft, warning);
            i++;
        }

        return dt;
    }

    public async Task<DataTable> GetDebtsDataTableAsync()
    {
        await using var db = await _contextFactory.CreateDbContextAsync();
        var pharmacies = db.Pharmacies
            .Where(p => p.Balance > 0 && p.AccountStatus == "active")
            .OrderByDescending(p => p.Balance)
            .ToList();

        var dt = new DataTable();
        dt.Columns.Add("م", typeof(int));
        dt.Columns.Add("الاسم", typeof(string));
        dt.Columns.Add("الحالة", typeof(string));
        dt.Columns.Add("الرصيد", typeof(decimal));

        int i = 1;
        foreach (var p in pharmacies)
        {
            dt.Rows.Add(i, p.Name, GetAccountStatusName(p.AccountStatus), p.Balance);
            i++;
        }

        return dt;
    }

    public async Task<DataTable> GetStockDataTableAsync()
    {
        await using var db = await _contextFactory.CreateDbContextAsync();
        var products = db.Products.OrderBy(p => p.Quantity).ToList();

        var dt = new DataTable();
        dt.Columns.Add("م", typeof(int));
        dt.Columns.Add("المنتج", typeof(string));
        dt.Columns.Add("الفئة", typeof(string));
        dt.Columns.Add("الكمية", typeof(int));
        dt.Columns.Add("سعر الوحدة", typeof(decimal));
        dt.Columns.Add("القيمة", typeof(decimal));
        dt.Columns.Add("الحالة", typeof(string));

        int i = 1;
        foreach (var p in products)
        {
            var value = p.Quantity * p.UnitPrice;
            var status = p.Quantity == 0 ? "نفذ" : p.Quantity <= 10 ? "منخفض" : "متوفر";
            dt.Rows.Add(i, p.Name, p.Category ?? "-", p.Quantity, p.UnitPrice, value, status);
            i++;
        }

        return dt;
    }

    // ═══════════════════════════════════════════════════════════════
    // FINANCIAL REPORT METHODS
    // ═══════════════════════════════════════════════════════════════

    public async Task<FinancialSummaryDto> GetFinancialSummaryAsync(DateTime? from = null, DateTime? to = null)
    {
        from ??= DateTime.Today.AddMonths(-1);
        to ??= DateTime.Today;
        await using var db = await _contextFactory.CreateDbContextAsync();

        var orders = await db.Orders
            .Where(o => o.CreatedAt >= from && o.CreatedAt < to.Value.AddDays(1))
            .ToListAsync();

        var payments = await db.Payments
            .Where(p => p.Date >= from && p.Date < to.Value.AddDays(1))
            .ToListAsync();

        var returns = await db.Returns
            .Where(r => r.CreatedAt >= from && r.CreatedAt < to.Value.AddDays(1))
            .ToListAsync();

        var totalRevenue = orders.Where(o => o.Status != "cancelled").Sum(o => o.FinalTotal);
        var totalRefunds = returns.Sum(r => r.TotalAmount);
        var totalDiscount = orders.Where(o => o.Status != "cancelled").Sum(o => o.Discount);
        var totalTax = orders.Where(o => o.Status != "cancelled").Sum(o => o.TotalAmount - o.Discount - o.FinalTotal);
        var totalExpenses = totalDiscount + totalRefunds;
        var netProfit = totalRevenue - totalExpenses;

        // Previous period for comparison
        var prevFrom = from.Value.AddMonths(-1);
        var prevTo = from.Value.AddDays(-1);
        var prevOrders = await db.Orders
            .Where(o => o.CreatedAt >= prevFrom && o.CreatedAt < prevTo.AddDays(1))
            .ToListAsync();
        var prevRevenue = prevOrders.Where(o => o.Status != "cancelled").Sum(o => o.FinalTotal);

        return new FinancialSummaryDto
        {
            TotalRevenue = totalRevenue,
            TotalExpenses = totalExpenses,
            NetProfit = netProfit,
            ProfitMargin = totalRevenue > 0 ? netProfit / totalRevenue * 100 : 0,
            TotalTax = totalTax,
            TotalDiscount = totalDiscount,
            TotalRefunds = totalRefunds,
            RevenueChange = prevRevenue > 0 ? (totalRevenue - prevRevenue) / prevRevenue * 100 : 0,
            ExpenseChange = 0,
            ProfitChange = 0,
            TotalOrders = orders.Count,
            PaidOrders = orders.Count(o => o.Status == "delivered" || o.Status == "paid"),
            PendingOrders = orders.Count(o => o.Status == "pending" || o.Status == "reviewed"),
            CancelledOrders = orders.Count(o => o.Status == "cancelled"),
            AverageOrderValue = orders.Count > 0 ? totalRevenue / orders.Count : 0,
            TotalReceivables = orders.Where(o => o.Status != "cancelled").Sum(o => o.FinalTotal) - payments.Sum(p => p.Amount),
            TotalPayables = 0
        };
    }

    public async Task<List<RevenueReportDto>> GetRevenueReportAsync(DateTime from, DateTime to, string periodType = "daily")
    {
        await using var db = await _contextFactory.CreateDbContextAsync();
        var orders = await db.Orders
            .Where(o => o.CreatedAt >= from && o.CreatedAt < to.AddDays(1) && o.Status != "cancelled")
            .Select(o => new { o.CreatedAt, o.FinalTotal, o.Discount, GrossTotal = o.TotalAmount })
            .ToListAsync();

        var returns = await db.Returns
            .Where(r => r.CreatedAt >= from && r.CreatedAt < to.AddDays(1))
            .Select(r => new { r.CreatedAt, r.TotalAmount })
            .ToListAsync();

        var grouped = periodType switch
        {
            "daily" => orders.GroupBy(o => o.CreatedAt.Date)
                .Select(g => new RevenueReportDto
                {
                    Date = g.Key,
                    Period = g.Key.ToString("yyyy-MM-dd"),
                    GrossRevenue = g.Sum(o => o.FinalTotal + o.Discount),
                    Discounts = g.Sum(o => o.Discount),
                    Refunds = returns.Where(r => r.CreatedAt.Date == g.Key).Sum(r => r.TotalAmount),
                    NetRevenue = g.Sum(o => o.FinalTotal),
                    Tax = g.Sum(o => o.GrossTotal - o.Discount - o.FinalTotal),
                    OrderCount = g.Count(),
                    ItemCount = 0
                }),
            "monthly" => orders.GroupBy(o => new { o.CreatedAt.Year, o.CreatedAt.Month })
                .Select(g => new RevenueReportDto
                {
                    Date = new DateTime(g.Key.Year, g.Key.Month, 1, 0, 0, 0),
                    Period = new DateTime(g.Key.Year, g.Key.Month, 1).ToString("yyyy-MM"),
                    GrossRevenue = g.Sum(o => o.FinalTotal + o.Discount),
                    Discounts = g.Sum(o => o.Discount),
                    Refunds = returns.Where(r => r.CreatedAt.Year == g.Key.Year && r.CreatedAt.Month == g.Key.Month).Sum(r => r.TotalAmount),
                    NetRevenue = g.Sum(o => o.FinalTotal),
                    Tax = g.Sum(o => o.GrossTotal - o.Discount - o.FinalTotal),
                    OrderCount = g.Count(),
                    ItemCount = 0
                }),
            _ => orders.GroupBy(o => o.CreatedAt.Date)
                .Select(g => new RevenueReportDto
                {
                    Date = g.Key,
                    Period = g.Key.ToString("yyyy-MM-dd"),
                    GrossRevenue = g.Sum(o => o.FinalTotal + o.Discount),
                    Discounts = g.Sum(o => o.Discount),
                    Refunds = returns.Where(r => r.CreatedAt.Date == g.Key).Sum(r => r.TotalAmount),
                    NetRevenue = g.Sum(o => o.FinalTotal),
                    Tax = g.Sum(o => o.GrossTotal - o.Discount - o.FinalTotal),
                    OrderCount = g.Count(),
                    ItemCount = 0
                })
        };

        return grouped.OrderBy(r => r.Date).ToList();
    }

    public async Task<List<ExpenseReportDto>> GetExpenseBreakdownAsync(DateTime from, DateTime to)
    {
        await using var db = await _contextFactory.CreateDbContextAsync();
        var orders = await db.Orders
            .Where(o => o.CreatedAt >= from && o.CreatedAt < to.AddDays(1))
            .ToListAsync();

        var returns = await db.Returns
            .Where(r => r.CreatedAt >= from && r.CreatedAt < to.AddDays(1))
            .ToListAsync();

        var purchases = await db.Purchases
            .Where(p => p.CreatedAt >= from && p.CreatedAt < to.AddDays(1))
            .ToListAsync();

        var total = orders.Sum(o => o.Discount) + returns.Sum(r => r.TotalAmount) + purchases.Sum(p => p.TotalAmount);

        var expenses = new List<ExpenseReportDto>
        {
            new() { Category = "الخصومات", Amount = orders.Sum(o => o.Discount), Count = orders.Count(o => o.Discount > 0), Trend = "up" },
            new() { Category = "المرتجعات", Amount = returns.Sum(r => r.TotalAmount), Count = returns.Count, Trend = "down" },
            new() { Category = "المشتريات", Amount = purchases.Sum(p => p.TotalAmount), Count = purchases.Count, Trend = "stable" }
        };

        foreach (var e in expenses)
            e.Percentage = total > 0 ? e.Amount / total * 100 : 0;

        return expenses;
    }

    public async Task<List<ProfitReportDto>> GetProfitLossAsync(DateTime from, DateTime to)
    {
        await using var db = await _contextFactory.CreateDbContextAsync();
        var orders = await db.Orders
            .Where(o => o.CreatedAt >= from && o.CreatedAt < to.AddDays(1) && o.Status != "cancelled")
            .Select(o => new { o.CreatedAt, o.FinalTotal, o.Discount, o.TotalAmount })
            .ToListAsync();

        var totalReturns = await db.Returns
            .Where(r => r.CreatedAt >= from && r.CreatedAt < to.AddDays(1))
            .SumAsync(r => r.TotalAmount);

        var grouped = orders.GroupBy(o => o.CreatedAt.Date)
            .Select(g =>
            {
                var revenue = g.Sum(o => o.FinalTotal);
                var discounts = g.Sum(o => o.Discount);
                var costs = discounts + totalReturns;
                var grossProfit = revenue - costs;
                var tax = g.Sum(o => o.TotalAmount - o.Discount - o.FinalTotal);
                return new ProfitReportDto
                {
                    Date = g.Key,
                    Revenue = revenue,
                    CostOfGoods = costs,
                    GrossProfit = grossProfit,
                    GrossMargin = revenue > 0 ? grossProfit / revenue * 100 : 0,
                    OperatingExpenses = tax,
                    NetProfit = grossProfit - tax,
                    NetMargin = revenue > 0 ? (grossProfit - tax) / revenue * 100 : 0
                };
            })
            .OrderBy(r => r.Date)
            .ToList();

        return grouped;
    }

    public async Task<List<ChartDataPoint>> GetRevenueTrendAsync(int days = 30)
    {
        await using var db = await _contextFactory.CreateDbContextAsync();
        var from = DateTime.Today.AddDays(-days);
        var orders = await db.Orders
            .Where(o => o.CreatedAt >= from && o.Status != "cancelled")
            .Select(o => new { o.CreatedAt, o.FinalTotal })
            .ToListAsync();

        return orders.GroupBy(o => o.CreatedAt.Date)
            .Select(g => new ChartDataPoint
            {
                Label = g.Key.ToString("MM/dd"),
                Value = g.Sum(o => o.FinalTotal),
                Color = "#FF10B981"
            })
            .OrderBy(p => p.Label)
            .ToList();
    }

    public async Task<List<ChartDataPoint>> GetCategoryRevenueAsync(DateTime from, DateTime to)
    {
        await using var db = await _contextFactory.CreateDbContextAsync();
        var items = await db.OrderItems
            .Where(i => i.Order != null && i.Order.CreatedAt >= from && i.Order.CreatedAt < to.AddDays(1) && i.Order.Status != "cancelled")
            .Select(i => new
            {
                Category = i.Product != null ? i.Product.Category : "غير محدد",
                i.TotalPrice
            })
            .ToListAsync();

        var colors = new[] { "#FF10B981", "#FF3B82F6", "#FFF59E0B", "#FFEF4444", "#FF8B5CF6", "#FFEC4899", "#FF6366F1" };
        var idx = 0;

        return items.GroupBy(i => i.Category)
            .Select(g => new ChartDataPoint
            {
                Label = g.Key ?? "غير محدد",
                Value = g.Sum(i => i.TotalPrice),
                Color = colors[idx++ % colors.Length]
            })
            .OrderByDescending(p => p.Value)
            .ToList();
    }

    public async Task<List<PeriodComparisonDto>> GetPeriodComparisonAsync(DateTime currentFrom, DateTime currentTo)
    {
        var days = (currentTo - currentFrom).Days + 1;
        var prevFrom = currentFrom.AddDays(-days);
        var prevTo = currentFrom.AddDays(-1);

        await using var db = await _contextFactory.CreateDbContextAsync();

        var currentOrders = await db.Orders
            .Where(o => o.CreatedAt >= currentFrom && o.CreatedAt < currentTo.AddDays(1))
            .ToListAsync();
        var prevOrders = await db.Orders
            .Where(o => o.CreatedAt >= prevFrom && o.CreatedAt < prevTo.AddDays(1))
            .ToListAsync();

        var curRevenue = currentOrders.Where(o => o.Status != "cancelled").Sum(o => o.FinalTotal);
        var prevRevenue = prevOrders.Where(o => o.Status != "cancelled").Sum(o => o.FinalTotal);
        var curOrders = currentOrders.Count;
        var prevOrdersCount = prevOrders.Count;
        var curAvg = curOrders > 0 ? curRevenue / curOrders : 0;
        var prevAvg = prevOrdersCount > 0 ? prevRevenue / prevOrdersCount : 0;

        static (decimal change, double pct) Calc(decimal cur, decimal prev) => (
            cur - prev,
            prev > 0 ? (double)((cur - prev) / prev * 100) : 0
        );

        var revDelta = Calc(curRevenue, prevRevenue);
        var ordDelta = Calc(curOrders, prevOrdersCount);
        var avgDelta = Calc(curAvg, prevAvg);

        return
        [
            new() { Metric = "إجمالي الإيرادات", CurrentPeriod = curRevenue, PreviousPeriod = prevRevenue, Change = revDelta.change, ChangePercent = revDelta.pct, Trend = revDelta.change >= 0 ? "up" : "down" },
            new() { Metric = "عدد الطلبات", CurrentPeriod = curOrders, PreviousPeriod = prevOrdersCount, Change = ordDelta.change, ChangePercent = ordDelta.pct, Trend = ordDelta.change >= 0 ? "up" : "down" },
            new() { Metric = "متوسط قيمة الطلب", CurrentPeriod = curAvg, PreviousPeriod = prevAvg, Change = avgDelta.change, ChangePercent = avgDelta.pct, Trend = avgDelta.change >= 0 ? "up" : "down" }
        ];
    }

    public async Task<List<TopProductFinancialDto>> GetTopProductsFinancialAsync(DateTime from, DateTime to, int top = 20)
    {
        await using var db = await _contextFactory.CreateDbContextAsync();
        var items = await db.OrderItems
            .Where(i => i.Order != null && i.Order.CreatedAt >= from && i.Order.CreatedAt < to.AddDays(1) && i.Order.Status != "cancelled")
            .Select(i => new
            {
                i.ProductId,
                ProductName = i.Product != null ? i.Product.Name : "غير محدد",
                Category = i.Product != null ? i.Product.Category : "غير محدد",
                i.Quantity,
                i.TotalPrice,
                UnitPrice = i.Product != null ? i.Product.UnitPrice : 0
            })
            .ToListAsync();

        return items.GroupBy(i => new { i.ProductId, i.ProductName, i.Category })
            .Select(g =>
            {
                var revenue = g.Sum(i => i.TotalPrice);
                var cost = g.Sum(i => i.Quantity * i.UnitPrice);
                var profit = revenue - cost;
                return new TopProductFinancialDto
                {
                    ProductName = g.Key.ProductName,
                    Category = g.Key.Category ?? "غير محدد",
                    QuantitySold = g.Sum(i => i.Quantity),
                    Revenue = revenue,
                    Cost = cost,
                    Profit = profit,
                    Margin = revenue > 0 ? profit / revenue * 100 : 0,
                    StockRemaining = 0
                };
            })
            .OrderByDescending(p => p.Revenue)
            .Take(top)
            .Select((p, idx) => { p.Rank = idx + 1; return p; })
            .ToList();
    }

    public async Task<List<PharmacyFinancialDto>> GetPharmaciesFinancialAsync(DateTime from, DateTime to, int top = 20)
    {
        await using var db = await _contextFactory.CreateDbContextAsync();
        var pharmacies = await db.Pharmacies
            .Select(p => new
            {
                p.Id,
                p.Name,
                p.Balance,
                p.AccountStatus,
                Orders = p.Orders.Where(o => o.CreatedAt >= from && o.CreatedAt < to.AddDays(1) && o.Status != "cancelled").ToList(),
                Payments = p.Payments.Where(pm => pm.Date >= from && pm.Date < to.AddDays(1)).ToList()
            })
            .ToListAsync();

        return pharmacies
            .Select(p => new PharmacyFinancialDto
            {
                Name = p.Name,
                Status = GetAccountStatusName(p.AccountStatus),
                OrderCount = p.Orders.Count,
                TotalSales = p.Orders.Sum(o => o.FinalTotal),
                TotalPayments = p.Payments.Sum(pm => pm.Amount),
                Balance = p.Balance,
                AverageOrderValue = p.Orders.Count > 0 ? p.Orders.Sum(o => o.FinalTotal) / p.Orders.Count : 0,
                LastOrderDate = p.Orders.Count > 0 ? p.Orders.Max(o => o.CreatedAt) : DateTime.MinValue
            })
            .OrderByDescending(p => p.TotalSales)
            .Take(top)
            .Select((p, idx) => { p.Rank = idx + 1; return p; })
            .ToList();
    }

    // ═══════════════════════════════════════════════════════════════
    // FINANCIAL DataTable METHODS (for grid view)
    // ═══════════════════════════════════════════════════════════════

    public async Task<DataTable> GetRevenueDataTableAsync(DateTime from, DateTime to, string periodType = "daily")
    {
        var data = await GetRevenueReportAsync(from, to, periodType);
        var dt = new DataTable();
        dt.Columns.Add("الفترة", typeof(string));
        dt.Columns.Add("الإيرادات الإجمالية", typeof(decimal));
        dt.Columns.Add("الخصومات", typeof(decimal));
        dt.Columns.Add("المرتجعات", typeof(decimal));
        dt.Columns.Add("صافي الإيرادات", typeof(decimal));
        dt.Columns.Add("الضريبة", typeof(decimal));
        dt.Columns.Add("الطلبات", typeof(int));

        foreach (var r in data)
            dt.Rows.Add(r.Period, r.GrossRevenue, r.Discounts, r.Refunds, r.NetRevenue, r.Tax, r.OrderCount);

        return dt;
    }

    public async Task<DataTable> GetProfitLossDataTableAsync(DateTime from, DateTime to)
    {
        var data = await GetProfitLossAsync(from, to);
        var dt = new DataTable();
        dt.Columns.Add("التاريخ", typeof(string));
        dt.Columns.Add("الإيرادات", typeof(decimal));
        dt.Columns.Add("التكاليف", typeof(decimal));
        dt.Columns.Add("إجمالي الربح", typeof(decimal));
        dt.Columns.Add("نسبة الربح", typeof(decimal));
        dt.Columns.Add("المصاريف", typeof(decimal));
        dt.Columns.Add("صافي الربح", typeof(decimal));

        foreach (var r in data)
            dt.Rows.Add(r.Date.ToString("yyyy-MM-dd"), r.Revenue, r.CostOfGoods, r.GrossProfit, $"{r.GrossMargin:F1}%", r.OperatingExpenses, r.NetProfit);

        return dt;
    }

    public async Task<DataTable> GetExpenseDataTableAsync(DateTime from, DateTime to)
    {
        var data = await GetExpenseBreakdownAsync(from, to);
        var dt = new DataTable();
        dt.Columns.Add("الفئة", typeof(string));
        dt.Columns.Add("المبلغ", typeof(decimal));
        dt.Columns.Add("النسبة", typeof(string));
        dt.Columns.Add("العدد", typeof(int));

        foreach (var e in data)
            dt.Rows.Add(e.Category, e.Amount, $"{e.Percentage:F1}%", e.Count);

        return dt;
    }

    public async Task<DataTable> GetTopProductsFinancialDataTableAsync(DateTime from, DateTime to)
    {
        var data = await GetTopProductsFinancialAsync(from, to);
        var dt = new DataTable();
        dt.Columns.Add("م", typeof(int));
        dt.Columns.Add("المنتج", typeof(string));
        dt.Columns.Add("الفئة", typeof(string));
        dt.Columns.Add("الكمية", typeof(int));
        dt.Columns.Add("الإيرادات", typeof(decimal));
        dt.Columns.Add("التكلفة", typeof(decimal));
        dt.Columns.Add("الربح", typeof(decimal));
        dt.Columns.Add("نسبة الربح", typeof(string));

        foreach (var p in data)
            dt.Rows.Add(p.Rank, p.ProductName, p.Category, p.QuantitySold, p.Revenue, p.Cost, p.Profit, $"{p.Margin:F1}%");

        return dt;
    }

    public async Task<DataTable> GetPharmaciesFinancialDataTableAsync(DateTime from, DateTime to)
    {
        var data = await GetPharmaciesFinancialAsync(from, to);
        var dt = new DataTable();
        dt.Columns.Add("م", typeof(int));
        dt.Columns.Add("الصيدلية", typeof(string));
        dt.Columns.Add("الحالة", typeof(string));
        dt.Columns.Add("الطلبات", typeof(int));
        dt.Columns.Add("المبيعات", typeof(decimal));
        dt.Columns.Add("المدفوعات", typeof(decimal));
        dt.Columns.Add("الرصيد", typeof(decimal));
        dt.Columns.Add("متوسط الطلب", typeof(decimal));

        foreach (var p in data)
            dt.Rows.Add(p.Rank, p.Name, p.Status, p.OrderCount, p.TotalSales, p.TotalPayments, p.Balance, p.AverageOrderValue);

        return dt;
    }
}
