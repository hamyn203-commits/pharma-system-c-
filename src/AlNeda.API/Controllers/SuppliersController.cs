using AlNeda.Core.Entities;
using AlNeda.Core.Models;
using AlNeda.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AlNeda.API.Controllers;

[ApiController]
[Route("api/suppliers")]
[Authorize]
public class SuppliersController : ControllerBase
{
    private readonly IDbContextFactory<Data.AppDbContext> _contextFactory;
    private readonly IAuditService _audit;

    public SuppliersController(IDbContextFactory<Data.AppDbContext> contextFactory, IAuditService audit)
    {
        _contextFactory = contextFactory;
        _audit = audit;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? search)
    {
        await using var db = await _contextFactory.CreateDbContextAsync();
        var q = db.Suppliers.AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            q = q.Where(x => x.Name.Contains(s) || x.Company.Contains(s));
        }
        var list = await q.OrderBy(x => x.Name).ToListAsync();
        var dto = list.Select(s => new SupplierDto
        {
            Id = s.Id, Name = s.Name, Phone = s.Phone, Address = s.Address,
            Company = s.Company, Balance = s.Balance, Notes = s.Notes, CreatedAt = s.CreatedAt
        }).ToList();
        return Ok(dto);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        await using var db = await _contextFactory.CreateDbContextAsync();
        var s = await db.Suppliers.FindAsync(id);
        if (s == null) return NotFound(new { message = "المورد غير موجود" });
        return Ok(new SupplierDto
        {
            Id = s.Id, Name = s.Name, Phone = s.Phone, Address = s.Address,
            Company = s.Company, Balance = s.Balance, Notes = s.Notes, CreatedAt = s.CreatedAt
        });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateSupplierRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest(new { message = "اسم المورد مطلوب" });

        await using var db = await _contextFactory.CreateDbContextAsync();
        var supplier = new Supplier
        {
            Name = request.Name.Trim(),
            Phone = request.Phone,
            Address = request.Address,
            Company = request.Company,
            Balance = request.Balance,
            Notes = request.Notes,
            CreatedAt = DateTime.Now
        };
        db.Suppliers.Add(supplier);
        await db.SaveChangesAsync();

        var username = User.Identity?.Name ?? "system";
        await _audit.LogAsync(username, "create", "Supplier", supplier.Id.ToString(), $"تم إنشاء المورد '{supplier.Name}'");

        return Ok(new SupplierDto
        {
            Id = supplier.Id, Name = supplier.Name, Phone = supplier.Phone,
            Address = supplier.Address, Company = supplier.Company,
            Balance = supplier.Balance, Notes = supplier.Notes, CreatedAt = supplier.CreatedAt
        });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateSupplierRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest(new { message = "اسم المورد مطلوب" });

        await using var db = await _contextFactory.CreateDbContextAsync();
        var s = await db.Suppliers.FindAsync(id);
        if (s == null) return NotFound(new { message = "المورد غير موجود" });

        s.Name = request.Name.Trim();
        s.Phone = request.Phone;
        s.Address = request.Address;
        s.Company = request.Company;
        s.Balance = request.Balance;
        s.Notes = request.Notes;
        await db.SaveChangesAsync();

        var username = User.Identity?.Name ?? "system";
        await _audit.LogAsync(username, "update", "Supplier", id.ToString(), $"تم تحديث المورد '{s.Name}'");

        return Ok(new SupplierDto
        {
            Id = s.Id, Name = s.Name, Phone = s.Phone, Address = s.Address,
            Company = s.Company, Balance = s.Balance, Notes = s.Notes, CreatedAt = s.CreatedAt
        });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        await using var db = await _contextFactory.CreateDbContextAsync();
        var s = await db.Suppliers.FindAsync(id);
        if (s == null) return NotFound(new { message = "المورد غير موجود" });

        db.Suppliers.Remove(s);
        await db.SaveChangesAsync();

        var username = User.Identity?.Name ?? "system";
        await _audit.LogAsync(username, "delete", "Supplier", id.ToString(), $"تم حذف المورد '{s.Name}'");

        return Ok(new { message = "تم حذف المورد بنجاح" });
    }
}
