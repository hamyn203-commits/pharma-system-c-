using System.Data;
using System.Text;
using AlNeda.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace AlNeda.Services.Export;

public interface IReportExporter
{
    Task<string> ExportToCsvAsync(string reportName, DataTable data);
    Task<string> ExportToExcelAsync(string reportName, DataTable data);
    string ExportToCsv(DataTable data);
    byte[] ExportToExcel(DataTable data);
    Task WriteCsvToStreamAsync(DataTable data, Stream destination);
    Task WriteExcelToStreamAsync(DataTable data, Stream destination);
}

public class ReportExporterService : IReportExporter
{
    private readonly IDbContextFactory<Data.AppDbContext> _contextFactory;

    public ReportExporterService(IDbContextFactory<Data.AppDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    // ── File-based exports (backward compat) ──

    public async Task<string> ExportToCsvAsync(string reportName, DataTable data)
    {
        var outputDir = GetExportDirectory();
        var fileName = $"{SanitizeFileName(reportName)}_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
        var filePath = Path.Combine(outputDir, fileName);

        await using var fileStream = File.Create(filePath);
        await WriteCsvToStreamAsync(data, fileStream);

        return filePath;
    }

    public async Task<string> ExportToExcelAsync(string reportName, DataTable data)
    {
        var outputDir = GetExportDirectory();
        var fileName = $"{SanitizeFileName(reportName)}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
        var filePath = Path.Combine(outputDir, fileName);

        await using var fileStream = File.Create(filePath);
        await WriteExcelToStreamAsync(data, fileStream);

        return filePath;
    }

    // ── In-memory exports ──

    public string ExportToCsv(DataTable data)
    {
        using var ms = new MemoryStream();
        WriteCsvToStreamAsync(data, ms).GetAwaiter().GetResult();
        return Encoding.UTF8.GetString(ms.ToArray());
    }

    public byte[] ExportToExcel(DataTable data)
    {
        using var ms = new MemoryStream();
        WriteExcelToStreamAsync(data, ms).GetAwaiter().GetResult();
        return ms.ToArray();
    }

    // ── Streaming exports (zero-copy, low memory) ──

    public async Task WriteCsvToStreamAsync(DataTable data, Stream destination)
    {
        var preamble = Encoding.UTF8.GetPreamble();
        await destination.WriteAsync(preamble);

        // Headers
        var headerLine = string.Join(",", data.Columns.Cast<DataColumn>().Select(c => EscapeCsvField(c.ColumnName))) + "\n";
        await destination.WriteAsync(Encoding.UTF8.GetBytes(headerLine));

        // Rows - streamed one by one
        foreach (DataRow row in data.Rows)
        {
            var line = string.Join(",", row.ItemArray.Select(f => EscapeCsvField(f?.ToString() ?? ""))) + "\n";
            var bytes = Encoding.UTF8.GetBytes(line);
            await destination.WriteAsync(bytes);
        }

        await destination.FlushAsync();
    }

    public async Task WriteExcelToStreamAsync(DataTable data, Stream destination)
    {
        var preamble = Encoding.UTF8.GetPreamble();
        await destination.WriteAsync(preamble);

        await WriteStringAsync(destination, "<!DOCTYPE html>\n");
        await WriteStringAsync(destination, "<html xmlns:o='urn:schemas-microsoft-com:office:office' xmlns:x='urn:schemas-microsoft-com:office:excel'>\n");
        await WriteStringAsync(destination, "<head><meta charset='UTF-8'><title>AlNeda Report</title>\n");
        await WriteStringAsync(destination, "<!--[if gte mso 9]><xml><x:ExcelWorkbook><x:ExcelWorksheets><x:ExcelWorksheet>\n");
        await WriteStringAsync(destination, "<x:Name>AlNeda</x:Name><x:WorksheetOptions><x:DisplayGridlines/></x:WorksheetOptions>\n");
        await WriteStringAsync(destination, "</x:ExcelWorksheet></x:ExcelWorksheets></x:ExcelWorkbook></xml><![endif]-->\n");
        await WriteStringAsync(destination, "</head><body>\n");
        await WriteStringAsync(destination, "<table border='1' style='direction:rtl;font-family:Arial;font-size:11pt;'>\n");

        // Headers
        await WriteStringAsync(destination, "<thead><tr style='background-color:#55D66B;color:white;'>\n");
        foreach (DataColumn col in data.Columns)
        {
            await WriteStringAsync(destination, $"<th style='padding:6px 10px;'>{HtmlEncode(col.ColumnName)}</th>\n");
        }
        await WriteStringAsync(destination, "</tr></thead>\n");

        // Body - streamed row by row
        await WriteStringAsync(destination, "<tbody>\n");
        foreach (DataRow row in data.Rows)
        {
            await WriteStringAsync(destination, "<tr>\n");
            foreach (var item in row.ItemArray)
            {
                var align = item is decimal or double or int ? "left" : "right";
                await WriteStringAsync(destination, $"<td style='text-align:{align};padding:4px 8px;'>{HtmlEncode(item?.ToString() ?? "")}</td>\n");
            }
            await WriteStringAsync(destination, "</tr>\n");
        }
        await WriteStringAsync(destination, "</tbody></table>\n");
        await WriteStringAsync(destination, "</body></html>\n");

        await destination.FlushAsync();
    }

    public async Task AutoExportAllReportsAsync()
    {
        var reports = new[] { "daily_sales", "monthly_sales", "top_products", "stock", "debts" };

        foreach (var name in reports)
        {
            try
            {
                var data = await GenerateDataTableAsync(name);
                await ExportToCsvAsync(name, data);
                await ExportToExcelAsync(name, data);
            }
            catch { }
        }
    }

    // ── Data Generators ──

    private async Task<DataTable> GenerateDataTableAsync(string reportName)
    {
        await using var db = await _contextFactory.CreateDbContextAsync();
        return reportName switch
        {
            "daily_sales" => await GenerateDailySalesAsync(db),
            "monthly_sales" => await GenerateMonthlySalesAsync(db),
            "top_products" => await GenerateTopProductsAsync(db),
            "stock" => await GenerateStockAsync(db),
            "debts" => await GenerateDebtsAsync(db),
            _ => new DataTable(reportName)
        };
    }

    private static async Task<DataTable> GenerateDailySalesAsync(Data.AppDbContext db)
    {
        var today = DateTime.Today;
        var orders = await db.Orders
            .Include(o => o.Pharmacy)
            .Where(o => o.CreatedAt >= today && o.CreatedAt < today.AddDays(1) && o.Status != "cancelled")
            .ToListAsync();

        var dt = CreateTable("م", "رقم الطلب", "الصيدلية", "الإجمالي", "الحالة", "التاريخ");
        int i = 1;
        foreach (var o in orders)
            dt.Rows.Add(i++, o.OrderNumber, o.Pharmacy?.Name ?? "-", o.FinalTotal, o.Status, o.CreatedAt.ToString("yyyy-MM-dd"));
        return dt;
    }

    private static async Task<DataTable> GenerateMonthlySalesAsync(Data.AppDbContext db)
    {
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

    private static async Task<DataTable> GenerateTopProductsAsync(Data.AppDbContext db)
    {
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

    private static async Task<DataTable> GenerateStockAsync(Data.AppDbContext db)
    {
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

    private static async Task<DataTable> GenerateDebtsAsync(Data.AppDbContext db)
    {
        var pharmacies = await db.Pharmacies.Where(p => p.Balance > 0).OrderByDescending(p => p.Balance).ToListAsync();
        var dt = CreateTable("م", "الاسم", "الرصيد", "الحالة");
        int i = 1;
        foreach (var p in pharmacies)
            dt.Rows.Add(i++, p.Name, p.Balance, p.AccountStatus);
        return dt;
    }

    // ── Helpers ──

    private static DataTable CreateTable(params string[] columns)
    {
        var dt = new DataTable();
        foreach (var col in columns)
            dt.Columns.Add(col);
        return dt;
    }

    private static string GetExportDirectory()
    {
        var dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "exports");
        Directory.CreateDirectory(dir);
        return dir;
    }

    private static string SanitizeFileName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        return string.Join("_", name.Split(invalid, StringSplitOptions.RemoveEmptyEntries));
    }

    private static string EscapeCsvField(string? field)
    {
        if (string.IsNullOrEmpty(field)) return "\"\"";
        if (field.Contains(',') || field.Contains('"') || field.Contains('\n'))
            return $"\"{field.Replace("\"", "\"\"")}\"";
        return field;
    }

    private static string HtmlEncode(string? text) =>
        System.Net.WebUtility.HtmlEncode(text ?? "");

    private static async Task WriteStringAsync(Stream stream, string value)
    {
        var bytes = Encoding.UTF8.GetBytes(value);
        await stream.WriteAsync(bytes);
    }
}
