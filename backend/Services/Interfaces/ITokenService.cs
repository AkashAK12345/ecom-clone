using ECommerce.Api.Models;

namespace ECommerce.Api.Services.Interfaces;

/// <summary>
/// Generates cryptographically signed JWT access tokens for authenticated users.
/// </summary>
public interface ITokenService
{
    /// <summary>
    /// Generates a signed HMAC-SHA256 JWT containing sub, email, role, and jti claims.
    /// </summary>
    string GenerateToken(User user);
}
