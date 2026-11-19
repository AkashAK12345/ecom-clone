using ECommerce.Api.Models;

namespace ECommerce.Api.Services.Interfaces;

/// <summary>
/// Defines all business operations for the Reviews domain.
/// Business rules (product existence, duplicate prevention, ownership checks) live here.
/// </summary>
public interface IReviewService
{
    /// <summary>
    /// Creates a review from the authenticated buyer.
    /// Rules enforced:
    ///   - Product must exist → 404.
    ///   - Buyer may not review the same product twice → 409 Conflict.
    ///   - BuyerId is taken from the JWT claim; the request body cannot supply it.
    ///   - Comment is trimmed server-side.
    ///   - CreatedAt is stamped server-side.
    /// Returns the saved ReviewDto on success.
    /// </summary>
    Task<ServiceResult<ReviewDto>> CreateReviewAsync(int buyerId, CreateReviewDto dto, CancellationToken ct = default);

    /// <summary>
    /// Returns all reviews for a product. Public — no authentication required.
    /// Product must exist → 404 if not.
    /// </summary>
    Task<ServiceResult<List<ReviewDto>>> GetProductReviewsAsync(int productId, CancellationToken ct = default);

    /// <summary>
    /// Returns reviews for a specific product, scoped to the authenticated seller.
    /// Rules enforced:
    ///   - Product must exist → 404.
    ///   - Seller must own the product → 403 if not.
    /// </summary>
    Task<ServiceResult<List<ReviewDto>>> GetSellerProductReviewsAsync(int productId, int sellerId, CancellationToken ct = default);

    /// <summary>
    /// Returns all reviews across every product owned by the authenticated seller.
    /// </summary>
    Task<ServiceResult<List<ReviewDto>>> GetAllSellerReviewsAsync(int sellerId, CancellationToken ct = default);
}
