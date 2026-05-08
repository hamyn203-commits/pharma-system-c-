using AlNeda.Core.Entities;
using AlNeda.DomainLogic;
using Microsoft.EntityFrameworkCore;

namespace AlNeda.Services;

public class PharmacyService
{
    private readonly IDbContextFactory<Data.AppDbContext> _contextFactory;

    public PharmacyService(IDbContextFactory<Data.AppDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<List<Pharmacy>> GetAllAsync(string? search = null)
    {
        await using var db = await _contextFactory.CreateDbContextAsync();
        var query = db.Pharmacies.AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            query = query.Where(p => p.Name.Contains(s) || p.Phone.Contains(s));
        }
        return await query.OrderBy(p => p.Name).ToListAsync();
    }

    public async Task<Pharmacy?> GetByIdAsync(int id)
    {
        await using var db = await _contextFactory.CreateDbContextAsync();
        return await db.Pharmacies.FindAsync(id);
    }

    public async Task AddAsync(Pharmacy pharmacy)
    {
        await using var db = await _contextFactory.CreateDbContextAsync();
        db.Pharmacies.Add(pharmacy);
        await db.SaveChangesAsync();
    }

    public async Task UpdateAsync(Pharmacy pharmacy)
    {
        await using var db = await _contextFactory.CreateDbContextAsync();
        db.Pharmacies.Update(pharmacy);
        await db.SaveChangesAsync();
    }

    public async Task SetStatusAsync(int id, string status)
    {
        await using var db = await _contextFactory.CreateDbContextAsync();
        var ph = await db.Pharmacies.FindAsync(id);
        if (ph != null)
        {
            ph.AccountStatus = status;
            if (status == "active") ph.ApprovedAt = DateTime.Now;
            if (status == "blocked") ph.BlockedAt = DateTime.Now;
            await db.SaveChangesAsync();
        }
    }
}

public class SupplierService
{
    private readonly IDbContextFactory<Data.AppDbContext> _contextFactory;

    public SupplierService(IDbContextFactory<Data.AppDbContext> contextFactory) => _contextFactory = contextFactory;

    public async Task<List<Supplier>> GetAllAsync(string? search = null)
    {
        await using var db = await _contextFactory.CreateDbContextAsync();
        var q = db.Suppliers.AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            q = q.Where(x => x.Name.Contains(s) || x.Company.Contains(s));
        }
        return await q.OrderBy(x => x.Name).ToListAsync();
    }

    public async Task AddAsync(Supplier s) { await using var db = await _contextFactory.CreateDbContextAsync(); db.Suppliers.Add(s); await db.SaveChangesAsync(); }
    public async Task UpdateAsync(Supplier s) { await using var db = await _contextFactory.CreateDbContextAsync(); db.Suppliers.Update(s); await db.SaveChangesAsync(); }
    public async Task DeleteAsync(int id) { await using var db = await _contextFactory.CreateDbContextAsync(); var x = await db.Suppliers.FindAsync(id); if (x != null) { db.Suppliers.Remove(x); await db.SaveChangesAsync(); } }
}

public class PurchaseService
{
    private readonly IDbContextFactory<Data.AppDbContext> _contextFactory;

    public PurchaseService(IDbContextFactory<Data.AppDbContext> contextFactory) => _contextFactory = contextFactory;

    public async Task<List<Purchase>> GetAllAsync()
    {
        await using var db = await _contextFactory.CreateDbContextAsync();
        return await db.Purchases.Include(p => p.Supplier).Include(p => p.Items).ThenInclude(i => i.Product).OrderByDescending(p => p.CreatedAt).ToListAsync();
    }

    public async Task AddAsync(Purchase purchase)
    {
        await using var db = await _contextFactory.CreateDbContextAsync();
        db.Purchases.Add(purchase);
        await db.SaveChangesAsync();
    }
}

public class OrderService
{
    private readonly IDbContextFactory<Data.AppDbContext> _contextFactory;

    public OrderService(IDbContextFactory<Data.AppDbContext> contextFactory) => _contextFactory = contextFactory;

    public async Task<List<Order>> GetAllAsync(string? statusFilter = null)
    {
        await using var db = await _contextFactory.CreateDbContextAsync();
        var q = db.Orders.Include(o => o.Pharmacy).Include(o => o.Items).ThenInclude(i => i.Product).AsQueryable();
        if (!string.IsNullOrWhiteSpace(statusFilter))
            q = q.Where(o => o.Status == statusFilter);
        return await q.OrderByDescending(o => o.CreatedAt).ToListAsync();
    }

    public async Task<Order?> GetByIdAsync(int id)
    {
        await using var db = await _contextFactory.CreateDbContextAsync();
        return await db.Orders.Include(o => o.Pharmacy).Include(o => o.Items).ThenInclude(i => i.Product).Include(o => o.StatusHistory).FirstOrDefaultAsync(o => o.Id == id);
    }

