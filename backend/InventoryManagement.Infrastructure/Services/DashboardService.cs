using InventoryManagement.Application.Abstractions;
using InventoryManagement.Application.DTOs.Dashboard;
using InventoryManagement.Domain.Enums;
using InventoryManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagement.Infrastructure.Services;

public class DashboardService : IDashboardService
{
    private readonly ApplicationDbContext _db;
    private readonly IProductService _products;

    public DashboardService(ApplicationDbContext db, IProductService products)
    {
        _db = db;
        _products = products;
    }

    public async Task<DashboardSummaryDto> GetSummaryAsync(CancellationToken cancellationToken = default)
    {
        var totalProducts = await _db.Products.AsNoTracking().CountAsync(cancellationToken);
        var totalWarehouses = await _db.Warehouses.AsNoTracking().CountAsync(cancellationToken);
        var lowStock = await _products.GetLowStockAsync(cancellationToken);
        var pendingPo = await _db.PurchaseOrders.AsNoTracking().CountAsync(o => o.Status == OrderStatus.Pending, cancellationToken);
        var pendingSo = await _db.SalesOrders.AsNoTracking().CountAsync(o => o.Status == OrderStatus.Pending, cancellationToken);

        return new DashboardSummaryDto
        {
            TotalProducts = totalProducts,
            TotalWarehouses = totalWarehouses,
            LowStockProductCount = lowStock.Count,
            PendingPurchaseOrders = pendingPo,
            PendingSalesOrders = pendingSo
        };
    }
}
