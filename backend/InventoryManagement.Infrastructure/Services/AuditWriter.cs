using InventoryManagement.Domain.Entities;
using InventoryManagement.Infrastructure.Persistence;

namespace InventoryManagement.Infrastructure.Services;

/// <summary>Queues audit rows on the current DbContext; caller must SaveChanges.</summary>
public class AuditWriter
{
    private readonly ApplicationDbContext _db;

    public AuditWriter(ApplicationDbContext db)
    {
        _db = db;
    }

    public void Enqueue(string entityName, string entityId, string action, string? details, Guid? userId, string? userName)
    {
        _db.AuditLogs.Add(new AuditLog
        {
            Id = Guid.NewGuid(),
            EntityName = entityName,
            EntityId = entityId,
            Action = action,
            Details = details,
            UserId = userId,
            UserName = userName,
            Timestamp = DateTime.UtcNow
        });
    }
}
