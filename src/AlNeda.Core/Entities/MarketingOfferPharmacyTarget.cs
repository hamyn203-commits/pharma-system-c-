using System.ComponentModel.DataAnnotations.Schema;

namespace AlNeda.Core.Entities;

public class MarketingOfferPharmacyTarget : IEntity
{
    public int Id { get; set; }

    public int MarketingOfferId { get; set; }

    [ForeignKey(nameof(MarketingOfferId))]
    public MarketingOffer Offer { get; set; } = null!;

    public int PharmacyId { get; set; }

    [ForeignKey(nameof(PharmacyId))]
    public Pharmacy Pharmacy { get; set; } = null!;
}
