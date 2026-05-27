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
    public DbSet<MarketingOffer> MarketingOffers => Set<MarketingOffer>();
    public DbSet<MarketingOfferProduct> MarketingOfferProducts => Set<MarketingOfferProduct>();
    public DbSet<OfferEvent> OfferEvents => Set<OfferEvent>();
    public DbSet<MarketingOfferPharmacyTarget> MarketingOfferPharmacyTargets => Set<MarketingOfferPharmacyTarget>();

    // ─── الإضافات الجديدة: الكوبونات، BOGO، الإشعارات ─────────────
    public DbSet<Coupon> Coupons => Set<Coupon>();
    public DbSet<BogoOfferRule> BogoOfferRules => Set<BogoOfferRule>();
    public DbSet<NotificationDevice> NotificationDevices => Set<NotificationDevice>();
    public DbSet<NotificationLog> NotificationLogs => Set<NotificationLog>();


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
            e.HasOne(o => o.SourceOffer).WithMany(o => o.Orders).HasForeignKey(o => o.SourceOfferId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<MarketingOffer>(e =>
        {
            e.Property(o => o.OldPrice).HasPrecision(18, 2);
            e.Property(o => o.NewPrice).HasPrecision(18, 2);
            e.HasIndex(o => o.Status);
            e.HasIndex(o => new { o.StartsAt, o.EndsAt });
        });

        modelBuilder.Entity<OfferEvent>(e =>
        {
            e.HasIndex(x => new { x.MarketingOfferId, x.PharmacyId, x.DeviceKey, x.EventDateKey, x.EventType, x.IsUniqueDailyImpression })
                .IsUnique()
                .HasFilter("EventType = 'impression' AND IsRejected = 0 AND IsUniqueDailyImpression = 1");
            e.HasIndex(x => new { x.MarketingOfferId, x.EventType, x.OccurredAt });
        });

        modelBuilder.Entity<MarketingOfferPharmacyTarget>(e =>
        {
            e.HasIndex(x => new { x.MarketingOfferId, x.PharmacyId }).IsUnique();
            e.HasOne(x => x.Offer).WithMany(o => o.PharmacyTargets).HasForeignKey(x => x.MarketingOfferId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Pharmacy).WithMany().HasForeignKey(x => x.PharmacyId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<MarketingOfferProduct>(e =>
        {
            e.HasIndex(x => new { x.MarketingOfferId, x.ProductId }).IsUnique();
            e.Property(x => x.OldPrice).HasPrecision(18, 2);
            e.Property(x => x.NewPrice).HasPrecision(18, 2);
            e.HasOne(x => x.Offer).WithMany(o => o.OfferProducts).HasForeignKey(x => x.MarketingOfferId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Cascade);
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
            e.HasOne(u => u.Pharmacy).WithMany().HasForeignKey(u => u.PharmacyId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Category>(e =>
        {
            e.HasIndex(c => c.Name).IsUnique();
        });

        modelBuilder.Entity<Product>(e =>
        {
            e.HasIndex(p => p.Barcode).IsUnique();
        });

        // ─── تكوينات الكوبونات ────────────────────────────────────
        modelBuilder.Entity<Coupon>(e =>
        {
            e.HasIndex(c => c.Code).IsUnique();
            e.Property(c => c.DiscountValue).HasPrecision(18, 2);
            e.Property(c => c.MinOrderAmount).HasPrecision(18, 2);
            e.Property(c => c.MaxDiscountAmount).HasPrecision(18, 2);
            e.HasOne(c => c.TargetPharmacy).WithMany().HasForeignKey(c => c.TargetPharmacyId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(c => c.TargetOffer).WithMany().HasForeignKey(c => c.TargetOfferId).OnDelete(DeleteBehavior.SetNull);
        });

        // ─── تكوينات BOGO ─────────────────────────────────────────
        modelBuilder.Entity<BogoOfferRule>(e =>
        {
            e.HasOne(b => b.Offer).WithMany().HasForeignKey(b => b.MarketingOfferId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(b => b.TargetProduct).WithMany().HasForeignKey(b => b.TargetProductId).OnDelete(DeleteBehavior.SetNull);
            e.HasIndex(b => b.MarketingOfferId).IsUnique();
        });

        // ─── تكوينات أجهزة الإشعارات ──────────────────────────────
        modelBuilder.Entity<NotificationDevice>(e =>
        {
            e.HasIndex(d => d.DeviceToken).IsUnique();
            e.HasOne(d => d.User).WithMany().HasForeignKey(d => d.UserId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(d => d.Pharmacy).WithMany().HasForeignKey(d => d.PharmacyId).OnDelete(DeleteBehavior.SetNull);
        });

        // ─── تكوينات سجلات الإشعارات ──────────────────────────────
        modelBuilder.Entity<NotificationLog>(e =>
        {
            e.HasOne(n => n.RelatedOffer).WithMany().HasForeignKey(n => n.RelatedOfferId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(n => n.RelatedCoupon).WithMany().HasForeignKey(n => n.RelatedCouponId).OnDelete(DeleteBehavior.SetNull);
            e.HasIndex(n => n.SentAt);
            e.HasIndex(n => n.NotificationType);
        });

    }
}
