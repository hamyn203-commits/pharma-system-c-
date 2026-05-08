using AlNeda.Core.Entities;

namespace AlNeda.Core.Models;

public class LoginRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class SyncRequest
{
    public DateTime LastSyncTime { get; set; }
    public List<Product> Products { get; set; } = [];
    public List<Category> Categories { get; set; } = [];
    public List<Order> Orders { get; set; } = [];
    public List<Pharmacy> Pharmacies { get; set; } = [];
    public List<Payment> Payments { get; set; } = [];
}

public class SyncResponse
{
    public List<Product> NewOrUpdatedProducts { get; set; } = [];
    public List<Category> NewOrUpdatedCategories { get; set; } = [];
    public List<Order> NewOrUpdatedOrders { get; set; } = [];
    public List<Pharmacy> NewOrUpdatedPharmacies { get; set; } = [];
    public List<Payment> NewOrUpdatedPayments { get; set; } = [];
    public DateTime ServerTime { get; set; }
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
}