using InventoryManagement.Application.Abstractions;
using InventoryManagement.Application.Common;
using InventoryManagement.Application.DTOs.Inventory;
using InventoryManagement.Application.DTOs.Products;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InventoryManagement.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly IProductService _products;

    public ProductsController(IProductService products)
    {
        _products = products;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<ProductDto>>> GetPaged(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        CancellationToken cancellationToken = default)
    {
        var query = new PaginationQuery { Page = page, PageSize = pageSize, Search = search };
        var result = await _products.GetPagedAsync(query, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ProductDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var p = await _products.GetByIdAsync(id, cancellationToken);
        return p == null ? NotFound() : Ok(p);
    }

    [HttpPost]
    [Authorize(Policy = "CanEdit")]
    public async Task<ActionResult<ProductDto>> Create([FromBody] CreateProductDto dto, CancellationToken cancellationToken)
    {
        var created = await _products.CreateAsync(dto, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "CanEdit")]
    public async Task<ActionResult<ProductDto>> Update(Guid id, [FromBody] UpdateProductDto dto, CancellationToken cancellationToken)
    {
        var updated = await _products.UpdateAsync(id, dto, cancellationToken);
        return Ok(updated);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "CanEdit")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _products.DeleteAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpGet("low-stock")]
    public async Task<ActionResult<IReadOnlyList<LowStockItemDto>>> LowStock(CancellationToken cancellationToken)
    {
        var items = await _products.GetLowStockAsync(cancellationToken);
        return Ok(items);
    }
}
