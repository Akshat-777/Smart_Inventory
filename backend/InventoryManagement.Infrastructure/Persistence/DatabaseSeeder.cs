using InventoryManagement.Application.Auth;
using InventoryManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace InventoryManagement.Infrastructure.Persistence;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(ApplicationDbContext db, ILogger logger)
    {
        const string adminEmail = "admin@inventory.local";
        if (!await db.Users.AnyAsync(u => u.Email == adminEmail))
        {
            var admin = new UserAccount
            {
                Id = Guid.NewGuid(),
                Email = adminEmail,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!"),
                Role = AppRoles.Admin,
                CreatedAt = DateTime.UtcNow
            };
            db.Users.Add(admin);
            await db.SaveChangesAsync();
            logger.LogInformation("Seeded admin user {Email}", adminEmail);
        }
    }
}
