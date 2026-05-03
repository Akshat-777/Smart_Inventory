using InventoryManagement.Application.Common;
using InventoryManagement.Domain.Entities;

namespace InventoryManagement.Application.Abstractions.Repositories;

public interface IWarehouseRepository
{
    Task<Warehouse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Warehouse?> GetTrackedByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(Warehouse warehouse, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<Warehouse> Items, int TotalCount)> GetPagedAsync(PaginationQuery query, CancellationToken cancellationToken = default);
    void Remove(Warehouse warehouse);
}
