using AlNeda.Core.Entities;
using AlNeda.Data.UnitOfWork;

namespace AlNeda.Services;

public class AuditService : IAuditService
{
    private readonly IUnitOfWorkFactory _uowFactory;

    public AuditService(IUnitOfWorkFactory uowFactory)
    {
        _uowFactory = uowFactory;
    }

    public async Task<List<AuditLog>> GetAllAsync()
    {
        using var uow = _uowFactory.Create();
        return (await uow.GetRepository<AuditLog>().GetAllAsync()).OrderByDescending(l => l.CreatedAt).ToList();
    }

    public async Task LogAsync(string username, string action, string entity, string entityId, string details = "")
    {
        using var uow = _uowFactory.Create();
        var log = new AuditLog
        {
            Username = username,
            Action = action,
            Entity = entity,
            EntityId = entityId,
            Details = details,
            CreatedAt = DateTime.Now
        };
        await uow.GetRepository<AuditLog>().AddAsync(log);
        await uow.SaveChangesAsync();
    }

    public async Task LogWithChangesAsync(AuditLog auditLog)
    {
        using var uow = _uowFactory.Create();
        await uow.GetRepository<AuditLog>().AddAsync(auditLog);
        await uow.SaveChangesAsync();
    }

    public async Task LogPriceChangeAsync(string username, int productId, string productName, decimal oldPrice, decimal newPrice, string? reason = null)
    {
        var auditLog = AuditExtensions.CreatePriceChangeAudit(username, "Product", productId, oldPrice, newPrice, reason);
        await LogWithChangesAsync(auditLog);
    }

    public async Task LogBalanceChangeAsync(string username, int pharmacyId, string pharmacyName, decimal oldBalance, decimal newBalance, string transactionType)
    {
        var auditLog = AuditExtensions.CreateBalanceChangeAudit(username, "Pharmacy", pharmacyId, oldBalance, newBalance, transactionType);
        await LogWithChangesAsync(auditLog);
    }

    public async Task LogEntityUpdateAsync(string username, string entity, int entityId, params (string FieldName, object? OldValue, object? NewValue)[] changes)
    {
        var auditLog = AuditExtensions.CreateAuditLog(username, "Update", entity, entityId.ToString(), changes);
        await LogWithChangesAsync(auditLog);
    }
}