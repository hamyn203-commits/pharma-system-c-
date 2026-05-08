using AlNeda.Core.Entities;
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

        var orders = db.Orders.Include(o => o.Pharmacy).Include(o => o.Items)
            .Where(o => o.CreatedAt >= start && o.CreatedAt < end && o.Status != "cancelled")
            .ToList();

        var payments = db.Payments.Where(p => p.Date >= start && p.Date < end).ToList();

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
            var totalItems = orders.SelectMany(o => o.Items).Sum(i => i.Quantity);
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

        var orders = db.Orders.Include(o => o.Pharmacy).Include(o => o.Items)
            .Where(o => o.CreatedAt >= start && o.CreatedAt < end && o.Status != "cancelled")
            .ToList();

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
        var productSales = db.OrderItems.Include(i => i.Product).Include(i => i.Order)
            .Where(i => i.Order != null && i.Order.Status != "cancelled")
            .ToList()
            .GroupBy(i => new { i.ProductId, Name = i.Product?.Name ?? "غير محدد", Category = i.Product?.Category ?? "غير محدد" })
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
            .Include(p => p.Orders.Where(o => o.Status != "cancelled"))
            .Select(p => new
            {
                p.Id,
                p.Name,
                p.Balance,
                p.AccountStatus,
                OrderCount = p.Orders.Count(),
                TotalSales = p.Orders.Sum(o => (decimal?)o.FinalTotal) ?? 0
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

        var orders = db.Orders.Include(o => o.Pharmacy)
            .Where(o => o.CreatedAt >= start && o.CreatedAt < end && o.Status != "cancelled")
            .ToList();

        var dt = new DataTable();
        dt.Columns.Add("رقم الطلب", typeof(string));
        dt.Columns.Add("الصيدلية", typeof(string));
        dt.Columns.Add("الإجمالي", typeof(decimal));
        dt.Columns.Add("الحالة", typeof(string));
        dt.Columns.Add("التاريخ", typeof(DateTime));

        foreach (var o in orders)
        {
            dt.Rows.Add(o.OrderNumber, o.Pharmacy?.Name ?? "-", o.FinalTotal, GetStatusName(o.Status), o.CreatedAt);
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

        var orders = db.Orders.Include(o => o.Pharmacy)
            .Where(o => o.CreatedAt >= start && o.CreatedAt < end && o.Status != "cancelled")
            .ToList();

        var dt = new DataTable();
        dt.Columns.Add("رقم الطلب", typeof(string));
        dt.Columns.Add("الصيدلية", typeof(string));
        dt.Columns.Add("الإجمالي", typeof(decimal));
        dt.Columns.Add("الحالة", typeof(string));
        dt.Columns.Add("التاريخ", typeof(DateTime));

        foreach (var o in orders)
        {
            dt.Rows.Add(o.OrderNumber, o.Pharmacy?.Name ?? "-", o.FinalTotal, GetStatusName(o.Status), o.CreatedAt);
        }

        return dt;
    }

    public async Task<DataTable> GetTopProductsDataTableAsync(int top = 20)
    {
        await using var db = await _contextFactory.CreateDbContextAsync();
        var productSales = db.OrderItems.Include(i => i.Product).Include(i => i.Order)
            .Where(i => i.Order != null && i.Order.Status != "cancelled")
            .ToList()
            .GroupBy(i => new { i.ProductId, Name = i.Product?.Name ?? "غير محدد", Category = i.Product?.Category ?? "غير محدد" })
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
            .Include(p => p.Orders.Where(o => o.Status != "cancelled"))
            .Select(p => new
            {
                p.Id,
                p.Name,
                p.Balance,
                p.AccountStatus,
                OrderCount = p.Orders.Count(),
                TotalSales = p.Orders.Sum(o => (decimal?)o.FinalTotal) ?? 0
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
}
