using InventoryManagement.Domain.Enums;

namespace InventoryManagement.Application.DTOs.Orders;

public class SalesOrderDto
{
    public Guid Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public OrderStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public IReadOnlyList<SalesOrderLineDto> Lines { get; set; } = Array.Empty<SalesOrderLineDto>();
}

public class SalesOrderLineDto
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public Guid WarehouseId { get; set; }
    public string WarehouseName { get; set; } = string.Empty;
    public int Quantity { get; set; }
}
