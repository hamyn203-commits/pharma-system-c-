using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AlNeda.Core.Entities;

public class OfferEvent : IEntity
{
    public int Id { get; set; }

    public int MarketingOfferId { get; set; }

    [ForeignKey(nameof(MarketingOfferId))]
    public MarketingOffer Offer { get; set; } = null!;

    public int PharmacyId { get; set; }

    [ForeignKey(nameof(PharmacyId))]
    public Pharmacy Pharmacy { get; set; } = null!;

    [MaxLength(200)]
    public string? DeviceId { get; set; }

    // Used for idempotent daily impression counting. If DeviceId is missing, the pharmacy-level fallback key is used.
    [MaxLength(220)]
    public string DeviceKey { get; set; } = string.Empty;

    [MaxLength(20)]
    public string EventType { get; set; } = "impression";

    public DateTime OccurredAt { get; set; } = DateTime.Now;

    [MaxLength(10)]
    public string EventDateKey { get; set; } = string.Empty;

    public bool IsUniqueDailyImpression { get; set; }

    public bool UsedDeviceFallback { get; set; }

    public bool IsRejected { get; set; }

    [MaxLength(120)]
    public string RejectionReason { get; set; } = string.Empty;
}
