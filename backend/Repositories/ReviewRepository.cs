using ECommerce.Api.Data;
using ECommerce.Api.Models;
using ECommerce.Api.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Api.Repositories;

/// <summary>
/// Implements IReviewRepository using EF Core + AppDbContext.
/// Only data-access concerns live here: finds, any-checks, inserts, projections, and saves.
/// </summary>
public class ReviewRepository : IReviewRepository
{
    private readonly AppDbContext _db;

    public ReviewRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<Product?> FindProductAsync(int productId, CancellationToken ct = default)
    {
        return await _db.Products.FindAsync([productId], ct);
    }

    public async Task<bool> HasBuyerReviewedAsync(int productId, int buyerId, CancellationToken ct = default)
    {
        return await _db.Reviews
            .AnyAsync(r => r.ProductId == productId && r.BuyerId == buyerId, ct);
    }

    public async Task<string> GetBuyerEmailAsync(int buyerId, CancellationToken ct = default)
    {
        var buyer = await _db.Users.FindAsync([buyerId], ct);
        return buyer?.Email ?? "";
    }

    public async Task AddReviewAsync(Review review, CancellationToken ct = default)
    {
        _db.Reviews.Add(review);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<List<ReviewDto>> GetReviewsByProductAsync(int productId, CancellationToken ct = default)
    {
        return await _db.Reviews
            .Include(r => r.Buyer)
            .Include(r => r.Product)
            .Where(r => r.ProductId == productId)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new ReviewDto
            {
                Id = r.Id,
                ProductId = r.ProductId,
                ProductName = r.Product != null ? r.Product.Name : "",
                BuyerId = r.BuyerId,
                BuyerEmail = r.Buyer != null ? r.Buyer.Email : "",
                Rating = r.Rating,
                Comment = r.Comment,
                CreatedAt = r.CreatedAt
            })
            .ToListAsync(ct);
    }

    public async Task<List<ReviewDto>> GetReviewsForSellerProductAsync(int productId, CancellationToken ct = default)
    {
        // Identical projection to GetReviewsByProductAsync — the ownership check
        // is handled by ReviewService before this method is called.
        return await _db.Reviews
            .Include(r => r.Buyer)
            .Include(r => r.Product)
            .Where(r => r.ProductId == productId)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new ReviewDto
            {
                Id = r.Id,
                ProductId = r.ProductId,
                ProductName = r.Product != null ? r.Product.Name : "",
                BuyerId = r.BuyerId,
                BuyerEmail = r.Buyer != null ? r.Buyer.Email : "",
                Rating = r.Rating,
                Comment = r.Comment,
                CreatedAt = r.CreatedAt
            })
            .ToListAsync(ct);
    }

    public async Task<List<ReviewDto>> GetAllReviewsBySellerAsync(int sellerId, CancellationToken ct = default)
    {
        return await _db.Reviews
            .Include(r => r.Buyer)
            .Include(r => r.Product)
            .Where(r => r.Product != null && r.Product.SellerId == sellerId)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new ReviewDto
            {
                Id = r.Id,
                ProductId = r.ProductId,
                ProductName = r.Product != null ? r.Product.Name : "",
                BuyerId = r.BuyerId,
                BuyerEmail = r.Buyer != null ? r.Buyer.Email : "",
                Rating = r.Rating,
                Comment = r.Comment,
                CreatedAt = r.CreatedAt
            })
            .ToListAsync(ct);
    }
}
