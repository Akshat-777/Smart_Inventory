namespace InventoryManagement.Application.DTOs.Orders;

public class CreateSalesOrderDto
{
    public IReadOnlyList<CreateSalesOrderLineDto> Lines { get; set; } = Array.Empty<CreateSalesOrderLineDto>();
}

public class CreateSalesOrderLineDto
{
    public Guid ProductId { get; set; }
    public Guid WarehouseId { get; set; }
    public int Quantity { get; set; }
}
