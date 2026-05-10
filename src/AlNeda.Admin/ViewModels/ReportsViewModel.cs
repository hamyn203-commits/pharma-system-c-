using System.Collections.ObjectModel;
using System.Data;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using AlNeda.Admin.Services;
using AlNeda.Core.Models;
using AlNeda.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AlNeda.Admin.ViewModels;

public partial class ReportsViewModel : ObservableObject
{
    private readonly ReportService _reportService;
    private readonly IDialogService _dialog;

    // ─── Report Types ───
    public ObservableCollection<ReportTypeTab> ReportTypes { get; } =
    [
        new("financial_overview", "📊", "نظرة عامة مالية", "ملخص مالي كامل"),
        new("revenue", "💰", "تحليل الإيرادات", "المبيعات والخصومات"),
        new("expenses", "📉", "تحليل المصروفات", "المرتجعات والمشتريات"),
        new("profit_loss", "📈", "الأرباح والخسائر", "صافي الربح والهوامش"),
        new("top_products", "🏆", "المنتجات الأفضل", "الأعلى إيراداً وربحاً"),
        new("pharmacies", "🏥", "الصيدليات", "تحليل مالي شامل"),
        new("comparison", "📋", "مقارنة الفترات", "نمو الأداء المالي"),
    ];

    [ObservableProperty] private ReportTypeTab? _selectedReportType;

    // ─── Filters ───
    [ObservableProperty] private DateTime _filterFrom = DateTime.Today.AddMonths(-1);
    [ObservableProperty] private DateTime _filterTo = DateTime.Today;
    [ObservableProperty] private string _selectedPeriodType = "daily";
    [ObservableProperty] private bool _showFilters = true;

    public List<string> PeriodTypes { get; } = ["daily", "monthly"];

    // ─── Loading ───
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string _statusMessage = "اختر تقريراً للبدء";
    [ObservableProperty] private bool _hasData;

    // ─── Financial Summary KPIs ───
    [ObservableProperty] private decimal _totalRevenue;
    [ObservableProperty] private decimal _totalExpenses;
    [ObservableProperty] private decimal _netProfit;
    [ObservableProperty] private decimal _profitMargin;
    [ObservableProperty] private decimal _revenueChange;
    [ObservableProperty] private decimal _expenseChange;
    [ObservableProperty] private decimal _profitChange;
    [ObservableProperty] private int _totalOrders;
    [ObservableProperty] private int _paidOrders;
    [ObservableProperty] private int _pendingOrders;
    [ObservableProperty] private decimal _averageOrder;
    [ObservableProperty] private decimal _totalTax;
    [ObservableProperty] private decimal _totalDiscount;
    [ObservableProperty] private decimal _totalRefunds;

    // ─── Data Collections ───
    [ObservableProperty] private ObservableCollection<RevenueReportDto> _revenueData = [];
    [ObservableProperty] private ObservableCollection<ExpenseReportDto> _expenseData = [];
    [ObservableProperty] private ObservableCollection<ProfitReportDto> _profitData = [];
    [ObservableProperty] private ObservableCollection<TopProductFinancialDto> _topProductsData = [];
    [ObservableProperty] private ObservableCollection<PharmacyFinancialDto> _pharmaciesData = [];
    [ObservableProperty] private ObservableCollection<PeriodComparisonDto> _comparisonData = [];

    // ─── Chart Data ───
    [ObservableProperty] private ObservableCollection<ChartDataPoint> _revenueTrend = [];
    [ObservableProperty] private ObservableCollection<ChartDataPoint> _categoryRevenue = [];
    [ObservableProperty] private ObservableCollection<ChartDataPoint> _expenseBreakdown = [];

    // ─── Grid Data ───
    [ObservableProperty] private DataTable? _gridData;
    [ObservableProperty] private bool _isGridView = true;

    public ReportsViewModel(ReportService reportService, IDialogService dialog)
    {
        _reportService = reportService;
        _dialog = dialog;
    }

    partial void OnSelectedReportTypeChanged(ReportTypeTab? value)
    {
        if (value != null) _ = LoadReportAsync(value.Id);
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        SelectedReportType ??= ReportTypes[0];
        await LoadReportAsync(SelectedReportType.Id);
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        if (SelectedReportType != null)
            await LoadReportAsync(SelectedReportType.Id);
    }

