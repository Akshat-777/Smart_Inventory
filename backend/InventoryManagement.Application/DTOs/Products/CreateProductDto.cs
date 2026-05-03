namespace InventoryManagement.Application.DTOs.Products;

public class CreateProductDto
{
    public string Name { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int LowStockThreshold { get; set; } = 10;
}
