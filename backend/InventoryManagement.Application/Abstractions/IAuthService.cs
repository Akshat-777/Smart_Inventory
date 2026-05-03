using InventoryManagement.Application.DTOs.Auth;

namespace InventoryManagement.Application.Abstractions;

public interface IAuthService
{
    Task<LoginResponseDto> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken = default);
    Task<LoginResponseDto> RegisterAsync(RegisterRequestDto request, CancellationToken cancellationToken = default);
}
