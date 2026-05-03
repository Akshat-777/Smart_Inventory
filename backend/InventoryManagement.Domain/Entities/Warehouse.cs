namespace InventoryManagement.Domain.Entities;

public class Warehouse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;

    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    public ICollection<InventoryItem> InventoryItems { get; set; } = new List<InventoryItem>();
    public ICollection<StockMovement> StockMovements { get; set; } = new List<StockMovement>();
}
