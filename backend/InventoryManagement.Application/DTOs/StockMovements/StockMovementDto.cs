using InventoryManagement.Domain.Enums;

namespace InventoryManagement.Application.DTOs.StockMovements;

public class StockMovementDto
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public Guid WarehouseId { get; set; }
    public string WarehouseName { get; set; } = string.Empty;
    public int QuantityChange { get; set; }
    public string Reason { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public StockMovementType MovementType { get; set; }
    public Guid? PurchaseOrderId { get; set; }
    public Guid? SalesOrderId { get; set; }
}