    private async Task LoadReportAsync(string reportId)
    {
        IsLoading = true;
        HasData = false;
        StatusMessage = "جاري تحميل التقرير...";

        try
        {
            switch (reportId)
            {
                case "financial_overview":
                    await LoadFinancialOverviewAsync();
                    break;
                case "revenue":
                    await LoadRevenueReportAsync();
                    break;
                case "expenses":
                    await LoadExpenseReportAsync();
                    break;
                case "profit_loss":
                    await LoadProfitLossAsync();
                    break;
                case "top_products":
                    await LoadTopProductsAsync();
                    break;
                case "pharmacies":
                    await LoadPharmaciesAsync();
                    break;
                case "comparison":
                    await LoadComparisonAsync();
                    break;
            }
            HasData = true;
            StatusMessage = "تم تحميل التقرير بنجاح";
        }
        catch (Exception ex)
        {
            StatusMessage = $"خطأ: {ex.Message}";
        }

        IsLoading = false;
    }

    private async Task LoadFinancialOverviewAsync()
    {
        var summary = await _reportService.GetFinancialSummaryAsync(FilterFrom, FilterTo);
        TotalRevenue = summary.TotalRevenue;
        TotalExpenses = summary.TotalExpenses;
        NetProfit = summary.NetProfit;
        ProfitMargin = summary.ProfitMargin;
        RevenueChange = summary.RevenueChange;
        ExpenseChange = summary.ExpenseChange;
        ProfitChange = summary.ProfitChange;
        TotalOrders = summary.TotalOrders;
        PaidOrders = summary.PaidOrders;
        PendingOrders = summary.PendingOrders;
        AverageOrder = summary.AverageOrderValue;
        TotalTax = summary.TotalTax;
        TotalDiscount = summary.TotalDiscount;
        TotalRefunds = summary.TotalRefunds;

        RevenueTrend = new ObservableCollection<ChartDataPoint>(
            await _reportService.GetRevenueTrendAsync(30));

        CategoryRevenue = new ObservableCollection<ChartDataPoint>(
            await _reportService.GetCategoryRevenueAsync(FilterFrom, FilterTo));
    }

    private async Task LoadRevenueReportAsync()
    {
        var data = await _reportService.GetRevenueReportAsync(FilterFrom, FilterTo, SelectedPeriodType);
        RevenueData = new ObservableCollection<RevenueReportDto>(data);

        GridData = await _reportService.GetRevenueDataTableAsync(FilterFrom, FilterTo, SelectedPeriodType);

        TotalRevenue = data.Sum(d => d.NetRevenue);
        TotalDiscount = data.Sum(d => d.Discounts);
        TotalTax = data.Sum(d => d.Tax);
        TotalRefunds = data.Sum(d => d.Refunds);
        TotalOrders = data.Sum(d => d.OrderCount);
        AverageOrder = TotalOrders > 0 ? TotalRevenue / TotalOrders : 0;

        RevenueTrend = new ObservableCollection<ChartDataPoint>(
            data.Select(d => new ChartDataPoint
            {
                Label = d.Period,
                Value = d.NetRevenue,
                Color = "#FF10B981"
            }));
    }

    private async Task LoadExpenseReportAsync()
    {
        var data = await _reportService.GetExpenseBreakdownAsync(FilterFrom, FilterTo);
        ExpenseData = new ObservableCollection<ExpenseReportDto>(data);

        GridData = await _reportService.GetExpenseDataTableAsync(FilterFrom, FilterTo);
        TotalExpenses = data.Sum(e => e.Amount);

        ExpenseBreakdown = new ObservableCollection<ChartDataPoint>(
            data.Select(e => new ChartDataPoint
            {
                Label = e.Category,
                Value = e.Amount,
                Color = e.Trend switch
                {
                    "up" => "#FFEF4444",
                    "down" => "#FF10B981",
                    _ => "#FFF59E0B"
                }
            }));
    }

    private async Task LoadProfitLossAsync()
    {
        var data = await _reportService.GetProfitLossAsync(FilterFrom, FilterTo);
        ProfitData = new ObservableCollection<ProfitReportDto>(data);

        GridData = await _reportService.GetProfitLossDataTableAsync(FilterFrom, FilterTo);

        if (data.Count > 0)
        {
            var last = data.Last();
            TotalRevenue = last.Revenue;
            NetProfit = last.NetProfit;
            ProfitMargin = last.NetMargin;
        }
        TotalRevenue = data.Sum(d => d.Revenue);
        NetProfit = data.Sum(d => d.NetProfit);

        RevenueTrend = new ObservableCollection<ChartDataPoint>(
            data.Select(d => new ChartDataPoint
            {
                Label = d.Date.ToString("MM/dd"),
                Value = d.Revenue,
                ComparisonValue = d.NetProfit,
                Color = "#FF10B981"
            }));
    }

