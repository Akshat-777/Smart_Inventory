namespace InventoryManagement.Application.DTOs.Products;

public class ProductDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int LowStockThreshold { get; set; }
    /// <summary>For optimistic concurrency on updates (JSON: base64).</summary>
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}
