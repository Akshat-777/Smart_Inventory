using InventoryManagement.Application.Abstractions;
using InventoryManagement.Application.Common;
using InventoryManagement.Application.DTOs.Inventory;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InventoryManagement.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class InventoryController : ControllerBase
{
    private readonly IInventoryQueryService _inventory;

    public InventoryController(IInventoryQueryService inventory)
    {
        _inventory = inventory;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<InventoryItemDto>>> GetPaged(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] Guid? productId = null,
        [FromQuery] Guid? warehouseId = null,
        CancellationToken cancellationToken = default)
    {
        var query = new InventoryQueryDto
        {
            Page = page,
            PageSize = pageSize,
            Search = search,
            ProductId = productId,
            WarehouseId = warehouseId
        };
        var result = await _inventory.GetPagedAsync(query, cancellationToken);
        return Ok(result);
    }
}
