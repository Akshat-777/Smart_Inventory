namespace InventoryManagement.Application.DTOs.StockMovements;

/// <summary>Admin/Manager adjustment — always recorded as StockMovement.</summary>
public class ManualAdjustmentDto
{
    public Guid ProductId { get; set; }
    public Guid WarehouseId { get; set; }
    public int QuantityChange { get; set; }
    public string Reason { get; set; } = string.Empty;
}
