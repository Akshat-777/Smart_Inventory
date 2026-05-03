using InventoryManagement.Domain.Enums;

namespace InventoryManagement.Domain.Entities;

public class PurchaseOrder
{
    public Guid Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public OrderStatus Status { get; set; } = OrderStatus.Draft;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    public Guid CreatedByUserId { get; set; }

    public ICollection<PurchaseOrderLine> Lines { get; set; } = new List<PurchaseOrderLine>();
}