    private async Task LoadTopProductsAsync()
    {
        var data = await _reportService.GetTopProductsFinancialAsync(FilterFrom, FilterTo);
        TopProductsData = new ObservableCollection<TopProductFinancialDto>(data);

        GridData = await _reportService.GetTopProductsFinancialDataTableAsync(FilterFrom, FilterTo);

        CategoryRevenue = new ObservableCollection<ChartDataPoint>(
            data.GroupBy(p => p.Category)
                .Select(g => new ChartDataPoint
                {
                    Label = g.Key,
                    Value = g.Sum(p => p.Revenue),
                    Color = "#FF3B82F6"
                }));
    }

    private async Task LoadPharmaciesAsync()
    {
        var data = await _reportService.GetPharmaciesFinancialAsync(FilterFrom, FilterTo);
        PharmaciesData = new ObservableCollection<PharmacyFinancialDto>(data);

        GridData = await _reportService.GetPharmaciesFinancialDataTableAsync(FilterFrom, FilterTo);

        TotalRevenue = data.Sum(p => p.TotalSales);
        TotalOrders = data.Sum(p => p.OrderCount);
    }

    private async Task LoadComparisonAsync()
    {
        var data = await _reportService.GetPeriodComparisonAsync(FilterFrom, FilterTo);
        ComparisonData = new ObservableCollection<PeriodComparisonDto>(data);

        var dt = new DataTable();
        dt.Columns.Add("المؤشر", typeof(string));
        dt.Columns.Add("الفترة الحالية", typeof(decimal));
        dt.Columns.Add("الفترة السابقة", typeof(decimal));
        dt.Columns.Add("التغيير", typeof(string));
        dt.Columns.Add("نسبة التغيير", typeof(string));
        dt.Columns.Add("الاتجاه", typeof(string));
        foreach (var c in data)
            dt.Rows.Add(c.Metric, c.CurrentPeriod, c.PreviousPeriod, c.Change, $"{c.ChangePercent:F1}%", c.Trend);
        GridData = dt;

        RevenueTrend = new ObservableCollection<ChartDataPoint>(
            data.Select(c => new ChartDataPoint
            {
                Label = c.Metric,
                Value = c.CurrentPeriod,
                ComparisonValue = c.PreviousPeriod,
                Color = c.Trend == "up" ? "#FF10B981" : "#FFEF4444"
            }));
    }

    // ─── Export ───

