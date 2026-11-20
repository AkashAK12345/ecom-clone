using ECommerce.Api.Models;
using ECommerce.Api.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Api.Controllers;

/// <summary>
/// Thin HTTP layer for Authentication (Registration & Login).
/// Responsibilities: model validation, delegating to IAuthService, and mapping results to HTTP responses.
/// No business rules, password hashing, database queries, or JWT building live here.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    // -------------------------------------------------------------------------
    // POST /api/auth/register
    // -------------------------------------------------------------------------

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterDto dto, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var result = await _authService.RegisterAsync(dto, ct);

        if (!result.IsSuccess)
        {
            return StatusCode(result.StatusCode, result.Error);
        }

        var r = result.Value!;
        return Ok(new
        {
            token = r.Token,
            userId = r.UserId,
            email = r.Email,
            role = r.Role,
            message = r.Message
        });
    }

    // -------------------------------------------------------------------------
    // POST /api/auth/login
    // -------------------------------------------------------------------------

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto dto, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var result = await _authService.LoginAsync(dto, ct);

        if (!result.IsSuccess)
        {
            return StatusCode(result.StatusCode, result.Error);
        }

        var r = result.Value!;
        return Ok(new
        {
            token = r.Token,
            userId = r.UserId,
            email = r.Email,
            role = r.Role,
            message = r.Message
        });
    }
}
