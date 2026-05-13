using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AlNeda.Core.Entities;

public class Pharmacy : IEntity
{
    public int Id { get; set; }

    [Required, MaxLength(300)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string Address { get; set; } = string.Empty;

    [MaxLength(50)]
    public string Phone { get; set; } = string.Empty;

    [Column(TypeName = "decimal(18,2)")]
    public decimal Balance { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    [MaxLength(20)]
    public string AccountStatus { get; set; } = "active";

    public DateTime? ApprovedAt { get; set; }
    public DateTime? BlockedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }

    [MaxLength(200)]
    public string? DeviceId { get; set; }

    [MaxLength(200)]
    public string? OwnerName { get; set; }

    [System.Text.Json.Serialization.JsonIgnore]
    public bool IsApproved => string.Equals(AccountStatus, "active", StringComparison.OrdinalIgnoreCase);

    public ICollection<Order> Orders { get; set; } = new List<Order>();
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
}
