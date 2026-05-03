using InventoryManagement.Domain.Enums;

namespace InventoryManagement.Domain.Entities;

public class SalesOrder
{
    public Guid Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public OrderStatus Status { get; set; } = OrderStatus.Draft;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    public Guid CreatedByUserId { get; set; }

    public ICollection<SalesOrderLine> Lines { get; set; } = new List<SalesOrderLine>();
}
