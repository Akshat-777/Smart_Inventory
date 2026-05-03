using InventoryManagement.Application.Abstractions;
using InventoryManagement.Application.DTOs.StockMovements;
using InventoryManagement.Application.Exceptions;
using InventoryManagement.Domain.Enums;

namespace InventoryManagement.Infrastructure.Services;

public class ManualAdjustmentService : IManualAdjustmentService
{
    private readonly IInventoryLedgerService _ledger;

    public ManualAdjustmentService(IInventoryLedgerService ledger)
    {
        _ledger = ledger;
    }

    public async Task ApplyAsync(ManualAdjustmentDto dto, CancellationToken cancellationToken = default)
    {
        if (dto.QuantityChange == 0)
            throw new BusinessRuleException("Quantity change must be non-zero.");

        await _ledger.ApplyChangeAsync(
            dto.ProductId,
            dto.WarehouseId,
            dto.QuantityChange,
            string.IsNullOrWhiteSpace(dto.Reason) ? "Manual adjustment" : dto.Reason.Trim(),
            StockMovementType.Adjustment,
            null,
            null,
            cancellationToken);
    }
}
