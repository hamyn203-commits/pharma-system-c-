using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AlNeda.Core.Entities;

public class MarketingOffer : IEntity
{
    public int Id { get; set; }

    [Required, MaxLength(180)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(260)]
    public string Subtitle { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    [MaxLength(400)]
    public string ImageUrl { get; set; } = string.Empty;

    public string AdditionalImageUrls { get; set; } = string.Empty;

    [MaxLength(50)]
    public string OfferType { get; set; } = "discount";

    // Version 1 audience rules are simple strings evaluated by the API: all, active_pharmacies_last_30d, geo:Cairo.
    [MaxLength(200)]
    public string AudienceRule { get; set; } = "all";

    // Active offers use an exclusive end time: StartsAt <= now < EndsAt.
    public DateTime StartsAt { get; set; } = DateTime.Now;

    public DateTime EndsAt { get; set; } = DateTime.Now.AddDays(7);

    [MaxLength(20)]
    public string Status { get; set; } = "draft";

    public int? QuantityLimit { get; set; }

    public int? RemainingQuantity { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? OldPrice { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? NewPrice { get; set; }

    public int? DiscountPercent { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    public ICollection<OfferEvent> Events { get; set; } = new List<OfferEvent>();
    public ICollection<Order> Orders { get; set; } = new List<Order>();
    public ICollection<MarketingOfferPharmacyTarget> PharmacyTargets { get; set; } = new List<MarketingOfferPharmacyTarget>();
    public ICollection<MarketingOfferProduct> OfferProducts { get; set; } = new List<MarketingOfferProduct>();
}
