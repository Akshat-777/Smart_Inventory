namespace InventoryManagement.Domain.Entities;

public class Product
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public decimal Price { get; set; }
    /// <summary>Alert when total quantity across all warehouses is at or below this value.</summary>
    public int LowStockThreshold { get; set; }

    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    public ICollection<InventoryItem> InventoryItems { get; set; } = new List<InventoryItem>();
    public ICollection<StockMovement> StockMovements { get; set; } = new List<StockMovement>();
}
