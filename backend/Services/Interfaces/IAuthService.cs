using ECommerce.Api.Models;

namespace ECommerce.Api.Services.Interfaces;

/// <summary>
/// Data contract returned by successful registration or login.
/// </summary>
public record AuthResult(
    string Token,
    int UserId,
    string Email,
    string Role,
    string Message
);

/// <summary>
/// Defines business operations for authentication and user credential lifecycle.
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// Registers a new user account, hashes their password, persists to database, and issues a JWT.
    /// Returns 409 Conflict if email is already in use.
    /// </summary>
    Task<ServiceResult<AuthResult>> RegisterAsync(RegisterDto dto, CancellationToken ct = default);

    /// <summary>
    /// Authenticates a user with email and password, rehashes password if algorithm upgrade is needed, and issues a JWT.
    /// Returns 401 Unauthorized if email or password does not match.
    /// </summary>
    Task<ServiceResult<AuthResult>> LoginAsync(LoginDto dto, CancellationToken ct = default);
}
