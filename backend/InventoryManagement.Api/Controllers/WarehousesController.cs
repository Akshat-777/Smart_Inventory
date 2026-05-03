using InventoryManagement.Application.Abstractions;
using InventoryManagement.Application.Common;
using InventoryManagement.Application.DTOs.Warehouses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InventoryManagement.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class WarehousesController : ControllerBase
{
    private readonly IWarehouseService _warehouses;

    public WarehousesController(IWarehouseService warehouses)
    {
        _warehouses = warehouses;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<WarehouseDto>>> GetPaged(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        CancellationToken cancellationToken = default)
    {
        var query = new PaginationQuery { Page = page, PageSize = pageSize, Search = search };
        var result = await _warehouses.GetPagedAsync(query, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<WarehouseDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var w = await _warehouses.GetByIdAsync(id, cancellationToken);
        return w == null ? NotFound() : Ok(w);
    }

    [HttpPost]
    [Authorize(Policy = "CanEdit")]
    public async Task<ActionResult<WarehouseDto>> Create([FromBody] CreateWarehouseDto dto, CancellationToken cancellationToken)
    {
        var created = await _warehouses.CreateAsync(dto, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "CanEdit")]
    public async Task<ActionResult<WarehouseDto>> Update(Guid id, [FromBody] UpdateWarehouseDto dto, CancellationToken cancellationToken)
    {
        var updated = await _warehouses.UpdateAsync(id, dto, cancellationToken);
        return Ok(updated);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "CanEdit")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _warehouses.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
