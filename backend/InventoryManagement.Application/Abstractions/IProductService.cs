using InventoryManagement.Application.Common;
using InventoryManagement.Application.DTOs.Inventory;
using InventoryManagement.Application.DTOs.Products;

namespace InventoryManagement.Application.Abstractions;

public interface IProductService
{
    Task<ProductDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PagedResult<ProductDto>> GetPagedAsync(PaginationQuery query, CancellationToken cancellationToken = default);
    Task<ProductDto> CreateAsync(CreateProductDto dto, CancellationToken cancellationToken = default);
    Task<ProductDto> UpdateAsync(Guid id, UpdateProductDto dto, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LowStockItemDto>> GetLowStockAsync(CancellationToken cancellationToken = default);
}
