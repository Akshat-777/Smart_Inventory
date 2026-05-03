using InventoryManagement.Application.Abstractions;
using InventoryManagement.Application.Auth;
using InventoryManagement.Application.DTOs.Auth;
using InventoryManagement.Application.Exceptions;
using InventoryManagement.Domain.Entities;
using InventoryManagement.Infrastructure.Auth;
using InventoryManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagement.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly ApplicationDbContext _db;
    private readonly JwtTokenService _tokens;

    public AuthService(ApplicationDbContext db, JwtTokenService tokens)
    {
        _db = db;
        _tokens = tokens;
    }

    public async Task<LoginResponseDto> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken = default)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Email == email, cancellationToken);
        if (user == null)
            throw new BusinessRuleException("Invalid credentials.");

        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            throw new BusinessRuleException("Invalid credentials.");

        var roles = new[] { user.Role };
        var (token, expires) = _tokens.CreateToken(user.Id, user.Email, roles);

        return new LoginResponseDto
        {
            Token = token,
            ExpiresAt = expires,
            Email = user.Email,
            Roles = roles.ToList()
        };
    }

    public async Task<LoginResponseDto> RegisterAsync(RegisterRequestDto request, CancellationToken cancellationToken = default)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        if (await _db.Users.AnyAsync(u => u.Email == email, cancellationToken))
            throw new BusinessRuleException("Email is already registered.");

        var role = NormalizeRole(request.Role);
        var user = new UserAccount
        {
            Id = Guid.NewGuid(),
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Role = role,
            CreatedAt = DateTime.UtcNow
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync(cancellationToken);

        var roles = new[] { user.Role };
        var (token, expires) = _tokens.CreateToken(user.Id, user.Email, roles);

        return new LoginResponseDto
        {
            Token = token,
            ExpiresAt = expires,
            Email = user.Email,
            Roles = roles.ToList()
        };
    }

    private static string NormalizeRole(string role)
    {
        return role switch
        {
            AppRoles.Admin => AppRoles.Admin,
            AppRoles.Manager => AppRoles.Manager,
            _ => AppRoles.Viewer
        };
    }
}
