using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AlNeda.Core.Entities;
using AlNeda.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace AlNeda.Data.Repositories;

public class CategoryRepository
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;

    public CategoryRepository(IDbContextFactory<AppDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    // Explicit Projection without Include()
    public async Task<List<CategoryDto>> GetAllCategoriesAsync()
    {
        await using var db = await _contextFactory.CreateDbContextAsync();
        
        return await db.Categories
            .Where(c => !c.IsDeleted)
            .OrderBy(c => c.DisplayOrder)
            .ThenBy(c => c.Name)
            .Select(c => new CategoryDto
            {
                Id = c.Id,
                Name = c.Name,
                Description = c.Description,
                Icon = c.Icon,
                ColorCode = c.ColorCode,
                IsActive = c.IsActive,
                DisplayOrder = c.DisplayOrder,
                ProductCount = c.Products.Count(p => p.IsActive == 1),
                CreatedAt = c.CreatedAt
            })
            .ToListAsync();
    }

    public async Task<CategoryDto?> GetCategoryByIdAsync(int id)
    {
        await using var db = await _contextFactory.CreateDbContextAsync();
        
        return await db.Categories
            .Where(c => c.Id == id && !c.IsDeleted)
            .Select(c => new CategoryDto
            {
                Id = c.Id,
                Name = c.Name,
                Description = c.Description,
                Icon = c.Icon,
                ColorCode = c.ColorCode,
                IsActive = c.IsActive,
                DisplayOrder = c.DisplayOrder,
                ProductCount = c.Products.Count(p => p.IsActive == 1),
                CreatedAt = c.CreatedAt
            })
            .FirstOrDefaultAsync();
    }

    public async Task<CategoryDto> AddCategoryAsync(CategoryDto dto)
    {
        await using var db = await _contextFactory.CreateDbContextAsync();

        if (await db.Categories.AnyAsync(c => c.Name == dto.Name && !c.IsDeleted))
            throw new Exception($"التصنيف '{dto.Name}' موجود مسبقاً.");

        var entity = new Category
        {
            Name = dto.Name.Trim(),
            Description = dto.Description,
            Icon = dto.Icon,
            ColorCode = dto.ColorCode,
            IsActive = dto.IsActive,
            DisplayOrder = dto.DisplayOrder,
            CreatedAt = DateTime.Now
        };

        db.Categories.Add(entity);
        await db.SaveChangesAsync();

        dto.Id = entity.Id;
        dto.CreatedAt = entity.CreatedAt;
        return dto;
    }

    public async Task UpdateCategoryAsync(CategoryDto dto)
    {
        await using var db = await _contextFactory.CreateDbContextAsync();

        if (await db.Categories.AnyAsync(c => c.Name == dto.Name && c.Id != dto.Id && !c.IsDeleted))
            throw new Exception($"التصنيف '{dto.Name}' موجود مسبقاً.");

        var entity = await db.Categories.FindAsync(dto.Id);
        if (entity == null || entity.IsDeleted)
            throw new Exception("التصنيف غير موجود.");

        entity.Name = dto.Name.Trim();
        entity.Description = dto.Description;
        entity.Icon = dto.Icon;
        entity.ColorCode = dto.ColorCode;
        entity.IsActive = dto.IsActive;
        entity.DisplayOrder = dto.DisplayOrder;
        entity.UpdatedAt = DateTime.Now;

        await db.SaveChangesAsync();
    }

    public async Task DeleteCategoryAsync(int id)
    {
        await using var db = await _contextFactory.CreateDbContextAsync();
        
        var entity = await db.Categories.FirstOrDefaultAsync(c => c.Id == id);
        if (entity == null || entity.IsDeleted)
            throw new Exception("التصنيف غير موجود.");

        var hasProducts = await db.Products.AnyAsync(p => p.CategoryId == id && p.IsActive == 1);
        if (hasProducts)
            throw new Exception("لا يمكن حذف التصنيف لاحتوائه على منتجات نشطة.");

        db.Categories.Remove(entity); // Or entity.IsDeleted = true if using soft delete
        await db.SaveChangesAsync();
    }
}
