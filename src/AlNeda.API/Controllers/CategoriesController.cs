using AlNeda.API.Authorization;
using AlNeda.Core.Entities;
using AlNeda.Core.Models;
using AlNeda.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AlNeda.API.Controllers;

[ApiController]
[Route("api/categories")]
[Authorize(Policy = AuthPolicies.Staff)]
public class CategoriesController : ControllerBase
{
    private readonly IDbContextFactory<Data.AppDbContext> _contextFactory;
    private readonly IAuditService _audit;

    public CategoriesController(IDbContextFactory<Data.AppDbContext> contextFactory, IAuditService audit)
    {
        _contextFactory = contextFactory;
        _audit = audit;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        await using var db = await _contextFactory.CreateDbContextAsync();
        var cats = await db.Categories.Include(c => c.Products).OrderBy(c => c.Name).ToListAsync();
        var dto = cats.Select(c => new CategoryDto
        {
            Id = c.Id, Name = c.Name, Description = c.Description, Icon = c.Icon,
            ColorCode = c.ColorCode, IsActive = c.IsActive, DisplayOrder = c.DisplayOrder,
            ProductCount = c.Products?.Count(p => p.IsActive == 1) ?? 0, CreatedAt = c.CreatedAt
        }).ToList();
        return Ok(dto);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        await using var db = await _contextFactory.CreateDbContextAsync();
        var c = await db.Categories.Include(c => c.Products).FirstOrDefaultAsync(x => x.Id == id);
        if (c == null) return NotFound(new { message = "التصنيف غير موجود" });
        return Ok(new CategoryDto
        {
            Id = c.Id, Name = c.Name, Description = c.Description, Icon = c.Icon,
            ColorCode = c.ColorCode, IsActive = c.IsActive, DisplayOrder = c.DisplayOrder,
            ProductCount = c.Products?.Count(p => p.IsActive == 1) ?? 0, CreatedAt = c.CreatedAt
        });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateCategoryRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest(new { message = "اسم التصنيف مطلوب" });

        await using var db = await _contextFactory.CreateDbContextAsync();

        var exists = await db.Categories.AnyAsync(c => c.Name == request.Name && !c.IsDeleted);
        if (exists)
            return Conflict(new { message = $"التصنيف '{request.Name}' موجود مسبقاً" });

        var cat = new Category
        {
            Name = request.Name.Trim(),
            Description = request.Description,
            Icon = request.Icon,
            ColorCode = request.ColorCode,
            IsActive = request.IsActive,
            DisplayOrder = request.DisplayOrder,
            CreatedAt = DateTime.Now
        };
        db.Categories.Add(cat);
        await db.SaveChangesAsync();

        var username = User.Identity?.Name ?? "system";
        await _audit.LogAsync(username, "create", "Category", cat.Id.ToString(), $"تم إنشاء التصنيف '{cat.Name}'");

        return Ok(new CategoryDto
        {
            Id = cat.Id, Name = cat.Name, Description = cat.Description, Icon = cat.Icon,
            ColorCode = cat.ColorCode, IsActive = cat.IsActive, DisplayOrder = cat.DisplayOrder, CreatedAt = cat.CreatedAt
        });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateCategoryRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest(new { message = "اسم التصنيف مطلوب" });

        await using var db = await _contextFactory.CreateDbContextAsync();
        var cat = await db.Categories.FindAsync(id);
        if (cat == null) return NotFound(new { message = "التصنيف غير موجود" });

        var dup = await db.Categories.AnyAsync(c => c.Name == request.Name && c.Id != id && !c.IsDeleted);
        if (dup)
            return Conflict(new { message = $"التصنيف '{request.Name}' موجود مسبقاً" });

        cat.Name = request.Name.Trim();
        cat.Description = request.Description;
        cat.Icon = request.Icon;
        cat.ColorCode = request.ColorCode;
        cat.IsActive = request.IsActive;
        cat.DisplayOrder = request.DisplayOrder;
        cat.UpdatedAt = DateTime.Now;
        await db.SaveChangesAsync();

        var username = User.Identity?.Name ?? "system";
        await _audit.LogAsync(username, "update", "Category", id.ToString(), $"تم تحديث التصنيف '{cat.Name}'");

        return Ok(new CategoryDto
        {
            Id = cat.Id, Name = cat.Name, Description = cat.Description, Icon = cat.Icon,
            ColorCode = cat.ColorCode, IsActive = cat.IsActive, DisplayOrder = cat.DisplayOrder, CreatedAt = cat.CreatedAt
        });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        await using var db = await _contextFactory.CreateDbContextAsync();
        var cat = await db.Categories.Include(c => c.Products).FirstOrDefaultAsync(c => c.Id == id);
        if (cat == null) return NotFound(new { message = "التصنيف غير موجود" });

        var prodCount = cat.Products?.Count(p => p.IsActive == 1) ?? 0;
        if (prodCount > 0)
            return Conflict(new { message = $"لا يمكن حذف التصنيف لأنه يحتوي على {prodCount} منتج نشط" });

        db.Categories.Remove(cat);
        await db.SaveChangesAsync();

        var username = User.Identity?.Name ?? "system";
        await _audit.LogAsync(username, "delete", "Category", id.ToString(), $"تم حذف التصنيف '{cat.Name}'");

        return Ok(new { message = "تم حذف التصنيف بنجاح" });
    }
}
