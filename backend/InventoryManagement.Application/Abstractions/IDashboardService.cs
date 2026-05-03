using InventoryManagement.Application.DTOs.Dashboard;

namespace InventoryManagement.Application.Abstractions;

public interface IDashboardService
{
    Task<DashboardSummaryDto> GetSummaryAsync(CancellationToken cancellationToken = default);
}
