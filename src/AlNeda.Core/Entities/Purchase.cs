using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AlNeda.Core.Entities;

public class Purchase : IEntity
{
    public int Id { get; set; }

    [Required, MaxLength(100)]
    public string InvoiceNumber { get; set; } = string.Empty;

    public int SupplierId { get; set; }

    [ForeignKey(nameof(SupplierId))]
    public Supplier Supplier { get; set; } = null!;

    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalAmount { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal AmountPaid { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal RemainingAmount { get; set; }

    [MaxLength(20)]
    public string Status { get; set; } = "unpaid";

    public string Notes { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public ICollection<PurchaseItem> Items { get; set; } = new List<PurchaseItem>();
}
