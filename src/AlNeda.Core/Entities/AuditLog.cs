using System.ComponentModel.DataAnnotations;

namespace AlNeda.Core.Entities;

public class AuditLog
{
    public int Id { get; set; }

    [MaxLength(100)]
    public string Username { get; set; } = "system";

    [Required, MaxLength(50)]
    public string Action { get; set; } = string.Empty;

    [MaxLength(50)]
    public string Entity { get; set; } = string.Empty;

    [MaxLength(50)]
    public string EntityId { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string Details { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
