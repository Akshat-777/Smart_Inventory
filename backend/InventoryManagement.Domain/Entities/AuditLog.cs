namespace InventoryManagement.Domain.Entities;

public class AuditLog
{
    public Guid Id { get; set; }
    public string EntityName { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string? Details { get; set; }
    public Guid? UserId { get; set; }
    public string? UserName { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
