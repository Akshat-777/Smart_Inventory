using InventoryManagement.Domain.Enums;

namespace InventoryManagement.Application.Abstractions;

/// <summary>Single entry point for all stock changes. Always creates a movement record.</summary>
public interface IInventoryLedgerService
{
    Task ApplyChangeAsync(
        Guid productId,
        Guid warehouseId,
        int quantityChange,
        string reason,
        StockMovementType movementType,
        Guid? purchaseOrderId,
        Guid? salesOrderId,
        CancellationToken cancellationToken = default);
}
