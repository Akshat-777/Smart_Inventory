using InventoryManagement.Application.DTOs.StockMovements;

namespace InventoryManagement.Application.Abstractions;

public interface IManualAdjustmentService
{
    Task ApplyAsync(ManualAdjustmentDto dto, CancellationToken cancellationToken = default);
}
