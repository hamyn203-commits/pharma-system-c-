using AlNeda.Core.Entities;

namespace AlNeda.Services;

public static class AuditExtensions
{
    public static AuditLog CreateAuditLog(string username, string action, string entity, string entityId, params (string FieldName, object? OldValue, object? NewValue)[] changes)
    {
        var details = new List<string>();
        string? firstField = null;
        string? firstOld = null;
        string? firstNew = null;

        foreach (var (fieldName, oldValue, newValue) in changes)
        {
            var oldStr = oldValue?.ToString() ?? "فارغ";
            var newStr = newValue?.ToString() ?? "فارغ";

            if (oldStr != newStr)
            {
                details.Add($"{fieldName}: {oldStr} → {newStr}");

                if (firstField == null)
                {
                    firstField = fieldName;
                    firstOld = oldStr;
                    firstNew = newStr;
                }
            }
        }

        return new AuditLog
        {
            Username = username,
            Action = action,
            Entity = entity,
            EntityId = entityId,
            FieldName = firstField,
            OldValue = firstOld,
            NewValue = firstNew,
            Details = string.Join("; ", details),
            CreatedAt = DateTime.Now
        };
    }

    public static AuditLog CreatePriceChangeAudit(string username, string entity, int entityId, decimal oldPrice, decimal newPrice, string? reason = null)
    {
        var details = $"تغيير السعر: {oldPrice:F2} → {newPrice:F2}";
        if (!string.IsNullOrEmpty(reason))
            details += $" (السبب: {reason})";

        return new AuditLog
        {
            Username = username,
            Action = "update",
            Entity = entity,
            EntityId = entityId.ToString(),
            FieldName = "UnitPrice",
            OldValue = oldPrice.ToString("F2"),
            NewValue = newPrice.ToString("F2"),
            Details = details,
            CreatedAt = DateTime.Now
        };
    }

    public static AuditLog CreateBalanceChangeAudit(string username, string entity, int entityId, decimal oldBalance, decimal newBalance, string transactionType)
    {
        var diff = newBalance - oldBalance;
        var direction = diff >= 0 ? "زيادة" : "نقصان";

        var details = $"تغيير الرصيد: {oldBalance:F2} → {newBalance:F2} ({direction}: {Math.Abs(diff):F2}) - نوع المعاملة: {transactionType}";

        return new AuditLog
        {
            Username = username,
            Action = "update",
            Entity = entity,
            EntityId = entityId.ToString(),
            FieldName = "Balance",
            OldValue = oldBalance.ToString("F2"),
            NewValue = newBalance.ToString("F2"),
            Details = details,
            CreatedAt = DateTime.Now
        };
    }

    public static AuditLog CreateOrderStatusChangeAudit(string username, int orderId, string orderNumber, string oldStatus, string newStatus)
    {
        return new AuditLog
        {
            Username = username,
            Action = "transition",
            Entity = "Order",
            EntityId = orderId.ToString(),
            FieldName = "Status",
            OldValue = oldStatus,
            NewValue = newStatus,
            Details = $"تغيير حالة الطلب #{orderNumber}: {oldStatus} → {newStatus}",
            CreatedAt = DateTime.Now
        };
    }

    public static AuditLog CreateQuantityChangeAudit(string username, int productId, string productName, int oldQuantity, int newQuantity, string? reason = null)
    {
        var diff = newQuantity - oldQuantity;
        var details = $"تغيير كمية المنتج '{productName}': {oldQuantity} → {newQuantity} (التغير: {diff:+0;-0;0})";
        if (!string.IsNullOrEmpty(reason))
            details += $" - {reason}";

        return new AuditLog
        {
            Username = username,
            Action = "update",
            Entity = "Product",
            EntityId = productId.ToString(),
            FieldName = "Quantity",
            OldValue = oldQuantity.ToString(),
            NewValue = newQuantity.ToString(),
            Details = details,
            CreatedAt = DateTime.Now
        };
    }
}