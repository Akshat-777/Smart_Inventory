using InventoryManagement.Application.Abstractions;
using InventoryManagement.Application.Common;
using InventoryManagement.Application.DTOs.Inventory;
using InventoryManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagement.Infrastructure.Services;

public class InventoryQueryService : IInventoryQueryService
{
    private readonly ApplicationDbContext _db;

    public InventoryQueryService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<PagedResult<InventoryItemDto>> GetPagedAsync(InventoryQueryDto query, CancellationToken cancellationToken = default)
    {
        var q =
            from i in _db.InventoryItems.AsNoTracking()
            join p in _db.Products.AsNoTracking() on i.ProductId equals p.Id
            join w in _db.Warehouses.AsNoTracking() on i.WarehouseId equals w.Id
            select new { i, p, w };

        if (query.ProductId.HasValue)
            q = q.Where(x => x.i.ProductId == query.ProductId.Value);
        if (query.WarehouseId.HasValue)
            q = q.Where(x => x.i.WarehouseId == query.WarehouseId.Value);
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var s = query.Search.Trim();
            q = q.Where(x =>
                x.p.Name.Contains(s) ||
                x.p.Sku.Contains(s) ||
                x.w.Name.Contains(s));
        }

        var total = await q.CountAsync(cancellationToken);
        var rows = await q
            .OrderBy(x => x.p.Name)
            .ThenBy(x => x.w.Name)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(x => new InventoryItemDto
            {
                ProductId = x.i.ProductId,
                ProductName = x.p.Name,
                Sku = x.p.Sku,
                WarehouseId = x.i.WarehouseId,
                WarehouseName = x.w.Name,
                Quantity = x.i.Quantity
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<InventoryItemDto>
        {
            Items = rows,
            Page = query.Page,
            PageSize = query.PageSize,
            TotalCount = total
        };
    }
}
