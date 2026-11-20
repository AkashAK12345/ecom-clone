using ECommerce.Api.Models;
using ECommerce.Api.Repositories.Interfaces;
using ECommerce.Api.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;

namespace ECommerce.Api.Services;

/// <summary>
/// Implements IAuthService — manages registration, login, password salting/hashing, and token issuance.
/// Database persistence is delegated to IUserRepository; JWT generation is delegated to ITokenService.
/// </summary>
public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepo;
    private readonly ITokenService _tokenService;
    private static readonly PasswordHasher<User> _hasher = new();

    public AuthService(IUserRepository userRepo, ITokenService tokenService)
    {
        _userRepo = userRepo;
        _tokenService = tokenService;
    }

    public async Task<ServiceResult<AuthResult>> RegisterAsync(RegisterDto dto, CancellationToken ct = default)
    {
        // Business rule: email uniqueness (normalized case-insensitive check)
        var exists = await _userRepo.EmailExistsAsync(dto.Email, ct);
        if (exists)
        {
            return ServiceResult<AuthResult>.Fail(
                StatusCodes.Status409Conflict,
                "An account with this email already exists."
            );
        }

        // Normalize email and build new user entity
        var user = new User
        {
            Email = dto.Email.Trim().ToLower(),
            Role = dto.Role,
            CreatedAt = DateTime.UtcNow
        };

        // Secure password hashing via ASP.NET Core PasswordHasher (PBKDF2)
        user.PasswordHash = _hasher.HashPassword(user, dto.Password);

        await _userRepo.AddAsync(user, ct);

        // Generate signed JWT token
        var token = _tokenService.GenerateToken(user);

        var result = new AuthResult(
            Token: token,
            UserId: user.Id,
            Email: user.Email,
            Role: user.Role,
            Message: $"Registration successful. Welcome, {user.Email}!"
        );

        return ServiceResult<AuthResult>.Ok(result);
    }

    public async Task<ServiceResult<AuthResult>> LoginAsync(LoginDto dto, CancellationToken ct = default)
    {
        // Find user by normalized email
        var user = await _userRepo.GetByEmailAsync(dto.Email, ct);
        if (user == null)
        {
            return ServiceResult<AuthResult>.Fail(
                StatusCodes.Status401Unauthorized,
                "Invalid email or password."
            );
        }

        // Verify hashed password
        var verificationResult = _hasher.VerifyHashedPassword(user, user.PasswordHash, dto.Password);
        if (verificationResult == PasswordVerificationResult.Failed)
        {
            return ServiceResult<AuthResult>.Fail(
                StatusCodes.Status401Unauthorized,
                "Invalid email or password."
            );
        }

        // Rehash password if standard security parameters were upgraded
        if (verificationResult == PasswordVerificationResult.SuccessRehashNeeded)
        {
            user.PasswordHash = _hasher.HashPassword(user, dto.Password);
            await _userRepo.SaveChangesAsync(ct);
        }

        // Generate signed JWT token
        var token = _tokenService.GenerateToken(user);

        var result = new AuthResult(
            Token: token,
            UserId: user.Id,
            Email: user.Email,
            Role: user.Role,
            Message: "Login successful."
        );

        return ServiceResult<AuthResult>.Ok(result);
    }
}
