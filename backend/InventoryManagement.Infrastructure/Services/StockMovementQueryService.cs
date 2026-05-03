using InventoryManagement.Application.Abstractions;
using InventoryManagement.Application.Common;
using InventoryManagement.Application.DTOs.StockMovements;
using InventoryManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagement.Infrastructure.Services;

public class StockMovementQueryService : IStockMovementQueryService
{
    private readonly ApplicationDbContext _db;

    public StockMovementQueryService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<PagedResult<StockMovementDto>> GetPagedAsync(
        PaginationQuery query,
        Guid? productId,
        Guid? warehouseId,
        CancellationToken cancellationToken = default)
    {
        var q =
            from m in _db.StockMovements.AsNoTracking()
            join p in _db.Products.AsNoTracking() on m.ProductId equals p.Id
            join w in _db.Warehouses.AsNoTracking() on m.WarehouseId equals w.Id
            select new { m, p, w };

        if (productId.HasValue)
            q = q.Where(x => x.m.ProductId == productId.Value);
        if (warehouseId.HasValue)
            q = q.Where(x => x.m.WarehouseId == warehouseId.Value);
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var s = query.Search.Trim();
            q = q.Where(x =>
                x.m.Reason.Contains(s) ||
                x.p.Name.Contains(s) ||
                x.p.Sku.Contains(s));
        }

        var total = await q.CountAsync(cancellationToken);
        var rows = await q
            .OrderByDescending(x => x.m.Timestamp)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(x => new StockMovementDto
            {
                Id = x.m.Id,
                ProductId = x.m.ProductId,
                ProductName = x.p.Name,
                WarehouseId = x.m.WarehouseId,
                WarehouseName = x.w.Name,
                QuantityChange = x.m.QuantityChange,
                Reason = x.m.Reason,
                Timestamp = x.m.Timestamp,
                MovementType = x.m.MovementType,
                PurchaseOrderId = x.m.PurchaseOrderId,
                SalesOrderId = x.m.SalesOrderId
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<StockMovementDto>
        {
            Items = rows,
            Page = query.Page,
            PageSize = query.PageSize,
            TotalCount = total
        };
    }
}
