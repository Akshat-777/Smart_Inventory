using InventoryManagement.Application.Abstractions;
using InventoryManagement.Application.Exceptions;
using InventoryManagement.Domain.Entities;
using InventoryManagement.Domain.Enums;
using InventoryManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagement.Infrastructure.Services;

public class InventoryLedgerService : IInventoryLedgerService
{
    private readonly ApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public InventoryLedgerService(ApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task ApplyChangeAsync(
        Guid productId,
        Guid warehouseId,
        int quantityChange,
        string reason,
        StockMovementType movementType,
        Guid? purchaseOrderId,
        Guid? salesOrderId,
        CancellationToken cancellationToken = default)
    {
        if (quantityChange == 0)
            return;

        var productExists = await _db.Products.AsNoTracking().AnyAsync(p => p.Id == productId, cancellationToken);
        if (!productExists)
            throw new BusinessRuleException("Product not found.");

        var warehouseExists = await _db.Warehouses.AsNoTracking().AnyAsync(w => w.Id == warehouseId, cancellationToken);
        if (!warehouseExists)
            throw new BusinessRuleException("Warehouse not found.");

        const int maxAttempts = 5;
        for (var attempt = 0; attempt < maxAttempts; attempt++)
        {
            try
            {
                var item = await _db.InventoryItems
                    .FirstOrDefaultAsync(i => i.ProductId == productId && i.WarehouseId == warehouseId, cancellationToken);

                if (item == null)
                {
                    if (quantityChange < 0)
                        throw new BusinessRuleException("Cannot remove stock: no inventory record for this product and warehouse.");

                    item = new InventoryItem
                    {
                        ProductId = productId,
                        WarehouseId = warehouseId,
                        Quantity = quantityChange
                    };
                    _db.InventoryItems.Add(item);
                }
                else
                {
                    var newQty = item.Quantity + quantityChange;
                    if (newQty < 0)
                        throw new BusinessRuleException("Insufficient stock for this operation.");

                    item.Quantity = newQty;
                }

                _db.StockMovements.Add(new StockMovement
                {
                    Id = Guid.NewGuid(),
                    ProductId = productId,
                    WarehouseId = warehouseId,
                    QuantityChange = quantityChange,
                    Reason = reason,
                    Timestamp = DateTime.UtcNow,
                    MovementType = movementType,
                    PerformedByUserId = _currentUser.UserId,
                    PurchaseOrderId = purchaseOrderId,
                    SalesOrderId = salesOrderId,
                    Reference = purchaseOrderId.HasValue ? $"PO:{purchaseOrderId}" : salesOrderId.HasValue ? $"SO:{salesOrderId}" : null
                });

                await _db.SaveChangesAsync(cancellationToken);
                return;
            }
            catch (DbUpdateConcurrencyException)
            {
                _db.ChangeTracker.Clear();
                if (attempt == maxAttempts - 1)
                    throw new ConcurrencyConflictException();
            }
        }
    }
}
