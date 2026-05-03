namespace InventoryManagement.Domain.Entities;

/// <summary>Stock level for a product in a specific warehouse. Concurrency via RowVersion.</summary>
public class InventoryItem
{
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;

    public Guid WarehouseId { get; set; }
    public Warehouse Warehouse { get; set; } = null!;

    public int Quantity { get; set; }

    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}
