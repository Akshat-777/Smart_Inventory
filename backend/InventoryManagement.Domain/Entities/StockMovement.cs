using InventoryManagement.Domain.Enums;

namespace InventoryManagement.Domain.Entities;

public class StockMovement
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;

    public Guid WarehouseId { get; set; }
    public Warehouse Warehouse { get; set; } = null!;

    /// <summary>Positive = inbound, negative = outbound.</summary>
    public int QuantityChange { get; set; }

    public string Reason { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public StockMovementType MovementType { get; set; }

    public Guid? PerformedByUserId { get; set; }
    public string? Reference { get; set; }
    public Guid? PurchaseOrderId { get; set; }
    public Guid? SalesOrderId { get; set; }
}
