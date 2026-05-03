using InventoryManagement.Application.Abstractions;
using InventoryManagement.Application.Common;
using InventoryManagement.Application.DTOs.Orders;
using InventoryManagement.Application.Exceptions;
using InventoryManagement.Domain.Entities;
using InventoryManagement.Domain.Enums;
using InventoryManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagement.Infrastructure.Services;

public class OrderService : IOrderService
{
    private readonly ApplicationDbContext _db;
    private readonly IInventoryLedgerService _ledger;
    private readonly ICurrentUserService _currentUser;
    private readonly AuditWriter _audit;

    public OrderService(
        ApplicationDbContext db,
        IInventoryLedgerService ledger,
        ICurrentUserService currentUser,
        AuditWriter audit)
    {
        _db = db;
        _ledger = ledger;
        _currentUser = currentUser;
        _audit = audit;
    }

    public async Task<PurchaseOrderDto> CreatePurchaseOrderAsync(CreatePurchaseOrderDto dto, CancellationToken cancellationToken = default)
    {
        var userId = _currentUser.UserId ?? throw new BusinessRuleException("User context required.");
        if (dto.Lines == null || dto.Lines.Count == 0)
            throw new BusinessRuleException("At least one line is required.");

        var order = new PurchaseOrder
        {
            Id = Guid.NewGuid(),
            OrderNumber = await NextPoNumberAsync(cancellationToken),
            Status = OrderStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            CreatedByUserId = userId
        };

        foreach (var line in dto.Lines)
        {
            if (line.Quantity <= 0)
                throw new BusinessRuleException("Line quantities must be positive.");
            order.Lines.Add(new PurchaseOrderLine
            {
                Id = Guid.NewGuid(),
                PurchaseOrderId = order.Id,
                ProductId = line.ProductId,
                WarehouseId = line.WarehouseId,
                Quantity = line.Quantity
            });
        }

        _db.PurchaseOrders.Add(order);
        _audit.Enqueue(nameof(PurchaseOrder), order.Id.ToString(), "Created", order.OrderNumber, _currentUser.UserId, _currentUser.Email);
        await _db.SaveChangesAsync(cancellationToken);

        return (await GetPurchaseOrderByIdAsync(order.Id, cancellationToken))!;
    }

    public async Task<PagedResult<PurchaseOrderDto>> GetPurchaseOrdersPagedAsync(PaginationQuery query, CancellationToken cancellationToken = default)
    {
        var q = _db.PurchaseOrders.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var s = query.Search.Trim();
            q = q.Where(o => o.OrderNumber.Contains(s));
        }

        var total = await q.CountAsync(cancellationToken);
        var ids = await q
            .OrderByDescending(o => o.CreatedAt)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(o => o.Id)
            .ToListAsync(cancellationToken);

        var items = new List<PurchaseOrderDto>();
        foreach (var id in ids)
        {
            var po = await GetPurchaseOrderByIdAsync(id, cancellationToken);
            if (po != null)
                items.Add(po);
        }

