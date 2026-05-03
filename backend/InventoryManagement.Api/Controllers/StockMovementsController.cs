using InventoryManagement.Application.Abstractions;
using InventoryManagement.Application.Common;
using InventoryManagement.Application.DTOs.StockMovements;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InventoryManagement.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class StockMovementsController : ControllerBase
{
    private readonly IStockMovementQueryService _movements;
    private readonly IManualAdjustmentService _adjustments;

    public StockMovementsController(IStockMovementQueryService movements, IManualAdjustmentService adjustments)
    {
        _movements = movements;
        _adjustments = adjustments;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<StockMovementDto>>> GetPaged(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] Guid? productId = null,
        [FromQuery] Guid? warehouseId = null,
        CancellationToken cancellationToken = default)
    {
        var query = new PaginationQuery { Page = page, PageSize = pageSize, Search = search };
        var result = await _movements.GetPagedAsync(query, productId, warehouseId, cancellationToken);
        return Ok(result);
    }

    [HttpPost("adjust")]
    [Authorize(Policy = "CanEdit")]
    public async Task<IActionResult> Adjust([FromBody] ManualAdjustmentDto dto, CancellationToken cancellationToken)
    {
        await _adjustments.ApplyAsync(dto, cancellationToken);
        return NoContent();
    }
}
