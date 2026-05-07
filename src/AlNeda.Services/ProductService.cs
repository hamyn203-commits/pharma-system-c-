using AlNeda.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace AlNeda.Services;

public class ProductService
{
    private readonly IDbContextFactory<Data.AppDbContext> _contextFactory;

    public ProductService(IDbContextFactory<Data.AppDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<List<Product>> GetAllAsync(string? search = null)
    {
        await using var db = await _contextFactory.CreateDbContextAsync();
        var query = db.Products.Include(p => p.CategoryObj).AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            query = query.Where(p => p.Name.Contains(s) || (p.Barcode != null && p.Barcode.Contains(s)));
        }
        return await query.OrderBy(p => p.Name).ToListAsync();
    }

    public async Task<Product?> GetByIdAsync(int id)
    {
        await using var db = await _contextFactory.CreateDbContextAsync();
        return await db.Products.Include(p => p.CategoryObj).FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task AddAsync(Product product)
    {
        await using var db = await _contextFactory.CreateDbContextAsync();
        db.Products.Add(product);
        await db.SaveChangesAsync();
    }

    public async Task UpdateAsync(Product product)
    {
        await using var db = await _contextFactory.CreateDbContextAsync();
        db.Products.Update(product);
        await db.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        await using var db = await _contextFactory.CreateDbContextAsync();
        var product = await db.Products.FindAsync(id);
        if (product != null)
        {
            db.Products.Remove(product);
            await db.SaveChangesAsync();
        }
    }
}

public class CategoryService
{
    private readonly IDbContextFactory<Data.AppDbContext> _contextFactory;

    public CategoryService(IDbContextFactory<Data.AppDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<List<Category>> GetAllAsync()
    {
        await using var db = await _contextFactory.CreateDbContextAsync();
        return await db.Categories.OrderBy(c => c.Name).ToListAsync();
    }

    public async Task AddAsync(Category category)
    {
        await using var db = await _contextFactory.CreateDbContextAsync();
        db.Categories.Add(category);
        await db.SaveChangesAsync();
    }

    public async Task UpdateAsync(Category category)
    {
        await using var db = await _contextFactory.CreateDbContextAsync();
        db.Categories.Update(category);
        await db.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        await using var db = await _contextFactory.CreateDbContextAsync();
        var cat = await db.Categories.FindAsync(id);
        if (cat != null)
        {
            db.Categories.Remove(cat);
            await db.SaveChangesAsync();
        }
    }
}
