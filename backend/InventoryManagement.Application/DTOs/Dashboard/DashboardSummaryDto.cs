namespace InventoryManagement.Application.DTOs.Dashboard;

public class DashboardSummaryDto
{
    public int TotalProducts { get; set; }
    public int TotalWarehouses { get; set; }
    public int LowStockProductCount { get; set; }
    public int PendingPurchaseOrders { get; set; }
    public int PendingSalesOrders { get; set; }
}
