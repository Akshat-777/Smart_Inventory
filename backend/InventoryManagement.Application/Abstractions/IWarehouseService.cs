using InventoryManagement.Application.Common;
using InventoryManagement.Application.DTOs.Warehouses;

namespace InventoryManagement.Application.Abstractions;

public interface IWarehouseService
{
    Task<WarehouseDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PagedResult<WarehouseDto>> GetPagedAsync(PaginationQuery query, CancellationToken cancellationToken = default);
    Task<WarehouseDto> CreateAsync(CreateWarehouseDto dto, CancellationToken cancellationToken = default);
    Task<WarehouseDto> UpdateAsync(Guid id, UpdateWarehouseDto dto, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
