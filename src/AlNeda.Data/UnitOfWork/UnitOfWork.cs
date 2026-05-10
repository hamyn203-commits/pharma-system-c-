using AlNeda.Data.Repositories;
using Microsoft.EntityFrameworkCore;
using AlNeda.Core.Entities;

namespace AlNeda.Data.UnitOfWork;

public interface IUnitOfWork : IDisposable, IAsyncDisposable
{
    IRepository<T> GetRepository<T>() where T : class, IEntity;
    IReadOnlyRepository<T> GetReadOnlyRepository<T>() where T : class, IEntity;
    Task<int> SaveChangesAsync();
    Task BeginTransactionAsync();
    Task CommitTransactionAsync();
    Task RollbackTransactionAsync();
}

public interface IUnitOfWorkFactory
{
    IUnitOfWork Create();
}

public class UnitOfWork : IUnitOfWork
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;
    private AppDbContext? _context;
    private bool _disposed;

    private static readonly Dictionary<Type, Type> _repositoryTypes = new()
    {
        { typeof(Core.Entities.Product), typeof(ProductRepository) },
        { typeof(Core.Entities.Pharmacy), typeof(PharmacyRepository) },
        { typeof(Core.Entities.Order), typeof(OrderRepository) },
        { typeof(Core.Entities.OrderItem), typeof(OrderItemRepository) },
        { typeof(Core.Entities.Payment), typeof(PaymentRepository) },
        { typeof(Core.Entities.Return), typeof(ReturnRepository) },
        { typeof(Core.Entities.Supplier), typeof(SupplierRepository) },
        { typeof(Core.Entities.Purchase), typeof(PurchaseRepository) },
        { typeof(Core.Entities.AuditLog), typeof(AuditLogRepository) },
    };

    public UnitOfWork(IDbContextFactory<AppDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    private AppDbContext GetContext()
    {
        _context ??= _contextFactory.CreateDbContext();
        return _context;
    }

    public IRepository<T> GetRepository<T>() where T : class, IEntity
    {
        var entityType = typeof(T);

        if (_repositoryTypes.TryGetValue(entityType, out var repoType))
        {
            return (IRepository<T>)Activator.CreateInstance(repoType, GetContext())!;
        }

        return new GenericRepository<T>(GetContext());
    }

    public IReadOnlyRepository<T> GetReadOnlyRepository<T>() where T : class, IEntity
    {
        return GetRepository<T>();
    }

    public async Task<int> SaveChangesAsync()
    {
        return await GetContext().SaveChangesAsync();
    }

    private Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction? _transaction;

    public async Task BeginTransactionAsync()
    {
        _transaction = await GetContext().Database.BeginTransactionAsync();
    }

    public async Task CommitTransactionAsync()
    {
        if (_transaction != null)
        {
            await _transaction.CommitAsync();
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public async Task RollbackTransactionAsync()
    {
        if (_transaction != null)
        {
            await _transaction.RollbackAsync();
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _transaction?.Dispose();
            _context?.Dispose();
            _disposed = true;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (!_disposed)
        {
            if (_transaction != null)
                await _transaction.DisposeAsync();
            if (_context != null)
                await _context.DisposeAsync();
            _disposed = true;
        }
    }
}

public class UnitOfWorkFactory : IUnitOfWorkFactory
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;

    public UnitOfWorkFactory(IDbContextFactory<AppDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public IUnitOfWork Create() => new UnitOfWork(_contextFactory);
}


public class GenericRepository<TEntity> : IRepository<TEntity> where TEntity : class, IEntity
{
    protected readonly AppDbContext _context;
    protected readonly DbSet<TEntity> _dbSet;

    public GenericRepository(AppDbContext context)
    {
        _context = context;
        _dbSet = context.Set<TEntity>();
    }

    public virtual async Task<TEntity?> GetByIdAsync(int id)
        => await _dbSet.FindAsync(id);

    public virtual async Task<IEnumerable<TEntity>> GetAllAsync()
        => await _dbSet.ToListAsync();

    public virtual async Task<IEnumerable<TEntity>> FindAsync(Func<TEntity, bool> predicate)
        => await Task.FromResult(_dbSet.Where(predicate).ToList());

    public virtual async Task AddAsync(TEntity entity)
    {
        await _dbSet.AddAsync(entity);
        await _context.SaveChangesAsync();
    }

    public virtual async Task UpdateAsync(TEntity entity)
    {
        _dbSet.Update(entity);
        await _context.SaveChangesAsync();
    }

    public virtual async Task DeleteAsync(int id)
    {
        var entity = await GetByIdAsync(id);
        if (entity != null)
        {
            _dbSet.Remove(entity);
            await _context.SaveChangesAsync();
        }
    }

    public virtual async Task<int> CountAsync()
        => await _dbSet.CountAsync();

    public virtual async Task<int> CountAsync(Func<TEntity, bool> predicate)
        => await Task.FromResult(_dbSet.Count(predicate));
}


public class ProductRepository : GenericRepository<Core.Entities.Product>
{
    public ProductRepository(AppDbContext context) : base(context) { }

    public override async Task<IEnumerable<Core.Entities.Product>> GetAllAsync()
    {
        return await _dbSet.Where(p => p.IsActive == 1).OrderBy(p => p.Name).ToListAsync();
    }

    public async Task<IEnumerable<Core.Entities.Product>> GetLowStockAsync(int threshold)
    {
        return await _dbSet.Where(p => p.Quantity < threshold && p.IsActive == 1)
            .OrderBy(p => p.Quantity)
            .ToListAsync();
    }

    public async Task<IEnumerable<Core.Entities.Product>> GetExpiringAsync(DateTime threshold)
    {
        return await _dbSet
            .Where(p => !string.IsNullOrEmpty(p.ExpiryDate) && p.IsActive == 1)
            .ToListAsync()
            .ContinueWith(t => t.Result
                .Where(p => DateTime.TryParse(p.ExpiryDate, out var exp) && exp <= threshold && exp >= DateTime.Today)
                .OrderBy(p => DateTime.TryParse(p.ExpiryDate, out var exp2) ? exp2 : DateTime.MaxValue));
    }

    public async Task<IEnumerable<Core.Entities.Product>> SearchAsync(string searchTerm)
    {
        var term = searchTerm.Trim().ToLower();
        return await _dbSet
            .Where(p => p.Name.ToLower().Contains(term) ||
                       (p.Barcode != null && p.Barcode.Contains(term)) ||
                       p.Category.ToLower().Contains(term))
            .OrderBy(p => p.Name)
            .ToListAsync();
    }
}

public class PharmacyRepository : GenericRepository<Core.Entities.Pharmacy> 
{
    public PharmacyRepository(AppDbContext context) : base(context) { }
}

public class OrderRepository : GenericRepository<Core.Entities.Order>
{
    public OrderRepository(AppDbContext context) : base(context) { }

    public override async Task<IEnumerable<Core.Entities.Order>> GetAllAsync()
    {
        return await _dbSet.OrderByDescending(o => o.CreatedAt)
            .Select(o => new Core.Entities.Order
            {
                Id = o.Id, OrderNumber = o.OrderNumber, PharmacyId = o.PharmacyId, Pharmacy = o.Pharmacy,
                TotalAmount = o.TotalAmount, Discount = o.Discount, DiscountType = o.DiscountType,
                FinalTotal = o.FinalTotal, BalanceBefore = o.BalanceBefore, BalanceAfter = o.BalanceAfter,
                Status = o.Status, DeliveryPerson = o.DeliveryPerson, Notes = o.Notes,
                LastStatusUpdate = o.LastStatusUpdate, ExpectedDeliveryNote = o.ExpectedDeliveryNote,
                PaymentStatus = o.PaymentStatus, PaymentType = o.PaymentType, AmountPaid = o.AmountPaid,
                RemainingAmount = o.RemainingAmount, PaymentNotes = o.PaymentNotes, CreatedAt = o.CreatedAt
            }).ToListAsync();
    }
}

public class OrderItemRepository : GenericRepository<Core.Entities.OrderItem> 
{
    public OrderItemRepository(AppDbContext context) : base(context) { }
}

public class PaymentRepository : GenericRepository<Core.Entities.Payment> 
{
    public PaymentRepository(AppDbContext context) : base(context) { }
}

public class ReturnRepository : GenericRepository<Core.Entities.Return> 
{
    public ReturnRepository(AppDbContext context) : base(context) { }
}

public class SupplierRepository : GenericRepository<Core.Entities.Supplier> 
{
    public SupplierRepository(AppDbContext context) : base(context) { }
}

public class PurchaseRepository : GenericRepository<Core.Entities.Purchase> 
{
    public PurchaseRepository(AppDbContext context) : base(context) { }
}

public class AuditLogRepository : GenericRepository<Core.Entities.AuditLog> 
{
    public AuditLogRepository(AppDbContext context) : base(context) { }
}