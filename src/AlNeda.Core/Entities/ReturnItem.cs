using System.ComponentModel.DataAnnotations.Schema;

namespace AlNeda.Core.Entities;

public class ReturnItem : IEntity
{
    public int Id { get; set; }

    public int ReturnId { get; set; }

    [ForeignKey(nameof(ReturnId))]
    public Return Return { get; set; } = null!;

    public int ProductId { get; set; }

    [ForeignKey(nameof(ProductId))]
    public Product Product { get; set; } = null!;

    public int Quantity { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal UnitPrice { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalPrice { get; set; }
}
