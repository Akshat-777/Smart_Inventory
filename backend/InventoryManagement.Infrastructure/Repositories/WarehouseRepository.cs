using InventoryManagement.Application.Abstractions.Repositories;
using InventoryManagement.Application.Common;
using InventoryManagement.Domain.Entities;
using InventoryManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagement.Infrastructure.Repositories;

public class WarehouseRepository : IWarehouseRepository
{
    private readonly ApplicationDbContext _db;

    public WarehouseRepository(ApplicationDbContext db)
    {
        _db = db;
    }

    public Task<Warehouse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _db.Warehouses.AsNoTracking().FirstOrDefaultAsync(w => w.Id == id, cancellationToken);

    public Task<Warehouse?> GetTrackedByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _db.Warehouses.FirstOrDefaultAsync(w => w.Id == id, cancellationToken);

    public async Task AddAsync(Warehouse warehouse, CancellationToken cancellationToken = default) =>
        await _db.Warehouses.AddAsync(warehouse, cancellationToken);

    public void Remove(Warehouse warehouse) => _db.Warehouses.Remove(warehouse);

    public async Task<(IReadOnlyList<Warehouse> Items, int TotalCount)> GetPagedAsync(PaginationQuery query, CancellationToken cancellationToken = default)
    {
        var q = _db.Warehouses.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var s = query.Search.Trim();
            q = q.Where(w => w.Name.Contains(s) || w.Location.Contains(s));
        }

        var total = await q.CountAsync(cancellationToken);
        var items = await q
            .OrderBy(w => w.Name)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);
        return (items, total);
    }
}
