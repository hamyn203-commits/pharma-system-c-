namespace AlNeda.Core.Models;

// ─── BOGO (Buy One Get One) Models ─────────────────────────────────

public class CreateBogoRuleRequest
{
    public int MarketingOfferId { get; set; }
    public int BuyQuantity { get; set; } = 2;
    public int FreeQuantity { get; set; } = 1;
    public bool ApplyToCheapestItem { get; set; } = true;
    public int? TargetProductId { get; set; }
    public int MaxApplicationsPerOrder { get; set; } = 1;
    public string DisplayLabel { get; set; } = string.Empty;
}

public class UpdateBogoRuleRequest
{
    public int? BuyQuantity { get; set; }
    public int? FreeQuantity { get; set; }
    public bool? ApplyToCheapestItem { get; set; }
    public int? TargetProductId { get; set; }
    public int? MaxApplicationsPerOrder { get; set; }
    public string? DisplayLabel { get; set; }
}

public class BogoRuleDto
{
    public int Id { get; set; }
    public int MarketingOfferId { get; set; }
    public string OfferTitle { get; set; } = string.Empty;
    public int BuyQuantity { get; set; }
    public int FreeQuantity { get; set; }
    public bool ApplyToCheapestItem { get; set; }
    public int? TargetProductId { get; set; }
    public string? TargetProductName { get; set; }
    public int MaxApplicationsPerOrder { get; set; }
    public string DisplayLabel { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty; // "اشتر 2 واحصل على 1 مجاناً"
    public DateTime CreatedAt { get; set; }
}

public class BogoCalculationResult
{
    public bool Applied { get; set; }
    public string Description { get; set; } = string.Empty;
    public int TimesApplied { get; set; }
    public decimal TotalDiscount { get; set; }
    public List<BogoDiscountedItem> DiscountedItems { get; set; } = [];
}

public class BogoDiscountedItem
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal OriginalPrice { get; set; }
    public decimal DiscountedPrice { get; set; }
    public string Label { get; set; } = string.Empty; // "مجاناً" أو "خصم"
}
