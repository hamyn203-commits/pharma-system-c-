using Microsoft.EntityFrameworkCore;
using AlNeda.Core.Entities;
using System.Security.Cryptography;
using System.Text;

namespace AlNeda.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Pharmacy> Pharmacies => Set<Pharmacy>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<Purchase> Purchases => Set<Purchase>();
    public DbSet<PurchaseItem> PurchaseItems => Set<PurchaseItem>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<OrderStatusHistory> OrderStatusHistories => Set<OrderStatusHistory>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<Return> Returns => Set<Return>();
    public DbSet<ReturnItem> ReturnItems => Set<ReturnItem>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Product>(e =>
        {
            e.Property(p => p.UnitPrice).HasPrecision(18, 2);
            e.HasOne(p => p.CategoryObj).WithMany(c => c.Products).HasForeignKey(p => p.CategoryId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Order>(e =>
        {
            e.Property(o => o.TotalAmount).HasPrecision(18, 2);
            e.Property(o => o.Discount).HasPrecision(18, 2);
            e.Property(o => o.FinalTotal).HasPrecision(18, 2);
            e.Property(o => o.BalanceBefore).HasPrecision(18, 2);
            e.Property(o => o.BalanceAfter).HasPrecision(18, 2);
            e.Property(o => o.AmountPaid).HasPrecision(18, 2);
            e.Property(o => o.RemainingAmount).HasPrecision(18, 2);
        });

        modelBuilder.Entity<OrderItem>(e =>
        {
            e.Property(oi => oi.UnitPrice).HasPrecision(18, 2);
            e.Property(oi => oi.TotalPrice).HasPrecision(18, 2);
        });

        modelBuilder.Entity<Payment>(e =>
        {
            e.Property(p => p.Amount).HasPrecision(18, 2);
            e.Property(p => p.AmountPaid).HasPrecision(18, 2);
            e.Property(p => p.RemainingAmount).HasPrecision(18, 2);
        });

        modelBuilder.Entity<Return>(e =>
        {
            e.Property(r => r.TotalAmount).HasPrecision(18, 2);
            e.Property(r => r.BalanceBefore).HasPrecision(18, 2);
            e.Property(r => r.BalanceAfter).HasPrecision(18, 2);
        });

        modelBuilder.Entity<ReturnItem>(e =>
        {
            e.Property(ri => ri.UnitPrice).HasPrecision(18, 2);
            e.Property(ri => ri.TotalPrice).HasPrecision(18, 2);
        });

        modelBuilder.Entity<Purchase>(e =>
        {
            e.Property(p => p.TotalAmount).HasPrecision(18, 2);
            e.Property(p => p.AmountPaid).HasPrecision(18, 2);
            e.Property(p => p.RemainingAmount).HasPrecision(18, 2);
        });

        modelBuilder.Entity<PurchaseItem>(e =>
        {
            e.Property(pi => pi.UnitCost).HasPrecision(18, 2);
            e.Property(pi => pi.TotalCost).HasPrecision(18, 2);
        });

        modelBuilder.Entity<Pharmacy>(e =>
        {
            e.Property(p => p.Balance).HasPrecision(18, 2);
        });

        modelBuilder.Entity<Supplier>(e =>
        {
            e.Property(s => s.Balance).HasPrecision(18, 2);
        });

        modelBuilder.Entity<User>(e =>
        {
            e.HasIndex(u => u.Username).IsUnique();
        });

        modelBuilder.Entity<Category>(e =>
        {
            e.HasIndex(c => c.Name).IsUnique();
        });

        modelBuilder.Entity<Product>(e =>
        {
            e.HasIndex(p => p.Barcode).IsUnique();
        });

        modelBuilder.Entity<User>().HasData(
            new User 
            { 
                Id = 1, 
                Username = "admin", 
                Password = "admin", // Will be upgraded on first login or I can use a known SHA256
                PasswordSalt = "", 
                Role = "admin", 
                CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) 
            }
        );
    }
}
