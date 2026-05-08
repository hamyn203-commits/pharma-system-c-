using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AlNeda.Core.Entities;

public class Return : IEntity
{
    public int Id { get; set; }

    [Required, MaxLength(50)]
    public string ReturnNumber { get; set; } = string.Empty;

    public int PharmacyId { get; set; }

    [ForeignKey(nameof(PharmacyId))]
    public Pharmacy Pharmacy { get; set; } = null!;

    public int? OrderId { get; set; }

    [ForeignKey(nameof(OrderId))]
    public Order? Order { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalAmount { get; set; }

    [MaxLength(20)]
    public string Status { get; set; } = "pending";

    [MaxLength(20)]
    public string? ReturnType { get; set; }

    public string? Reason { get; set; }

    public string Notes { get; set; } = string.Empty;

    public bool StockAdjusted { get; set; }
    public bool BalanceAdjusted { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal BalanceBefore { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal BalanceAfter { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public ICollection<ReturnItem> Items { get; set; } = new List<ReturnItem>();
}
