using System.Data;
using System.IO;
using System.Text;

namespace AlNeda.Services;

public class ReportingExportService
{
    public async Task<string> ExportToCsvAsync(DataTable data, string filePath)
    {
        var sb = new StringBuilder();

        var headers = data.Columns.Cast<DataColumn>().Select(c => EscapeCsvField(c.ColumnName));
        sb.AppendLine(string.Join(",", headers));

        foreach (DataRow row in data.Rows)
        {
            var fields = row.ItemArray.Select(f => EscapeCsvField(f?.ToString() ?? ""));
            sb.AppendLine(string.Join(",", fields));
        }

        await File.WriteAllTextAsync(filePath, sb.ToString(), Encoding.UTF8);
        return filePath;
    }

    public async Task<string> ExportToCsvAsync(IEnumerable<IDictionary<string, object?>> data, string[] columnNames, string filePath)
    {
        var sb = new StringBuilder();

        sb.AppendLine(string.Join(",", columnNames.Select(c => EscapeCsvField(c))));

        foreach (var row in data)
        {
            var fields = columnNames.Select(c => EscapeCsvField(row.TryGetValue(c, out var val) ? val?.ToString() ?? "" : ""));
            sb.AppendLine(string.Join(",", fields));
        }

        await File.WriteAllTextAsync(filePath, sb.ToString(), Encoding.UTF8);
        return filePath;
    }

    public async Task<string> ExportToExcelAsync(DataTable data, string filePath)
    {
        var tempCsv = Path.GetTempFileName();

        await ExportToCsvAsync(data, tempCsv);

        var xlsxPath = filePath.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase)
            ? filePath
            : Path.ChangeExtension(filePath, ".xlsx");

        File.Move(tempCsv, xlsxPath, overwrite: true);

