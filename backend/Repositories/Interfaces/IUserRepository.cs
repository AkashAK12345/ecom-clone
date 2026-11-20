using ECommerce.Api.Models;

namespace ECommerce.Api.Repositories.Interfaces;

/// <summary>
/// Encapsulates all database operations for the Users table.
/// No business rules or password hashing logic here — only EF Core queries, inserts, and saves.
/// </summary>
public interface IUserRepository
{
    /// <summary>
    /// Checks whether an account already exists with the given email (case-insensitive).
    /// </summary>
    Task<bool> EmailExistsAsync(string email, CancellationToken ct = default);

    /// <summary>
    /// Finds a user by normalized email, or returns null if not found.
    /// </summary>
    Task<User?> GetByEmailAsync(string email, CancellationToken ct = default);

    /// <summary>
    /// Finds a user by ID, or returns null if not found.
    /// </summary>
    Task<User?> GetByIdAsync(int id, CancellationToken ct = default);

    /// <summary>
    /// Adds a new user entity and persists to the database.
    /// </summary>
    Task AddAsync(User user, CancellationToken ct = default);

    /// <summary>
    /// Persists tracked entity modifications (e.g., password rehash).
    /// </summary>
    Task SaveChangesAsync(CancellationToken ct = default);
}
