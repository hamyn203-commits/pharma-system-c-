namespace AlNeda.Core.Models;

public class FinancialSummaryDto
{
    public decimal TotalRevenue { get; set; }
    public decimal TotalExpenses { get; set; }
    public decimal NetProfit { get; set; }
    public decimal ProfitMargin { get; set; }
    public decimal TotalTax { get; set; }
    public decimal TotalDiscount { get; set; }
    public decimal TotalRefunds { get; set; }

    public decimal RevenueChange { get; set; }
    public decimal ExpenseChange { get; set; }
    public decimal ProfitChange { get; set; }

    public int TotalOrders { get; set; }
    public int PaidOrders { get; set; }
    public int PendingOrders { get; set; }
    public int CancelledOrders { get; set; }

    public decimal AverageOrderValue { get; set; }
    public decimal TotalReceivables { get; set; }
    public decimal TotalPayables { get; set; }
}

public class RevenueReportDto
{
    public DateTime Date { get; set; }
    public string Period { get; set; } = string.Empty;
    public decimal GrossRevenue { get; set; }
    public decimal Discounts { get; set; }
    public decimal Refunds { get; set; }
    public decimal NetRevenue { get; set; }
    public decimal Tax { get; set; }
    public int OrderCount { get; set; }
    public int ItemCount { get; set; }
}

public class ExpenseReportDto
{
    public string Category { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal Percentage { get; set; }
    public int Count { get; set; }
    public string Trend { get; set; } = "stable";
}

public class ProfitReportDto
{
    public DateTime Date { get; set; }
    public decimal Revenue { get; set; }
    public decimal CostOfGoods { get; set; }
    public decimal GrossProfit { get; set; }
    public decimal GrossMargin { get; set; }
    public decimal OperatingExpenses { get; set; }
    public decimal NetProfit { get; set; }
    public decimal NetMargin { get; set; }
}

public class ReportFilterDto
{
    public DateTime FromDate { get; set; } = DateTime.Today.AddMonths(-1);
    public DateTime ToDate { get; set; } = DateTime.Today;
    public int? PharmacyId { get; set; }
    public string? Category { get; set; }
    public string? Status { get; set; }
    public string PeriodType { get; set; } = "daily";
}

public class ChartDataPoint
{
    public string Label { get; set; } = string.Empty;
    public decimal Value { get; set; }
    public decimal? ComparisonValue { get; set; }
    public string Color { get; set; } = "#FF10B981";
}

public class PeriodComparisonDto
{
    public string Metric { get; set; } = string.Empty;
    public decimal CurrentPeriod { get; set; }
    public decimal PreviousPeriod { get; set; }
    public decimal Change { get; set; }
    public double ChangePercent { get; set; }
    public string Trend { get; set; } = "up";
}

public class TopProductFinancialDto
{
    public int Rank { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public int QuantitySold { get; set; }
    public decimal Revenue { get; set; }
    public decimal Cost { get; set; }
    public decimal Profit { get; set; }
    public decimal Margin { get; set; }
    public int StockRemaining { get; set; }
}

public class PharmacyFinancialDto
{
    public int Rank { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int OrderCount { get; set; }
    public decimal TotalSales { get; set; }
    public decimal TotalPayments { get; set; }
    public decimal Balance { get; set; }
    public decimal AverageOrderValue { get; set; }
    public DateTime LastOrderDate { get; set; }
}

public class FinancialReportBundle
{
    public FinancialSummaryDto Summary { get; set; } = new();
    public List<RevenueReportDto> Revenue { get; set; } = [];
    public List<ExpenseReportDto> Expenses { get; set; } = [];
    public List<ProfitReportDto> ProfitLoss { get; set; } = [];
    public List<ChartDataPoint> RevenueTrend { get; set; } = [];
    public List<ChartDataPoint> ExpenseBreakdown { get; set; } = [];
    public List<PeriodComparisonDto> Comparison { get; set; } = [];
    public List<TopProductFinancialDto> TopProducts { get; set; } = [];
    public List<PharmacyFinancialDto> TopPharmacies { get; set; } = [];
}
