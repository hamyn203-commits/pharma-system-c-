using System.ComponentModel.DataAnnotations.Schema;

namespace AlNeda.Core.Entities;

public class MarketingOfferProduct : IEntity
{
    public int Id { get; set; }

    public int MarketingOfferId { get; set; }

    [ForeignKey(nameof(MarketingOfferId))]
    public MarketingOffer Offer { get; set; } = null!;

    public int ProductId { get; set; }

    [ForeignKey(nameof(ProductId))]
    public Product Product { get; set; } = null!;

    public int OfferQuantity { get; set; }

    public int MinimumOrder { get; set; } = 1;

    [Column(TypeName = "decimal(18,2)")]
    public decimal OldPrice { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal NewPrice { get; set; }

    public string GiftProduct { get; set; } = string.Empty;
}
