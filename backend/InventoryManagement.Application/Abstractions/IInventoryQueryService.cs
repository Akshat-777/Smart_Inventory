using InventoryManagement.Application.Common;
using InventoryManagement.Application.DTOs.Inventory;

namespace InventoryManagement.Application.Abstractions;

public interface IInventoryQueryService
{
    Task<PagedResult<InventoryItemDto>> GetPagedAsync(InventoryQueryDto query, CancellationToken cancellationToken = default);
}
