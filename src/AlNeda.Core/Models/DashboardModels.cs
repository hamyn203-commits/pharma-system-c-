namespace AlNeda.Core.Models;

public class DashboardStats
{
    public decimal TotalReceivables { get; set; }
    public int ActiveOrders { get; set; }
    public int TotalPharmacies { get; set; }
    public int TotalProducts { get; set; }
    public decimal DailySales { get; set; }
    public decimal MonthlySales { get; set; }

    // Percentage changes (simulated for now)
    public double ReceivablesChange { get; set; } = -2.5;
    public double OrdersChange { get; set; } = 12.0;
    public double PharmaciesChange { get; set; } = 5.0;
    public double ProductsChange { get; set; } = 0.8;
    public double DailySalesChange { get; set; } = 15.3;
    public double MonthlySalesChange { get; set; } = 8.4;
}

public class SalesDataPoint
{
    public string Label { get; set; } = string.Empty;
    public decimal Value { get; set; }
}

public class DashboardAnalyticsDto
{
    public SalesMovementInsight SalesMovement { get; set; } = new();
    public TopProductInsight TopProduct { get; set; } = new();
    public List<TopProductInsight> TopProducts { get; set; } = [];
    public TopPharmacyInsight TopPharmacy { get; set; } = new();
    public List<TopPharmacyInsight> TopPharmacies { get; set; } = [];
    public List<InactivePharmacyInsight> InactivePharmacies { get; set; } = [];
}

public class SalesMovementInsight
{
    public decimal CurrentPeriodSales { get; set; }
    public decimal PreviousPeriodSales { get; set; }
    public double GrowthPercentage { get; set; }
    public int OrdersCount { get; set; }
    public decimal AverageOrderValue { get; set; }
    public int ActiveBuyingPharmacies { get; set; }
    public decimal SalesPerActivePharmacy { get; set; }
    public string BestSalesDay { get; set; } = "-";
    public decimal BestSalesDayValue { get; set; }
    public string TrendLabel { get; set; } = "لا توجد بيانات كافية";
}

public class TopProductInsight
{
    public string ProductName { get; set; } = "لا توجد مبيعات";
    public string Category { get; set; } = "-";
    public int QuantitySold { get; set; }
    public decimal Revenue { get; set; }
    public int OrdersCount { get; set; }
}

public class TopPharmacyInsight
{
    public string PharmacyName { get; set; } = "لا توجد مبيعات";
    public string Phone { get; set; } = "-";
    public decimal TotalPurchases { get; set; }
    public int OrdersCount { get; set; }
    public DateTime? LastOrderDate { get; set; }
}

public class InactivePharmacyInsight
{
    public string PharmacyName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public DateTime? LastOrderDate { get; set; }
    public int DaysSinceLastOrder { get; set; }
    public string InactivityLabel { get; set; } = string.Empty;
    public string LastProductName { get; set; } = "لا يوجد";
    public decimal LastOrderValue { get; set; }
}
