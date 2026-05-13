using AlNeda.API.Authorization;
using AlNeda.Core.Entities;
using AlNeda.Core.Models;
using AlNeda.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AlNeda.API.Controllers;

[ApiController]
[Route("api/products")]
[Authorize(Policy = AuthPolicies.Staff)]
[Tags("Products")]
public class ProductsController : ControllerBase
{
    private readonly IDbContextFactory<Data.AppDbContext> _contextFactory;
    private readonly IAuditService _audit;

    public ProductsController(IDbContextFactory<Data.AppDbContext> contextFactory, IAuditService audit)
    {
        _contextFactory = contextFactory;
        _audit = audit;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? search)
    {
        await using var db = await _contextFactory.CreateDbContextAsync();
        var query = db.Products.Include(p => p.CategoryObj).AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            query = query.Where(p => p.Name.Contains(s) || (p.Barcode != null && p.Barcode.Contains(s)));
        }
        var products = await query.OrderBy(p => p.Name).ToListAsync();
        var dto = products.Select(p => new ProductDto
        {
            Id = p.Id, Name = p.Name, Barcode = p.Barcode,
            Category = p.Category, CategoryId = p.CategoryId,
            CategoryName = p.CategoryObj?.Name,
            Company = p.Company, Quantity = p.Quantity, UnitPrice = p.UnitPrice,
            ExpiryDate = p.ExpiryDate, ImagePath = p.ImagePath, ImageUrl = p.ImageUrl,
            Description = p.Description, IsActive = p.IsActive, CreatedAt = p.CreatedAt
        }).ToList();
        return Ok(dto);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        await using var db = await _contextFactory.CreateDbContextAsync();
        var p = await db.Products.Include(p => p.CategoryObj).FirstOrDefaultAsync(x => x.Id == id);
        if (p == null) return NotFound(new { message = "المنتج غير موجود" });
        return Ok(new ProductDto
        {
            Id = p.Id, Name = p.Name, Barcode = p.Barcode,
            Category = p.Category, CategoryId = p.CategoryId,
            CategoryName = p.CategoryObj?.Name,
            Company = p.Company, Quantity = p.Quantity, UnitPrice = p.UnitPrice,
            ExpiryDate = p.ExpiryDate, ImagePath = p.ImagePath, ImageUrl = p.ImageUrl,
            Description = p.Description, IsActive = p.IsActive, CreatedAt = p.CreatedAt
        });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateProductRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest(new { message = "اسم المنتج مطلوب" });

        await using var db = await _contextFactory.CreateDbContextAsync();

        string categoryName = "عام";
        if (request.CategoryId.HasValue && request.CategoryId.Value > 0)
        {
            var cat = await db.Categories.FindAsync(request.CategoryId.Value);
            if (cat != null) categoryName = cat.Name;
            else return BadRequest(new { message = "التصنيف غير موجود" });
        }

        var product = new Product
        {
            Name = request.Name.Trim(), Barcode = request.Barcode,
            CategoryId = request.CategoryId > 0 ? request.CategoryId : null, Category = categoryName,
            Company = request.Company ?? "غير محدد",
            Quantity = request.Quantity, UnitPrice = request.UnitPrice,
            ExpiryDate = request.ExpiryDate, ImagePath = request.ImagePath,
            ImageUrl = request.ImageUrl, Description = request.Description ?? "",
            IsActive = request.IsActive, ProductImagesJson = request.ProductImagesJson ?? "",
            CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now
        };
        db.Products.Add(product);
        await db.SaveChangesAsync();

        var username = User.Identity?.Name ?? "system";
        await _audit.LogAsync(username, "create", "Product", product.Id.ToString(), $"تم إنشاء المنتج '{product.Name}'");

        return Ok(new ProductDto
        {
            Id = product.Id, Name = product.Name, Barcode = product.Barcode,
            Category = product.Category, CategoryId = product.CategoryId,
            CategoryName = categoryName, Company = product.Company,
            Quantity = product.Quantity, UnitPrice = product.UnitPrice,
            ExpiryDate = product.ExpiryDate, Description = product.Description,
            IsActive = product.IsActive, CreatedAt = product.CreatedAt
        });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateProductRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest(new { message = "اسم المنتج مطلوب" });

        await using var db = await _contextFactory.CreateDbContextAsync();
        var product = await db.Products.FindAsync(id);
        if (product == null) return NotFound(new { message = "المنتج غير موجود" });

        var oldPrice = product.UnitPrice;
        string? categoryName = null;
        if (request.CategoryId.HasValue && request.CategoryId.Value > 0 && request.CategoryId != product.CategoryId)
        {
            var cat = await db.Categories.FindAsync(request.CategoryId.Value);
            if (cat != null) categoryName = cat.Name;
        }

        product.Name = request.Name.Trim();
        product.Barcode = request.Barcode;
        product.CategoryId = request.CategoryId > 0 ? request.CategoryId : null;
        if (categoryName != null) product.Category = categoryName;
        else if (request.CategoryId.HasValue && request.CategoryId.Value > 0) { var cat = await db.Categories.FindAsync(request.CategoryId.Value); if (cat != null) product.Category = cat.Name; }
        product.Company = request.Company ?? "غير محدد";
        product.Quantity = request.Quantity;
        product.UnitPrice = request.UnitPrice;
        product.ExpiryDate = request.ExpiryDate;
        product.ImagePath = request.ImagePath;
        product.ImageUrl = request.ImageUrl;
        product.Description = request.Description ?? "";
        product.IsActive = request.IsActive;
        product.ProductImagesJson = request.ProductImagesJson ?? "";
        product.UpdatedAt = DateTime.Now;
        await db.SaveChangesAsync();

        var username = User.Identity?.Name ?? "system";
        if (oldPrice != request.UnitPrice)
            await _audit.LogPriceChangeAsync(username, id, product.Name, oldPrice, request.UnitPrice);
        await _audit.LogAsync(username, "update", "Product", id.ToString(), $"تم تحديث المنتج '{product.Name}'");

        return Ok(new ProductDto
        {
            Id = product.Id, Name = product.Name, Barcode = product.Barcode,
            Category = product.Category, CategoryId = product.CategoryId,
            CategoryName = product.CategoryObj?.Name, Company = product.Company,
            Quantity = product.Quantity, UnitPrice = product.UnitPrice,
            ExpiryDate = product.ExpiryDate, Description = product.Description,
            IsActive = product.IsActive, CreatedAt = product.CreatedAt
        });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        await using var db = await _contextFactory.CreateDbContextAsync();
        var product = await db.Products.FindAsync(id);
        if (product == null) return NotFound(new { message = "المنتج غير موجود" });

        db.Products.Remove(product);
        await db.SaveChangesAsync();

        var username = User.Identity?.Name ?? "system";
        await _audit.LogAsync(username, "delete", "Product", id.ToString(), $"تم حذف المنتج '{product.Name}'");

        return Ok(new { message = "تم حذف المنتج بنجاح" });
    }
}
