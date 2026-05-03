using InventoryManagement.Application.Common;
using InventoryManagement.Domain.Entities;

namespace InventoryManagement.Application.Abstractions.Repositories;

public interface IProductRepository
{
    Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Product?> GetTrackedByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> SkuExistsAsync(string sku, Guid? excludeProductId, CancellationToken cancellationToken = default);
    Task AddAsync(Product product, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<Product> Items, int TotalCount)> GetPagedAsync(PaginationQuery query, CancellationToken cancellationToken = default);
    void Remove(Product product);
}
