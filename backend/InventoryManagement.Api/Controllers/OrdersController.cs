using InventoryManagement.Application.Abstractions;
using InventoryManagement.Application.Common;
using InventoryManagement.Application.DTOs.Orders;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InventoryManagement.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _orders;

    public OrdersController(IOrderService orders)
    {
        _orders = orders;
    }

    [HttpGet("purchase")]
    public async Task<ActionResult<PagedResult<PurchaseOrderDto>>> PurchasePaged(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        CancellationToken cancellationToken = default)
    {
        var query = new PaginationQuery { Page = page, PageSize = pageSize, Search = search };
        var result = await _orders.GetPurchaseOrdersPagedAsync(query, cancellationToken);
        return Ok(result);
    }

    [HttpGet("purchase/{id:guid}")]
    public async Task<ActionResult<PurchaseOrderDto>> PurchaseById(Guid id, CancellationToken cancellationToken)
    {
        var o = await _orders.GetPurchaseOrderByIdAsync(id, cancellationToken);
        return o == null ? NotFound() : Ok(o);
    }

    [HttpPost("purchase")]
    [Authorize(Policy = "CanEdit")]
    public async Task<ActionResult<PurchaseOrderDto>> CreatePurchase([FromBody] CreatePurchaseOrderDto dto, CancellationToken cancellationToken)
    {
        var created = await _orders.CreatePurchaseOrderAsync(dto, cancellationToken);
        return CreatedAtAction(nameof(PurchaseById), new { id = created.Id }, created);
    }

    [HttpPost("purchase/{id:guid}/complete")]
    [Authorize(Policy = "CanEdit")]
    public async Task<IActionResult> CompletePurchase(Guid id, CancellationToken cancellationToken)
    {
        await _orders.CompletePurchaseOrderAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpGet("sales")]
    public async Task<ActionResult<PagedResult<SalesOrderDto>>> SalesPaged(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        CancellationToken cancellationToken = default)
    {
        var query = new PaginationQuery { Page = page, PageSize = pageSize, Search = search };
        var result = await _orders.GetSalesOrdersPagedAsync(query, cancellationToken);
        return Ok(result);
    }

    [HttpGet("sales/{id:guid}")]
    public async Task<ActionResult<SalesOrderDto>> SalesById(Guid id, CancellationToken cancellationToken)
    {
        var o = await _orders.GetSalesOrderByIdAsync(id, cancellationToken);
        return o == null ? NotFound() : Ok(o);
    }

    [HttpPost("sales")]
    [Authorize(Policy = "CanEdit")]
    public async Task<ActionResult<SalesOrderDto>> CreateSales([FromBody] CreateSalesOrderDto dto, CancellationToken cancellationToken)
    {
        var created = await _orders.CreateSalesOrderAsync(dto, cancellationToken);
        return CreatedAtAction(nameof(SalesById), new { id = created.Id }, created);
    }

    [HttpPost("sales/{id:guid}/fulfill")]
    [Authorize(Policy = "CanEdit")]
    public async Task<IActionResult> FulfillSales(Guid id, CancellationToken cancellationToken)
    {
        await _orders.FulfillSalesOrderAsync(id, cancellationToken);
        return NoContent();
    }
}
