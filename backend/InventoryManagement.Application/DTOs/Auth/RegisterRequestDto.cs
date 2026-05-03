using InventoryManagement.Application.Auth;

namespace InventoryManagement.Application.DTOs.Auth;

public class RegisterRequestDto
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Role { get; set; } = AppRoles.Viewer;
}
