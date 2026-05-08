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
