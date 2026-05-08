namespace AlNeda.Core.Models;

public class ProductDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Barcode { get; set; }
    public string Category { get; set; } = "عام";
    public int? CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public string Company { get; set; } = "غير محدد";
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public string? ExpiryDate { get; set; }
    public string? ImagePath { get; set; }
    public string? ImageUrl { get; set; }
    public string Description { get; set; } = string.Empty;
    public int IsActive { get; set; } = 1;
    public DateTime CreatedAt { get; set; }
}

public class CreateProductRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Barcode { get; set; }
    public int? CategoryId { get; set; }
    public string Company { get; set; } = "غير محدد";
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public string? ExpiryDate { get; set; }
    public string? ImagePath { get; set; }
    public string? ImageUrl { get; set; }
    public string Description { get; set; } = string.Empty;
    public int IsActive { get; set; } = 1;
    public string? ProductImagesJson { get; set; }
}

public class UpdateProductRequest
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Barcode { get; set; }
    public int? CategoryId { get; set; }
    public string Company { get; set; } = "غير محدد";
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public string? ExpiryDate { get; set; }
    public string? ImagePath { get; set; }
    public string? ImageUrl { get; set; }
    public string Description { get; set; } = string.Empty;
    public int IsActive { get; set; } = 1;
    public string? ProductImagesJson { get; set; }
}
