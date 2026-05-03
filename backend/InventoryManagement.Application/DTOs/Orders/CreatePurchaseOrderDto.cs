namespace InventoryManagement.Application.DTOs.Orders;

public class CreatePurchaseOrderDto
{
    public IReadOnlyList<CreatePurchaseOrderLineDto> Lines { get; set; } = Array.Empty<CreatePurchaseOrderLineDto>();
}

public class CreatePurchaseOrderLineDto
{
    public Guid ProductId { get; set; }
    public Guid WarehouseId { get; set; }
    public int Quantity { get; set; }
}
