using InventoryManagement.Application.Abstractions.Repositories;
using InventoryManagement.Application.Common;
using InventoryManagement.Domain.Entities;
using InventoryManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagement.Infrastructure.Repositories;

public class ProductRepository : IProductRepository
{
    private readonly ApplicationDbContext _db;

    public ProductRepository(ApplicationDbContext db)
    {
        _db = db;
    }

    public Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _db.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public Task<Product?> GetTrackedByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _db.Products.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public async Task<bool> SkuExistsAsync(string sku, Guid? excludeProductId, CancellationToken cancellationToken = default)
    {
        var q = _db.Products.AsNoTracking().Where(p => p.Sku == sku);
        if (excludeProductId.HasValue)
            q = q.Where(p => p.Id != excludeProductId.Value);
        return await q.AnyAsync(cancellationToken);
    }

    public async Task AddAsync(Product product, CancellationToken cancellationToken = default) =>
        await _db.Products.AddAsync(product, cancellationToken);

    public void Remove(Product product) => _db.Products.Remove(product);

    public async Task<(IReadOnlyList<Product> Items, int TotalCount)> GetPagedAsync(PaginationQuery query, CancellationToken cancellationToken = default)
    {
        var q = _db.Products.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var s = query.Search.Trim();
            q = q.Where(p =>
                p.Name.Contains(s) ||
                p.Sku.Contains(s) ||
                p.Category.Contains(s));
        }

        var total = await q.CountAsync(cancellationToken);
        var items = await q
            .OrderBy(p => p.Name)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);
        return (items, total);
    }
}
