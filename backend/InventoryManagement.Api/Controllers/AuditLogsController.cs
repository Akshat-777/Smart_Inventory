using InventoryManagement.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagement.Api.Controllers;

[ApiController]
[Authorize(Policy = "AdminOnly")]
[Route("api/[controller]")]
public class AuditLogsController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public AuditLogsController(ApplicationDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<PagedAuditResult>> Get(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        pageSize = Math.Clamp(pageSize, 1, 200);
        var q = _db.AuditLogs.AsNoTracking().OrderByDescending(a => a.Timestamp);
        var total = await q.CountAsync(cancellationToken);
        var items = await q
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new AuditLogItemDto
            {
                Id = a.Id,
                EntityName = a.EntityName,
                EntityId = a.EntityId,
                Action = a.Action,
                Details = a.Details,
                UserName = a.UserName,
                Timestamp = a.Timestamp
            })
            .ToListAsync(cancellationToken);

        return Ok(new PagedAuditResult
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = total
        });
    }

    public class PagedAuditResult
    {
        public List<AuditLogItemDto> Items { get; set; } = new();
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
    }

    public class AuditLogItemDto
    {
        public Guid Id { get; set; }
        public string EntityName { get; set; } = string.Empty;
        public string EntityId { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string? Details { get; set; }
        public string? UserName { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