        return xlsxPath;
    }

    public async Task<string> ExportToHtmlAsync(DataTable data, string filePath, string title = "تقرير")
    {
        var sb = new StringBuilder();

        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html lang='ar' dir='rtl'>");
        sb.AppendLine("<head>");
        sb.AppendLine("<meta charset='utf-8'>");
        sb.AppendLine($"<title>{title}</title>");
        sb.AppendLine("<style>");
        sb.AppendLine("body { font-family: Arial, sans-serif; padding: 20px; background: #f5f5f5; }");
        sb.AppendLine("h1 { color: #333; text-align: center; }");
        sb.AppendLine("table { width: 100%; border-collapse: collapse; background: white; box-shadow: 0 2px 4px rgba(0,0,0,0.1); }");
        sb.AppendLine("th { background: #4CAF50; color: white; padding: 12px; text-align: right; }");
        sb.AppendLine("td { padding: 10px; border-bottom: 1px solid #ddd; text-align: right; }");
        sb.AppendLine("tr:hover { background: #f5f5f5; }");
        sb.AppendLine(".date { color: #666; font-size: 0.9em; text-align: left; }");
        sb.AppendLine(".footer { text-align: center; color: #666; margin-top: 20px; font-size: 0.8em; }");
        sb.AppendLine("</style>");
        sb.AppendLine("</head>");
        sb.AppendLine("<body>");
        sb.AppendLine($"<h1>{title}</h1>");
        sb.AppendLine($"<p class='date'>تاريخ التقرير: {DateTime.Now:yyyy-MM-dd HH:mm}</p>");
        sb.AppendLine("<table>");
        sb.AppendLine("<thead><tr>");

        foreach (DataColumn col in data.Columns)
        {
            sb.AppendLine($"<th>{System.Net.WebUtility.HtmlEncode(col.ColumnName)}</th>");
        }

        sb.AppendLine("</tr></thead>");
        sb.AppendLine("<tbody>");

        foreach (DataRow row in data.Rows)
        {
            sb.AppendLine("<tr>");
            foreach (var item in row.ItemArray)
            {
                sb.AppendLine($"<td>{System.Net.WebUtility.HtmlEncode(item?.ToString() ?? "")}</td>");
            }
            sb.AppendLine("</tr>");
        }

        sb.AppendLine("</tbody>");
        sb.AppendLine("</table>");
        sb.AppendLine($"<p class='footer'>مخزن الندا للأدوية - نظام إدارة المخزن</p>");
        sb.AppendLine("</body>");
        sb.AppendLine("</html>");

        await File.WriteAllTextAsync(filePath, sb.ToString(), Encoding.UTF8);
        return filePath;
    }

    public async Task ExportDailySalesReportAsync(string filePath, DateTime date, IEnumerable<(string OrderNumber, string Pharmacy, decimal Total, string Status)> orders, decimal totalSales, int orderCount)
    {
        var dt = new DataTable();
        dt.Columns.Add("رقم الطلب", typeof(string));
        dt.Columns.Add("الصيدلية", typeof(string));
        dt.Columns.Add("الإجمالي", typeof(decimal));
        dt.Columns.Add("الحالة", typeof(string));

        foreach (var order in orders)
        {
            dt.Rows.Add(order.OrderNumber, order.Pharmacy, order.Total, order.Status);
        }

        dt.AcceptChanges();
        var summary = dt.Clone();
        summary.Columns.Add("ملاحظات", typeof(string));

        await ExportToCsvAsync(dt, Path.ChangeExtension(filePath, ".csv"));
        await ExportToHtmlAsync(dt, Path.ChangeExtension(filePath, ".html"), $"تقرير المبيعات اليومية - {date:yyyy-MM-dd}");
    }

    public async Task ExportInventoryReportAsync(string filePath, IEnumerable<(string Name, string Category, int Quantity, decimal Price, string ExpiryDate, string Status)> products)
    {
        var dt = new DataTable();
        dt.Columns.Add("اسم المنتج", typeof(string));
        dt.Columns.Add("الفئة", typeof(string));
        dt.Columns.Add("الكمية", typeof(int));
        dt.Columns.Add("سعر الوحدة", typeof(decimal));
        dt.Columns.Add("تاريخ الانتهاء", typeof(string));
        dt.Columns.Add("الحالة", typeof(string));

        foreach (var p in products)
        {
            dt.Rows.Add(p.Name, p.Category, p.Quantity, p.Price, p.ExpiryDate, p.Status);
        }

        await ExportToCsvAsync(dt, Path.ChangeExtension(filePath, ".csv"));
        await ExportToHtmlAsync(dt, Path.ChangeExtension(filePath, ".html"), "تقرير المخزون");
    }

    public async Task ExportDebtReportAsync(string filePath, IEnumerable<(string Name, decimal Balance, string Status)> pharmacies)
    {
        var dt = new DataTable();
        dt.Columns.Add("اسم الصيدلية", typeof(string));
        dt.Columns.Add("الرصيد", typeof(decimal));
        dt.Columns.Add("الحالة", typeof(string));

        foreach (var p in pharmacies)
        {
            dt.Rows.Add(p.Name, p.Balance, p.Status);
        }

        var htmlPath = Path.ChangeExtension(filePath, ".html");
        await ExportToHtmlAsync(dt, htmlPath, "تقرير المستحقات");
    }

    public async Task ExportToMultiSheetExcelAsync(Dictionary<string, DataTable> sheets, string filePath)
    {
        var csvFiles = new List<string>();

        foreach (var sheet in sheets)
        {
            var tempFile = Path.Combine(Path.GetTempPath(), $"{sheet.Key}_{Guid.NewGuid()}.csv");
            await ExportToCsvAsync(sheet.Value, tempFile);
            csvFiles.Add(tempFile);
        }

        var allCsv = new StringBuilder();
        allCsv.AppendLine("# Multi-sheet Export");
        allCsv.AppendLine($"# Created: {DateTime.Now:yyyy-MM-dd HH:mm}");

        foreach (var (name, _) in sheets)
        {
            allCsv.AppendLine($"# Sheet: {name}");
        }

        foreach (var csvFile in csvFiles)
        {
            var content = await File.ReadAllTextAsync(csvFile);
            allCsv.AppendLine();
            allCsv.AppendLine($"# === {Path.GetFileNameWithoutExtension(csvFile)} ===");
            allCsv.AppendLine(content);
            File.Delete(csvFile);
        }

        var finalPath = Path.ChangeExtension(filePath, ".csv");
        await File.WriteAllTextAsync(finalPath, allCsv.ToString(), Encoding.UTF8);
    }

    private static string EscapeCsvField(string field)
    {
        if (string.IsNullOrEmpty(field)) return "\"\"";

        if (field.Contains(',') || field.Contains('"') || field.Contains('\n') || field.Contains('\r'))
        {
            return $"\"{field.Replace("\"", "\"\"")}\"";
        }

        return field;
    }
}

public class ExportOptions
{
    public string FileName { get; set; } = "report";
    public ExportFormat Format { get; set; } = ExportFormat.Csv;
    public string Title { get; set; } = "تقرير";
    public bool IncludeTimestamp { get; set; } = true;
    public bool IncludeHeader { get; set; } = true;
}

public enum ExportFormat
{
    Csv,
    Excel,
    Html,
    Json
}

public static class ExportExtensions
{
    public static string GetFileName(this ExportFormat format, string baseName)
    {
        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var name = format switch
        {
            ExportFormat.Csv => $"{baseName}_{timestamp}.csv",
            ExportFormat.Excel => $"{baseName}_{timestamp}.xlsx",
            ExportFormat.Html => $"{baseName}_{timestamp}.html",
            ExportFormat.Json => $"{baseName}_{timestamp}.json",
            _ => $"{baseName}_{timestamp}.csv"
        };
        return name;
    }
}