using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AlNeda.Core.Entities;

public class Order : IEntity
{
    public int Id { get; set; }

    [Required, MaxLength(50)]
    public string OrderNumber { get; set; } = string.Empty;

    public int PharmacyId { get; set; }

    [ForeignKey(nameof(PharmacyId))]
    public Pharmacy Pharmacy { get; set; } = null!;

    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalAmount { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal Discount { get; set; }

    [MaxLength(20)]
    public string DiscountType { get; set; } = "value";

    [Column(TypeName = "decimal(18,2)")]
    public decimal FinalTotal { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal BalanceBefore { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal BalanceAfter { get; set; }

    [MaxLength(20)]
    public string Status { get; set; } = "pending";

    [MaxLength(200)]
    public string DeliveryPerson { get; set; } = string.Empty;

    public string Notes { get; set; } = string.Empty;

    public DateTime? LastStatusUpdate { get; set; }

    [MaxLength(500)]
    public string ExpectedDeliveryNote { get; set; } = string.Empty;

    [MaxLength(20)]
    public string PaymentStatus { get; set; } = "unpaid";

    [MaxLength(20)]
    public string PaymentType { get; set; } = string.Empty;

    [Column(TypeName = "decimal(18,2)")]
    public decimal AmountPaid { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal RemainingAmount { get; set; }

    public string PaymentNotes { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
    public ICollection<OrderStatusHistory> StatusHistory { get; set; } = new List<OrderStatusHistory>();
}
