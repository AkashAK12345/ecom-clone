using ECommerce.Api.Data;
using ECommerce.Api.Models;
using ECommerce.Api.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Api.Repositories;

/// <summary>
/// Implements IProductRepository using EF Core + AppDbContext.
/// Only data-access concerns belong here: queries, includes, inserts, and saves.
/// </summary>
public class ProductRepository : IProductRepository
{
    private readonly AppDbContext _db;

    public ProductRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<Product>> GetAllAsync(CancellationToken ct = default)
    {
        return await _db.Products
            .Include(p => p.Sizes.OrderBy(s => s.Id))
            .ToListAsync(ct);
    }

    public async Task<List<Product>> GetBySellerAsync(int sellerId, CancellationToken ct = default)
    {
        return await _db.Products
            .Include(p => p.Sizes.OrderBy(s => s.Id))
            .Where(p => p.SellerId == sellerId)
            .ToListAsync(ct);
    }

    public async Task<List<Product>> GetByCategoryAsync(string category, CancellationToken ct = default)
    {
        return await _db.Products
            .Include(p => p.Sizes.OrderBy(s => s.Id))
            .Where(p => p.Category == category)
            .ToListAsync(ct);
    }

    public async Task<Product?> GetByIdWithSizesAsync(int id, CancellationToken ct = default)
    {
        return await _db.Products
            .Include(p => p.Sizes)
            .FirstOrDefaultAsync(p => p.Id == id, ct);
    }

    public async Task AddAsync(Product product, CancellationToken ct = default)
    {
        _db.Products.Add(product);
        await _db.SaveChangesAsync(ct);
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
    {
        await _db.SaveChangesAsync(ct);
    }
}