    public async Task<string> CreateAsync(Order order, List<OrderItem> items)
    {
        await using var db = await _contextFactory.CreateDbContextAsync();
        order.OrderNumber = $"ORD-{DateTime.Now:yyyyMMdd}-{Random.Shared.Next(1000, 9999)}";
        order.Status = "pending";
        order.CreatedAt = DateTime.Now;
        order.BalanceBefore = (await db.Pharmacies.FindAsync(order.PharmacyId))?.Balance ?? 0;
        order.FinalTotal = OrderWorkflow.computeFinalTotal(order.TotalAmount, order.Discount, order.DiscountType);
        order.BalanceAfter = OrderWorkflow.updateBalance(order.BalanceBefore, order.FinalTotal, "order");

        db.Orders.Add(order);
        foreach (var item in items)
        {
            item.OrderId = order.Id;
            db.OrderItems.Add(item);
        }
        await db.SaveChangesAsync();

        var ph = await db.Pharmacies.FindAsync(order.PharmacyId);
        if (ph != null) { ph.Balance = order.BalanceAfter; await db.SaveChangesAsync(); }
        return order.OrderNumber;
    }

    public async Task<bool> TransitionStatusAsync(int orderId, string newStatus)
    {
        await using var db = await _contextFactory.CreateDbContextAsync();
        var order = await db.Orders.Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == orderId);
        if (order == null) return false;

        var result = OrderWorkflow.validateTransition(order.Status, newStatus);
        if (result is not OrderWorkflow.TransitionOutcome.Allowed allowed) return false;

        order.Status = allowed.newStatus;
        order.LastStatusUpdate = DateTime.Now;
        db.OrderStatusHistories.Add(new OrderStatusHistory
        {
            OrderId = orderId, OldStatus = order.Status, NewStatus = allowed.newStatus, Note = allowed.message, CreatedAt = DateTime.Now
        });

        if (allowed.newStatus == "reviewed")
        {
            foreach (var item in order.Items)
            {
                var prod = await db.Products.FindAsync(item.ProductId);
                if (prod != null) prod.Quantity -= item.Quantity;
            }
        }

        await db.SaveChangesAsync();
        return true;
    }
}

public class PaymentService
{
    private readonly IDbContextFactory<Data.AppDbContext> _contextFactory;

    public PaymentService(IDbContextFactory<Data.AppDbContext> contextFactory) => _contextFactory = contextFactory;

    public async Task<List<Payment>> GetAllAsync(int? pharmacyId = null)
    {
        await using var db = await _contextFactory.CreateDbContextAsync();
        var q = db.Payments.Include(p => p.Pharmacy).Include(p => p.Order).AsQueryable();
        if (pharmacyId.HasValue) q = q.Where(p => p.PharmacyId == pharmacyId.Value);
        return await q.OrderByDescending(p => p.Date).ToListAsync();
    }

    public async Task AddAsync(Payment payment)
    {
        await using var db = await _contextFactory.CreateDbContextAsync();
        var ph = await db.Pharmacies.FindAsync(payment.PharmacyId);
        if (ph == null) return;

        payment.PaymentStatus = Domain.PaymentStatus.calculate(payment.Amount, ph.Balance, payment.PaymentType);
        payment.Date = DateTime.Now;

        db.Payments.Add(payment);
        ph.Balance = OrderWorkflow.updateBalance(ph.Balance, payment.Amount, "payment");
        await db.SaveChangesAsync();
    }
}

public class ReturnService
{
    private readonly IDbContextFactory<Data.AppDbContext> _contextFactory;

    public ReturnService(IDbContextFactory<Data.AppDbContext> contextFactory) => _contextFactory = contextFactory;

    public async Task<List<Return>> GetAllAsync()
    {
        await using var db = await _contextFactory.CreateDbContextAsync();
        return await db.Returns.Include(r => r.Pharmacy).Include(r => r.Items).ThenInclude(i => i.Product).OrderByDescending(r => r.CreatedAt).ToListAsync();
    }

    public async Task<string> CreateAsync(Return ret, List<ReturnItem> items)
    {
        await using var db = await _contextFactory.CreateDbContextAsync();
        ret.ReturnNumber = $"RET-{DateTime.Now:yyyyMMdd}-{Random.Shared.Next(1000, 9999)}";
        ret.CreatedAt = DateTime.Now;
        ret.BalanceBefore = (await db.Pharmacies.FindAsync(ret.PharmacyId))?.Balance ?? 0;
        ret.BalanceAfter = OrderWorkflow.updateBalance(ret.BalanceBefore, ret.TotalAmount, "return");

        db.Returns.Add(ret);
        await db.SaveChangesAsync();

        foreach (var item in items)
        {
            item.ReturnId = ret.Id;
            db.ReturnItems.Add(item);
            if (ret.StockAdjusted)
            {
                var prod = await db.Products.FindAsync(item.ProductId);
                if (prod != null) prod.Quantity += item.Quantity;
            }
        }

        if (ret.BalanceAdjusted)
        {
            var ph = await db.Pharmacies.FindAsync(ret.PharmacyId);
            if (ph != null) ph.Balance = ret.BalanceAfter;
        }

        await db.SaveChangesAsync();
        return ret.ReturnNumber;
    }
}