        return new PagedResult<PurchaseOrderDto>
        {
            Items = items,
            Page = query.Page,
            PageSize = query.PageSize,
            TotalCount = total
        };
    }

    public async Task<PurchaseOrderDto?> GetPurchaseOrderByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var o = await _db.PurchaseOrders.AsNoTracking()
            .Include(x => x.Lines)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (o == null)
            return null;

        var productIds = o.Lines.Select(l => l.ProductId).Distinct().ToList();
        var warehouseIds = o.Lines.Select(l => l.WarehouseId).Distinct().ToList();
        var products = await _db.Products.AsNoTracking().Where(p => productIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id, cancellationToken);
        var warehouses = await _db.Warehouses.AsNoTracking().Where(w => warehouseIds.Contains(w.Id)).ToDictionaryAsync(w => w.Id, cancellationToken);

        return new PurchaseOrderDto
        {
            Id = o.Id,
            OrderNumber = o.OrderNumber,
            Status = o.Status,
            CreatedAt = o.CreatedAt,
            CompletedAt = o.CompletedAt,
            Lines = o.Lines.Select(l => new PurchaseOrderLineDto
            {
                Id = l.Id,
                ProductId = l.ProductId,
                ProductName = products.TryGetValue(l.ProductId, out var p) ? p.Name : "",
                WarehouseId = l.WarehouseId,
                WarehouseName = warehouses.TryGetValue(l.WarehouseId, out var w) ? w.Name : "",
                Quantity = l.Quantity
            }).ToList()
        };
    }

    public async Task CompletePurchaseOrderAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var order = await _db.PurchaseOrders
            .Include(o => o.Lines)
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken)
            ?? throw new BusinessRuleException("Purchase order not found.");

        if (order.Status != OrderStatus.Pending)
            throw new BusinessRuleException("Only pending purchase orders can be completed.");

        await using var tx = await _db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            foreach (var line in order.Lines)
            {
                await _ledger.ApplyChangeAsync(
                    line.ProductId,
                    line.WarehouseId,
                    line.Quantity,
                    $"Purchase receipt {order.OrderNumber}",
                    StockMovementType.PurchaseReceipt,
                    order.Id,
                    null,
                    cancellationToken);
            }

            order.Status = OrderStatus.Completed;
            order.CompletedAt = DateTime.UtcNow;
            _audit.Enqueue(nameof(PurchaseOrder), order.Id.ToString(), "Completed", order.OrderNumber, _currentUser.UserId, _currentUser.Email);
            await _db.SaveChangesAsync(cancellationToken);
            await tx.CommitAsync(cancellationToken);
        }
        catch
        {
            await tx.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<SalesOrderDto> CreateSalesOrderAsync(CreateSalesOrderDto dto, CancellationToken cancellationToken = default)
    {
        var userId = _currentUser.UserId ?? throw new BusinessRuleException("User context required.");
        if (dto.Lines == null || dto.Lines.Count == 0)
            throw new BusinessRuleException("At least one line is required.");

        var order = new SalesOrder
        {
            Id = Guid.NewGuid(),
            OrderNumber = await NextSoNumberAsync(cancellationToken),
            Status = OrderStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            CreatedByUserId = userId
        };

        foreach (var line in dto.Lines)
        {
            if (line.Quantity <= 0)
                throw new BusinessRuleException("Line quantities must be positive.");
            order.Lines.Add(new SalesOrderLine
            {
                Id = Guid.NewGuid(),
                SalesOrderId = order.Id,
                ProductId = line.ProductId,
                WarehouseId = line.WarehouseId,
                Quantity = line.Quantity
            });
        }

        _db.SalesOrders.Add(order);
        _audit.Enqueue(nameof(SalesOrder), order.Id.ToString(), "Created", order.OrderNumber, _currentUser.UserId, _currentUser.Email);
        await _db.SaveChangesAsync(cancellationToken);

        return (await GetSalesOrderByIdAsync(order.Id, cancellationToken))!;
    }

    public async Task<PagedResult<SalesOrderDto>> GetSalesOrdersPagedAsync(PaginationQuery query, CancellationToken cancellationToken = default)
    {
        var q = _db.SalesOrders.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var s = query.Search.Trim();
            q = q.Where(o => o.OrderNumber.Contains(s));
        }

        var total = await q.CountAsync(cancellationToken);
        var ids = await q
            .OrderByDescending(o => o.CreatedAt)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(o => o.Id)
            .ToListAsync(cancellationToken);

        var items = new List<SalesOrderDto>();
        foreach (var oid in ids)
        {
            var so = await GetSalesOrderByIdAsync(oid, cancellationToken);
            if (so != null)
                items.Add(so);
        }

        return new PagedResult<SalesOrderDto>
        {
            Items = items,
            Page = query.Page,
            PageSize = query.PageSize,
            TotalCount = total
        };
    }

    public async Task<SalesOrderDto?> GetSalesOrderByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var o = await _db.SalesOrders.AsNoTracking()
            .Include(x => x.Lines)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (o == null)
            return null;

        var productIds = o.Lines.Select(l => l.ProductId).Distinct().ToList();
        var warehouseIds = o.Lines.Select(l => l.WarehouseId).Distinct().ToList();
        var products = await _db.Products.AsNoTracking().Where(p => productIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id, cancellationToken);
        var warehouses = await _db.Warehouses.AsNoTracking().Where(w => warehouseIds.Contains(w.Id)).ToDictionaryAsync(w => w.Id, cancellationToken);

        return new SalesOrderDto
        {
            Id = o.Id,
            OrderNumber = o.OrderNumber,
            Status = o.Status,
            CreatedAt = o.CreatedAt,
            CompletedAt = o.CompletedAt,
            Lines = o.Lines.Select(l => new SalesOrderLineDto
            {
                Id = l.Id,
                ProductId = l.ProductId,
                ProductName = products.TryGetValue(l.ProductId, out var p) ? p.Name : "",
                WarehouseId = l.WarehouseId,
                WarehouseName = warehouses.TryGetValue(l.WarehouseId, out var w) ? w.Name : "",
                Quantity = l.Quantity
            }).ToList()
        };
    }

    public async Task FulfillSalesOrderAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var order = await _db.SalesOrders
            .Include(o => o.Lines)
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken)
            ?? throw new BusinessRuleException("Sales order not found.");

        if (order.Status != OrderStatus.Pending)
            throw new BusinessRuleException("Only pending sales orders can be fulfilled.");

        await using var tx = await _db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            foreach (var line in order.Lines)
            {
                await _ledger.ApplyChangeAsync(
                    line.ProductId,
                    line.WarehouseId,
                    -line.Quantity,
                    $"Sales shipment {order.OrderNumber}",
                    StockMovementType.SalesShipment,
                    null,
                    order.Id,
                    cancellationToken);
            }

            order.Status = OrderStatus.Completed;
            order.CompletedAt = DateTime.UtcNow;
            _audit.Enqueue(nameof(SalesOrder), order.Id.ToString(), "Fulfilled", order.OrderNumber, _currentUser.UserId, _currentUser.Email);
            await _db.SaveChangesAsync(cancellationToken);
            await tx.CommitAsync(cancellationToken);
        }
        catch
        {
            await tx.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private async Task<string> NextPoNumberAsync(CancellationToken cancellationToken)
    {
        var prefix = $"PO-{DateTime.UtcNow:yyyyMMdd}-";
        string number;
        do
        {
            number = prefix + Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
        } while (await _db.PurchaseOrders.AnyAsync(o => o.OrderNumber == number, cancellationToken));
        return number;
    }

    private async Task<string> NextSoNumberAsync(CancellationToken cancellationToken)
    {
        var prefix = $"SO-{DateTime.UtcNow:yyyyMMdd}-";
        string number;
        do
        {
            number = prefix + Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
        } while (await _db.SalesOrders.AnyAsync(o => o.OrderNumber == number, cancellationToken));
        return number;
    }
}
