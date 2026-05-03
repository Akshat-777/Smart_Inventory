using InventoryManagement.Application.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace InventoryManagement.Infrastructure.Jobs;

public class LowStockBackgroundJob
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<LowStockBackgroundJob> _logger;

    public LowStockBackgroundJob(IServiceScopeFactory scopeFactory, ILogger<LowStockBackgroundJob> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    /// <summary>Runs on a schedule to surface low-stock visibility (extend with email/webhooks).</summary>
    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var products = scope.ServiceProvider.GetRequiredService<IProductService>();

        var items = await products.GetLowStockAsync(cancellationToken);
        if (items.Count == 0)
        {
            _logger.LogInformation("Low stock check: no items below threshold.");
            return;
        }

        _logger.LogWarning("Low stock check: {Count} products below threshold.", items.Count);
        foreach (var i in items.Take(50))
            _logger.LogInformation("Low stock: {Name} ({Sku}) total={Qty} threshold={Th}",
                i.ProductName, i.Sku, i.TotalQuantityAcrossWarehouses, i.LowStockThreshold);
    }
}
