namespace InventoryManagement.Domain.Entities;

public class SalesOrderLine
{
    public Guid Id { get; set; }
    public Guid SalesOrderId { get; set; }
    public SalesOrder SalesOrder { get; set; } = null!;

    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;

    public Guid WarehouseId { get; set; }
    public Warehouse Warehouse { get; set; } = null!;

    public int Quantity { get; set; }
}
