using InventoryManagement.Application.Common;
using InventoryManagement.Application.DTOs.StockMovements;

namespace InventoryManagement.Application.Abstractions;

public interface IStockMovementQueryService
{
    Task<PagedResult<StockMovementDto>> GetPagedAsync(PaginationQuery query, Guid? productId, Guid? warehouseId, CancellationToken cancellationToken = default);
}
