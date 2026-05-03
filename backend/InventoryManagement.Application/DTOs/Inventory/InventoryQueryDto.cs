using InventoryManagement.Application.Common;

namespace InventoryManagement.Application.DTOs.Inventory;

public class InventoryQueryDto : PaginationQuery
{
    public Guid? ProductId { get; set; }
    public Guid? WarehouseId { get; set; }
}
