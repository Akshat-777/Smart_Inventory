using InventoryManagement.Application.Abstractions;
using InventoryManagement.Application.DTOs.Dashboard;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InventoryManagement.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboard;

    public DashboardController(IDashboardService dashboard)
    {
        _dashboard = dashboard;
    }

    [HttpGet("summary")]
    public async Task<ActionResult<DashboardSummaryDto>> Summary(CancellationToken cancellationToken)
    {
        var s = await _dashboard.GetSummaryAsync(cancellationToken);
        return Ok(s);
    }
}
