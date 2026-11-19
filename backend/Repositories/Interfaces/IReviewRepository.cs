using ECommerce.Api.Models;

namespace ECommerce.Api.Repositories.Interfaces;

/// <summary>
/// Encapsulates all database operations for the Reviews table.
/// No business rules here — only EF Core queries, inserts, and saves.
/// </summary>
public interface IReviewRepository
{
    /// <summary>
    /// Returns the product with the given ID, or null if it does not exist.
    /// Used to validate product existence before creating or reading reviews.
    /// </summary>
    Task<Product?> FindProductAsync(int productId, CancellationToken ct = default);

    /// <summary>
    /// Returns true if the buyer has already submitted a review for this product.
    /// Used to enforce the one-review-per-buyer-per-product business rule.
    /// </summary>
    Task<bool> HasBuyerReviewedAsync(int productId, int buyerId, CancellationToken ct = default);

    /// <summary>
    /// Returns the buyer's email by user ID.
    /// Used to populate BuyerEmail in the CreateReview response DTO.
    /// </summary>
    Task<string> GetBuyerEmailAsync(int buyerId, CancellationToken ct = default);

    /// <summary>Inserts a new Review and saves.</summary>
    Task AddReviewAsync(Review review, CancellationToken ct = default);

    /// <summary>
    /// Returns all reviews for the given product, ordered newest-first,
    /// projected to ReviewDto. No auth requirement — public endpoint.
    /// </summary>
    Task<List<ReviewDto>> GetReviewsByProductAsync(int productId, CancellationToken ct = default);

    /// <summary>
    /// Returns all reviews for the given product, ordered newest-first,
    /// projected to ReviewDto. Caller is responsible for the ownership check
    /// before invoking this method.
    /// </summary>
    Task<List<ReviewDto>> GetReviewsForSellerProductAsync(int productId, CancellationToken ct = default);

    /// <summary>
    /// Returns all reviews across every product owned by the given seller,
    /// ordered newest-first, projected to ReviewDto.
    /// </summary>
    Task<List<ReviewDto>> GetAllReviewsBySellerAsync(int sellerId, CancellationToken ct = default);
}
