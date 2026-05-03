using InventoryManagement.Application.Abstractions;
using InventoryManagement.Application.DTOs.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InventoryManagement.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _auth;

    public AuthController(IAuthService auth)
    {
        _auth = auth;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResponseDto>> Login([FromBody] LoginRequestDto request, CancellationToken cancellationToken)
    {
        var result = await _auth.LoginAsync(request, cancellationToken);
        return Ok(result);
    }

    [HttpPost("register")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<LoginResponseDto>> Register([FromBody] RegisterRequestDto request, CancellationToken cancellationToken)
    {
        var result = await _auth.RegisterAsync(request, cancellationToken);
        return Ok(result);
    }
}
