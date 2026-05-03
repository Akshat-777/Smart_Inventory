namespace InventoryManagement.Application.DTOs.Warehouses;

public class UpdateWarehouseDto
{
    public string Name { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public byte[]? RowVersion { get; set; }
}
