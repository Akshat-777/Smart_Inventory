using System.Security.Cryptography;
using InventoryManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.Extensions.Configuration;

namespace InventoryManagement.Infrastructure.Persistence;

public class ApplicationDbContext : DbContext
{
    private readonly IConfiguration? _configuration;

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, IConfiguration? configuration = null)
        : base(options)
    {
        _configuration = configuration;
    }

    private bool UseSqliteConfiguration =>
        _configuration?.GetValue<bool>("Database:UseSqlite") ?? false;

    public DbSet<UserAccount> Users => Set<UserAccount>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Warehouse> Warehouses => Set<Warehouse>();
    public DbSet<InventoryItem> InventoryItems => Set<InventoryItem>();
    public DbSet<StockMovement> StockMovements => Set<StockMovement>();
    public DbSet<PurchaseOrder> PurchaseOrders => Set<PurchaseOrder>();
    public DbSet<PurchaseOrderLine> PurchaseOrderLines => Set<PurchaseOrderLine>();
    public DbSet<SalesOrder> SalesOrders => Set<SalesOrder>();
    public DbSet<SalesOrderLine> SalesOrderLines => Set<SalesOrderLine>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserAccount>(e =>
        {
            e.HasKey(u => u.Id);
            e.Property(u => u.Email).HasMaxLength(256).IsRequired();
            e.HasIndex(u => u.Email).IsUnique();
            e.Property(u => u.PasswordHash).HasMaxLength(512).IsRequired();
            e.Property(u => u.Role).HasMaxLength(64).IsRequired();
        });

        modelBuilder.Entity<Product>(e =>
        {
            e.HasKey(p => p.Id);
            e.Property(p => p.Name).HasMaxLength(256).IsRequired();
            e.Property(p => p.Sku).HasMaxLength(64).IsRequired();
            e.Property(p => p.Category).HasMaxLength(128).IsRequired();
            e.Property(p => p.Price).HasPrecision(18, 2);
            ConfigureConcurrencyToken(e.Property(p => p.RowVersion));
            e.HasIndex(p => p.Sku).IsUnique();
        });

        modelBuilder.Entity<Warehouse>(e =>
        {
            e.HasKey(w => w.Id);
            e.Property(w => w.Name).HasMaxLength(256).IsRequired();
            e.Property(w => w.Location).HasMaxLength(512).IsRequired();
            ConfigureConcurrencyToken(e.Property(w => w.RowVersion));
        });

        modelBuilder.Entity<InventoryItem>(e =>
        {
            e.HasKey(i => new { i.ProductId, i.WarehouseId });
            ConfigureConcurrencyToken(e.Property(i => i.RowVersion));
            e.HasOne(i => i.Product).WithMany(p => p.InventoryItems).HasForeignKey(i => i.ProductId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(i => i.Warehouse).WithMany(w => w.InventoryItems).HasForeignKey(i => i.WarehouseId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<StockMovement>(e =>
        {
            e.HasKey(s => s.Id);
            e.Property(s => s.Reason).HasMaxLength(512).IsRequired();
            e.Property(s => s.Reference).HasMaxLength(256);
            e.Property(s => s.MovementType).HasConversion<int>();
            e.HasIndex(s => s.Timestamp);
            e.HasOne(s => s.Product).WithMany(p => p.StockMovements).HasForeignKey(s => s.ProductId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(s => s.Warehouse).WithMany(w => w.StockMovements).HasForeignKey(s => s.WarehouseId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<PurchaseOrder>(e =>
        {
            e.HasKey(o => o.Id);
            e.Property(o => o.OrderNumber).HasMaxLength(64).IsRequired();
            e.HasIndex(o => o.OrderNumber).IsUnique();
            e.Property(o => o.Status).HasConversion<int>();
        });

        modelBuilder.Entity<PurchaseOrderLine>(e =>
        {
            e.HasKey(l => l.Id);
            e.HasOne(l => l.PurchaseOrder).WithMany(o => o.Lines).HasForeignKey(l => l.PurchaseOrderId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(l => l.Product).WithMany().HasForeignKey(l => l.ProductId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(l => l.Warehouse).WithMany().HasForeignKey(l => l.WarehouseId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<SalesOrder>(e =>
        {
            e.HasKey(o => o.Id);
            e.Property(o => o.OrderNumber).HasMaxLength(64).IsRequired();
            e.HasIndex(o => o.OrderNumber).IsUnique();
            e.Property(o => o.Status).HasConversion<int>();
        });

        modelBuilder.Entity<SalesOrderLine>(e =>
        {
            e.HasKey(l => l.Id);
            e.HasOne(l => l.SalesOrder).WithMany(o => o.Lines).HasForeignKey(l => l.SalesOrderId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(l => l.Product).WithMany().HasForeignKey(l => l.ProductId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(l => l.Warehouse).WithMany().HasForeignKey(l => l.WarehouseId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<AuditLog>(e =>
        {
            e.HasKey(a => a.Id);
            e.Property(a => a.EntityName).HasMaxLength(128).IsRequired();
            e.Property(a => a.EntityId).HasMaxLength(64).IsRequired();
            e.Property(a => a.Action).HasMaxLength(64).IsRequired();
            e.HasIndex(a => a.Timestamp);
        });
    }

    /// <summary>
    /// SQLite cannot auto-fill SQL Server-style rowversion columns. Use plain concurrency tokens there;
    /// SQL Server keeps database-generated rowversion for optimistic concurrency.
    /// </summary>
    private void ConfigureConcurrencyToken(PropertyBuilder<byte[]> property)
    {
        if (UseSqliteConfiguration)
            property.IsConcurrencyToken();
        else
            property.IsRowVersion();
    }

    /// <summary>SQLite: ensure inserts send a RowVersion value (belt-and-suspenders).</summary>
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        if (Database.ProviderName?.Contains("Sqlite", StringComparison.OrdinalIgnoreCase) == true)
        {
            foreach (var entry in ChangeTracker.Entries())
            {
                if (entry.State != EntityState.Added)
                    continue;
                switch (entry.Entity)
                {
                    case Product p when p.RowVersion.Length == 0:
                        p.RowVersion = NewConcurrencyBytes();
                        break;
                    case Warehouse w when w.RowVersion.Length == 0:
                        w.RowVersion = NewConcurrencyBytes();
                        break;
                    case InventoryItem i when i.RowVersion.Length == 0:
                        i.RowVersion = NewConcurrencyBytes();
                        break;
                }
            }
        }

        return await base.SaveChangesAsync(cancellationToken);
    }

    private static byte[] NewConcurrencyBytes()
    {
        var b = new byte[8];
        RandomNumberGenerator.Fill(b);
        return b;
    }
}
