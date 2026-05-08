using System.ComponentModel.DataAnnotations;

namespace AlNeda.Core.Entities;

public class AuditLog : IEntity
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

    [MaxLength(100)]
    public string? FieldName { get; set; }

    public string? OldValue { get; set; }

    public string? NewValue { get; set; }

    [MaxLength(1000)]
    public string Details { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

public static class AuditLogFactory
{
    public static AuditLog CreateChange(string username, string entity, string entityId, string fieldName, string? oldValue, string? newValue, string details = "")
    {
        return new AuditLog
        {
            Username = username,
            Action = "update",
            Entity = entity,
            EntityId = entityId,
            FieldName = fieldName,
            OldValue = oldValue,
            NewValue = newValue,
            Details = details,
            CreatedAt = DateTime.Now
        };
    }

    public static AuditLog CreateAction(string username, string action, string entity, string entityId, string details = "")
    {
        return new AuditLog
        {
            Username = username,
            Action = action,
            Entity = entity,
            EntityId = entityId,
            Details = details,
            CreatedAt = DateTime.Now
        };
    }

    public static AuditLog CreateForPriceChange(string username, int productId, string productName, decimal oldPrice, decimal newPrice)
    {
        return CreateChange(username, "Product", productId.ToString(), "UnitPrice",
            oldPrice.ToString("F2"), newPrice.ToString("F2"),
            $"تم تغيير سعر المنتج '{productName}' من {oldPrice:F2} إلى {newPrice:F2}");
    }

    public static AuditLog CreateForBalanceChange(string username, int pharmacyId, string pharmacyName, decimal oldBalance, decimal newBalance, string reason)
    {
        return CreateChange(username, "Pharmacy", pharmacyId.ToString(), "Balance",
            oldBalance.ToString("F2"), newBalance.ToString("F2"),
            $"تم تغيير رصيد الصيدلية '{pharmacyName}': {reason} - من {oldBalance:F2} إلى {newBalance:F2}");
    }
}