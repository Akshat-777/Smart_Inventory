namespace InventoryManagement.Application.DTOs.Inventory;

public class LowStockItemDto
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public int TotalQuantityAcrossWarehouses { get; set; }
    public int LowStockThreshold { get; set; }
}
