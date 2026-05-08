using AlNeda.Core.Entities;

namespace AlNeda.Services;

public interface IAuditService
{
    Task<List<AuditLog>> GetAllAsync();
    Task LogAsync(string username, string action, string entity, string entityId, string details = "");
    Task LogWithChangesAsync(AuditLog auditLog);
    Task LogPriceChangeAsync(string username, int productId, string productName, decimal oldPrice, decimal newPrice, string? reason = null);
    Task LogBalanceChangeAsync(string username, int pharmacyId, string pharmacyName, decimal oldBalance, decimal newBalance, string transactionType);
    Task LogEntityUpdateAsync(string username, string entity, int entityId, params (string FieldName, object? OldValue, object? NewValue)[] changes);
}