    [RelayCommand]
    private void ExportToCsv()
    {
        if (GridData == null) { StatusMessage = "لا توجد بيانات للتصدير"; return; }
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Title = "تصدير إلى CSV",
            Filter = "CSV (*.csv)|*.csv",
            FileName = $"تقرير_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
        };
        if (dialog.ShowDialog() != true) return;
        try
        {
            var sb = new System.Text.StringBuilder();
            foreach (DataColumn col in GridData.Columns)
                sb.Append(EscapeCsv(col.ColumnName)).Append(",");
            sb.AppendLine();

            foreach (DataRow row in GridData.Rows)
            {
                foreach (var item in row.ItemArray)
                    sb.Append(EscapeCsv(item?.ToString() ?? "")).Append(",");
                sb.AppendLine();
            }
            File.WriteAllText(dialog.FileName, sb.ToString(), System.Text.Encoding.UTF8);
            StatusMessage = $"تم التصدير: {Path.GetFileName(dialog.FileName)}";
        }
        catch (Exception ex) { StatusMessage = $"خطأ: {ex.Message}"; }
    }

    [RelayCommand]
    private void ExportToPdf()
    {
        if (GridData == null || GridData.Rows.Count == 0)
        { StatusMessage = "لا توجد بيانات للتصدير"; return; }

        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Title = "تصدير إلى PDF",
            Filter = "XPS Document (*.xps)|*.xps",
            FileName = $"تقرير_{DateTime.Now:yyyyMMdd_HHmmss}.xps"
        };
        if (dialog.ShowDialog() != true) return;

        try
        {
            var doc = BuildReportDocument();
            var xpsPath = dialog.FileName;
            using var xpsDoc = new System.Windows.Xps.Packaging.XpsDocument(xpsPath, FileAccess.Write);
            var writer = System.Windows.Xps.Packaging.XpsDocument.CreateXpsDocumentWriter(xpsDoc);
            writer.Write(((IDocumentPaginatorSource)doc).DocumentPaginator);
            StatusMessage = $"تم التصدير: {Path.GetFileName(xpsPath)}";
        }
        catch (Exception ex) { StatusMessage = $"خطأ: {ex.Message}"; }
    }

    [RelayCommand]
    private void PrintReport()
    {
        if (GridData == null || GridData.Rows.Count == 0)
        { StatusMessage = "لا توجد بيانات للطباعة"; return; }
        try
        {
            var doc = BuildReportDocument();
            var pd = new PrintDialog();
            if (pd.ShowDialog() == true)
            {
                pd.PrintDocument(((IDocumentPaginatorSource)doc).DocumentPaginator,
                    $"التقرير المالي - {SelectedReportType?.Title ?? ""}");
                StatusMessage = "تم إرسال التقرير للطباعة";
            }
        }
        catch (Exception ex) { StatusMessage = $"خطأ في الطباعة: {ex.Message}"; }
    }

    [RelayCommand]
    private void ToggleGridView()
    {
        IsGridView = !IsGridView;
    }

    private FlowDocument BuildReportDocument()
    {
        var doc = new FlowDocument
        {
            PagePadding = new Thickness(40),
            FontFamily = new FontFamily("Segoe UI")
        };

        doc.Blocks.Add(new Paragraph(new Run($"التقرير المالي - {SelectedReportType?.Title ?? ""}"))
        {
            FontSize = 22, FontWeight = FontWeights.Bold,
            Foreground = new SolidColorBrush(Color.FromRgb(85, 214, 107)),
            TextAlignment = TextAlignment.Center, Margin = new Thickness(0, 0, 0, 8)
        });

        doc.Blocks.Add(new Paragraph(new Run($"من {FilterFrom:yyyy-MM-dd} إلى {FilterTo:yyyy-MM-dd}"))
        {
            FontSize = 11, Foreground = Brushes.Gray,
            TextAlignment = TextAlignment.Center, Margin = new Thickness(0, 0, 0, 24)
        });

        if (GridData != null)
        {
            var table = new Table();
            for (int i = 0; i < GridData.Columns.Count; i++)
                table.Columns.Add(new TableColumn { Width = new GridLength(1, GridUnitType.Star) });

            var headerRow = new TableRow { Background = new SolidColorBrush(Color.FromRgb(85, 214, 107)) };
            foreach (DataColumn col in GridData.Columns)
                headerRow.Cells.Add(new TableCell(new Paragraph(new Run(col.ColumnName))
                { Foreground = Brushes.Black, FontWeight = FontWeights.Bold, TextAlignment = TextAlignment.Right }));
            table.RowGroups.Add(new TableRowGroup { Rows = { headerRow } });

            var dataGroup = new TableRowGroup();
            foreach (DataRow row in GridData.Rows)
            {
                var dataRow = new TableRow();
                foreach (var item in row.ItemArray)
                    dataRow.Cells.Add(new TableCell(new Paragraph(new Run(item?.ToString() ?? ""))
                    { Foreground = Brushes.White, TextAlignment = TextAlignment.Right }));
                dataGroup.Rows.Add(dataRow);
            }
            table.RowGroups.Add(dataGroup);
            doc.Blocks.Add(table);
        }

        doc.Blocks.Add(new Paragraph(new Run("مخزن الندا للأدوية - تقرير مالي"))
        { FontSize = 10, Foreground = Brushes.Gray, TextAlignment = TextAlignment.Center, Margin = new Thickness(0, 24, 0, 0) });

        return doc;
    }

    private static string EscapeCsv(string s) =>
        s.Contains(',') || s.Contains('"') ? $"\"{s.Replace("\"", "\"\"")}\"" : s;

    [RelayCommand]
    private void CopyToClipboard()
    {
        if (GridData == null) { StatusMessage = "لا توجد بيانات للنسخ"; return; }
        try
        {
            var sb = new System.Text.StringBuilder();
            foreach (DataColumn col in GridData.Columns)
                sb.Append(col.ColumnName).Append("\t");
            sb.AppendLine();
            foreach (DataRow row in GridData.Rows)
            {
                foreach (var item in row.ItemArray)
                    sb.Append(item?.ToString() ?? "").Append("\t");
                sb.AppendLine();
            }
            Clipboard.SetText(sb.ToString());
            StatusMessage = "تم نسخ البيانات إلى الحافظة";
        }
        catch (Exception ex) { StatusMessage = $"خطأ: {ex.Message}"; }
    }

    [RelayCommand]
    private void ToggleFilters()
    {
        ShowFilters = !ShowFilters;
    }
}

public record ReportTypeTab(string Id, string Icon, string Title, string Description);
