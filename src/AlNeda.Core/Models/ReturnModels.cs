namespace AlNeda.Core.Models;

public class ReturnDto
{
    public int Id { get; set; }
    public string ReturnNumber { get; set; } = string.Empty;
    public int PharmacyId { get; set; }
    public string? PharmacyName { get; set; }
    public int? OrderId { get; set; }
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = "pending";
    public string? ReturnType { get; set; }
    public string? Reason { get; set; }
    public string Notes { get; set; } = string.Empty;
    public bool StockAdjusted { get; set; }
    public bool BalanceAdjusted { get; set; }
    public decimal BalanceBefore { get; set; }
    public decimal BalanceAfter { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<ReturnItemDto> Items { get; set; } = [];
}

public class ReturnItemDto
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string? ProductName { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
}

public class CreateReturnRequest
{
    public int PharmacyId { get; set; }
    public int? OrderId { get; set; }
    public decimal TotalAmount { get; set; }
    public string? ReturnType { get; set; }
    public string? Reason { get; set; }
    public string Notes { get; set; } = string.Empty;
    public bool StockAdjusted { get; set; } = true;
    public bool BalanceAdjusted { get; set; } = true;
    public List<CreateReturnItemRequest> Items { get; set; } = [];
}

public class CreateReturnItemRequest
{
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}
