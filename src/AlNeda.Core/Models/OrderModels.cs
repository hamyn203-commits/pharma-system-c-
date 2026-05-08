namespace AlNeda.Core.Models;

public class OrderDto
{
    public int Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public int PharmacyId { get; set; }
    public string? PharmacyName { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal Discount { get; set; }
    public string DiscountType { get; set; } = "value";
    public decimal FinalTotal { get; set; }
    public decimal AmountPaid { get; set; }
    public decimal BalanceBefore { get; set; }
    public decimal BalanceAfter { get; set; }
    public string Status { get; set; } = "pending";
    public DateTime CreatedAt { get; set; }
    public List<OrderItemDto> Items { get; set; } = [];
}

public class OrderItemDto
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string? ProductName { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
}

public class CreateOrderRequest
{
    public int PharmacyId { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal Discount { get; set; }
    public string DiscountType { get; set; } = "value";
    public List<CreateOrderItemRequest> Items { get; set; } = [];
}

public class CreateOrderItemRequest
{
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}

public class PurchaseDto
{
    public int Id { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public int SupplierId { get; set; }
    public string? SupplierName { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal AmountPaid { get; set; }
    public decimal RemainingAmount { get; set; }
    public string Status { get; set; } = "unpaid";
    public string Notes { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public List<PurchaseItemDto> Items { get; set; } = [];
}

public class PurchaseItemDto
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string? ProductName { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
}

public class CreatePurchaseRequest
{
    public string InvoiceNumber { get; set; } = string.Empty;
    public int SupplierId { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal AmountPaid { get; set; }
    public string Notes { get; set; } = string.Empty;
    public List<CreatePurchaseItemRequest> Items { get; set; } = [];
}

public class CreatePurchaseItemRequest
{
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}
