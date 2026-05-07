using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AlNeda.Core.Entities;

public class Payment
{
    public int Id { get; set; }

    public int PharmacyId { get; set; }

    [ForeignKey(nameof(PharmacyId))]
    public Pharmacy Pharmacy { get; set; } = null!;

    public int? OrderId { get; set; }

    [ForeignKey(nameof(OrderId))]
    public Order? Order { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }

    [MaxLength(20)]
    public string PaymentStatus { get; set; } = "partial";

    [MaxLength(20)]
    public string PaymentType { get; set; } = "cash";

    [Column(TypeName = "decimal(18,2)")]
    public decimal AmountPaid { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal RemainingAmount { get; set; }

    public string PaymentNotes { get; set; } = string.Empty;

    public DateTime Date { get; set; } = DateTime.Now;
}
