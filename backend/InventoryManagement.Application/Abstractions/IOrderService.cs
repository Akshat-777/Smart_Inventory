using InventoryManagement.Application.Common;
using InventoryManagement.Application.DTOs.Orders;

namespace InventoryManagement.Application.Abstractions;

public interface IOrderService
{
    Task<PurchaseOrderDto> CreatePurchaseOrderAsync(CreatePurchaseOrderDto dto, CancellationToken cancellationToken = default);
    Task<PagedResult<PurchaseOrderDto>> GetPurchaseOrdersPagedAsync(PaginationQuery query, CancellationToken cancellationToken = default);
    Task<PurchaseOrderDto?> GetPurchaseOrderByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task CompletePurchaseOrderAsync(Guid id, CancellationToken cancellationToken = default);

    Task<SalesOrderDto> CreateSalesOrderAsync(CreateSalesOrderDto dto, CancellationToken cancellationToken = default);
    Task<PagedResult<SalesOrderDto>> GetSalesOrdersPagedAsync(PaginationQuery query, CancellationToken cancellationToken = default);
    Task<SalesOrderDto?> GetSalesOrderByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task FulfillSalesOrderAsync(Guid id, CancellationToken cancellationToken = default);
}
