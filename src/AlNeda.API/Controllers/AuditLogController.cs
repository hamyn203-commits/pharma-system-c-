using AlNeda.Core.Entities;
using AlNeda.Core.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AlNeda.API.Controllers;

[ApiController]
[Route("api/audit-logs")]
[Authorize]
public class AuditLogController : ControllerBase
{
    private readonly IDbContextFactory<Data.AppDbContext> _contextFactory;

    public AuditLogController(IDbContextFactory<Data.AppDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    [HttpPost]
    public async Task<IActionResult> GetFiltered([FromBody] AuditLogFilterRequest filter)
    {
        await using var db = await _contextFactory.CreateDbContextAsync();
        var query = db.AuditLogs.AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.Username))
            query = query.Where(l => l.Username.Contains(filter.Username));
        if (!string.IsNullOrWhiteSpace(filter.Action))
            query = query.Where(l => l.Action == filter.Action);
        if (!string.IsNullOrWhiteSpace(filter.Entity))
            query = query.Where(l => l.Entity == filter.Entity);
        if (filter.From.HasValue)
            query = query.Where(l => l.CreatedAt >= filter.From.Value);
        if (filter.To.HasValue)
            query = query.Where(l => l.CreatedAt <= filter.To.Value);

        var totalCount = await query.CountAsync();

        var items = await query.OrderByDescending(l => l.CreatedAt)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(l => new AuditLogDto
            {
                Id = l.Id, Username = l.Username, Action = l.Action,
                Entity = l.Entity, EntityId = l.EntityId,
                FieldName = l.FieldName, OldValue = l.OldValue,
                NewValue = l.NewValue, Details = l.Details, CreatedAt = l.CreatedAt
            })
            .ToListAsync();

        return Ok(new PagedResult<AuditLogDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = filter.Page,
            PageSize = filter.PageSize
        });
    }

    [HttpGet("actions")]
    public async Task<IActionResult> GetDistinctActions()
    {
        await using var db = await _contextFactory.CreateDbContextAsync();
        var actions = await db.AuditLogs.Select(l => l.Action).Distinct().OrderBy(a => a).ToListAsync();
        return Ok(actions);
    }

    [HttpGet("entities")]
    public async Task<IActionResult> GetDistinctEntities()
    {
        await using var db = await _contextFactory.CreateDbContextAsync();
        var entities = await db.AuditLogs.Select(l => l.Entity).Distinct().OrderBy(e => e).ToListAsync();
        return Ok(entities);
    }
}
