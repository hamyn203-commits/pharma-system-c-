using AlNeda.Core.Entities;
using AlNeda.Core.Models;
using AlNeda.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AlNeda.API.Controllers;

[ApiController]
[Route("api/pharmacies")]
[Authorize]
public class PharmaciesController : ControllerBase
{
    private readonly IDbContextFactory<Data.AppDbContext> _contextFactory;
    private readonly IAuditService _audit;

    public PharmaciesController(IDbContextFactory<Data.AppDbContext> contextFactory, IAuditService audit)
    {
        _contextFactory = contextFactory;
        _audit = audit;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? search)
    {
        await using var db = await _contextFactory.CreateDbContextAsync();
        var q = db.Pharmacies.AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            q = q.Where(p => p.Name.Contains(s) || p.Phone.Contains(s));
        }
        var list = await q.OrderBy(p => p.Name).ToListAsync();
        var dto = list.Select(p => new PharmacyDto
        {
            Id = p.Id, Name = p.Name, Address = p.Address, Phone = p.Phone,
            Balance = p.Balance, AccountStatus = p.AccountStatus, CreatedAt = p.CreatedAt
        }).ToList();
        return Ok(dto);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        await using var db = await _contextFactory.CreateDbContextAsync();
        var p = await db.Pharmacies.FindAsync(id);
        if (p == null) return NotFound(new { message = "الصيدلية غير موجودة" });
        return Ok(new PharmacyDto
        {
            Id = p.Id, Name = p.Name, Address = p.Address, Phone = p.Phone,
            Balance = p.Balance, AccountStatus = p.AccountStatus, CreatedAt = p.CreatedAt
        });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreatePharmacyRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest(new { message = "اسم الصيدلية مطلوب" });

        await using var db = await _contextFactory.CreateDbContextAsync();
        var pharmacy = new Pharmacy
        {
            Name = request.Name.Trim(),
            Address = request.Address,
            Phone = request.Phone,
            Balance = request.Balance,
            AccountStatus = request.AccountStatus,
            CreatedAt = DateTime.Now
        };
        db.Pharmacies.Add(pharmacy);
        await db.SaveChangesAsync();

        var username = User.Identity?.Name ?? "system";
        await _audit.LogAsync(username, "create", "Pharmacy", pharmacy.Id.ToString(), $"تم إنشاء الصيدلية '{pharmacy.Name}'");

        return Ok(new PharmacyDto
        {
            Id = pharmacy.Id, Name = pharmacy.Name, Address = pharmacy.Address,
            Phone = pharmacy.Phone, Balance = pharmacy.Balance,
            AccountStatus = pharmacy.AccountStatus, CreatedAt = pharmacy.CreatedAt
        });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdatePharmacyRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest(new { message = "اسم الصيدلية مطلوب" });

        await using var db = await _contextFactory.CreateDbContextAsync();
        var p = await db.Pharmacies.FindAsync(id);
        if (p == null) return NotFound(new { message = "الصيدلية غير موجودة" });

        p.Name = request.Name.Trim();
        p.Address = request.Address;
        p.Phone = request.Phone;
        p.Balance = request.Balance;
        p.AccountStatus = request.AccountStatus;
        await db.SaveChangesAsync();

        var username = User.Identity?.Name ?? "system";
        await _audit.LogAsync(username, "update", "Pharmacy", id.ToString(), $"تم تحديث الصيدلية '{p.Name}'");

        return Ok(new PharmacyDto
        {
            Id = p.Id, Name = p.Name, Address = p.Address, Phone = p.Phone,
            Balance = p.Balance, AccountStatus = p.AccountStatus, CreatedAt = p.CreatedAt
        });
    }

    [HttpPut("{id}/status")]
    public async Task<IActionResult> SetStatus(int id, [FromBody] PharmacyStatusRequest request)
    {
        await using var db = await _contextFactory.CreateDbContextAsync();
        var p = await db.Pharmacies.FindAsync(id);
        if (p == null) return NotFound(new { message = "الصيدلية غير موجودة" });

        p.AccountStatus = request.Status;
        if (request.Status == "active") p.ApprovedAt = DateTime.Now;
        if (request.Status == "blocked") p.BlockedAt = DateTime.Now;
        await db.SaveChangesAsync();

        var username = User.Identity?.Name ?? "system";
        await _audit.LogAsync(username, "update", "Pharmacy", id.ToString(), $"تم تغيير حالة الصيدلية '{p.Name}' إلى {request.Status}");

        return Ok(new { message = "تم تحديث الحالة بنجاح" });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        await using var db = await _contextFactory.CreateDbContextAsync();
        var p = await db.Pharmacies.FindAsync(id);
        if (p == null) return NotFound(new { message = "الصيدلية غير موجودة" });

        db.Pharmacies.Remove(p);
        await db.SaveChangesAsync();

        var username = User.Identity?.Name ?? "system";
        await _audit.LogAsync(username, "delete", "Pharmacy", id.ToString(), $"تم حذف الصيدلية '{p.Name}'");

        return Ok(new { message = "تم حذف الصيدلية بنجاح" });
    }
}
