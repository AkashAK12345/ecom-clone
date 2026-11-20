using ECommerce.Api.Data;
using ECommerce.Api.Models;
using ECommerce.Api.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Api.Repositories;

/// <summary>
/// Implements IUserRepository using EF Core and AppDbContext.
/// </summary>
public class UserRepository : IUserRepository
{
    private readonly AppDbContext _db;

    public UserRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<bool> EmailExistsAsync(string email, CancellationToken ct = default)
    {
        var normalized = email.Trim().ToLower();
        return await _db.Users
            .AnyAsync(u => u.Email.ToLower() == normalized, ct);
    }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken ct = default)
    {
        var normalized = email.Trim().ToLower();
        return await _db.Users
            .FirstOrDefaultAsync(u => u.Email == normalized, ct);
    }

    public async Task<User?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        return await _db.Users.FindAsync([id], ct);
    }

    public async Task AddAsync(User user, CancellationToken ct = default)
    {
        _db.Users.Add(user);
        await _db.SaveChangesAsync(ct);
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
    {
        await _db.SaveChangesAsync(ct);
    }
}
