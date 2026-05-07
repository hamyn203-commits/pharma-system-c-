using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AlNeda.Core.Entities;

public class Product
{
    public int Id { get; set; }

    [Required, MaxLength(500)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? Barcode { get; set; }

    [MaxLength(200)]
    public string Category { get; set; } = "عام";

    public int? CategoryId { get; set; }

    [ForeignKey(nameof(CategoryId))]
    public Category? CategoryObj { get; set; }

    [MaxLength(200)]
    public string Company { get; set; } = "غير محدد";

    public int Quantity { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal UnitPrice { get; set; }

    [MaxLength(20)]
    public string? ExpiryDate { get; set; }

    [MaxLength(500)]
    public string? ImagePath { get; set; }

    [MaxLength(1000)]
    public string? ImageUrl { get; set; }

    public string ProductImagesJson { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public int IsActive { get; set; } = 1;

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public DateTime UpdatedAt { get; set; } = DateTime.Now;
}
