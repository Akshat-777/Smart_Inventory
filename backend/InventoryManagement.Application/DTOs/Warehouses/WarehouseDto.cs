namespace InventoryManagement.Application.DTOs.Warehouses;

public class WarehouseDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}
