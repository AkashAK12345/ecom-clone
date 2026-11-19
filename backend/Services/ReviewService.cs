using ECommerce.Api.Models;
using ECommerce.Api.Repositories.Interfaces;
using ECommerce.Api.Services.Interfaces;
using Microsoft.AspNetCore.Http;

namespace ECommerce.Api.Services;

/// <summary>
/// Implements IReviewService — the home for all Reviews business rules.
///
/// Design decisions preserved from the original controller:
///   - BuyerId is passed in by the controller from the JWT claim, never from the DTO.
///   - Comment is trimmed before persisting (client whitespace is not trusted).
///   - CreatedAt is stamped server-side with DateTime.UtcNow.
///   - Duplicate reviews are caught before the DB unique index fires, returning 409 Conflict.
///   - Sellers can only view reviews for products they own; mismatch returns 403 Forbidden.
/// </summary>
public class ReviewService : IReviewService
{
    private readonly IReviewRepository _repo;

    public ReviewService(IReviewRepository repo)
    {
        _repo = repo;
    }

    public async Task<ServiceResult<ReviewDto>> CreateReviewAsync(
        int buyerId, CreateReviewDto dto, CancellationToken ct = default)
    {
        // Business rule: product must exist
        var product = await _repo.FindProductAsync(dto.ProductId, ct);
        if (product == null)
            return ServiceResult<ReviewDto>.NotFound($"Product with ID {dto.ProductId} was not found.");

        // Business rule: one review per buyer per product
        var alreadyReviewed = await _repo.HasBuyerReviewedAsync(dto.ProductId, buyerId, ct);
        if (alreadyReviewed)
            return ServiceResult<ReviewDto>.Fail(StatusCodes.Status409Conflict, "You have already reviewed this product.");

        // Build entity — BuyerId from JWT claim, CreatedAt server-stamped, comment trimmed
        var review = new Review
        {
            ProductId = dto.ProductId,
            BuyerId = buyerId,
            Rating = dto.Rating,
            Comment = dto.Comment.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        await _repo.AddReviewAsync(review, ct);

        // Fetch buyer email for the response DTO (matches original controller behavior)
        var buyerEmail = await _repo.GetBuyerEmailAsync(buyerId, ct);

        var reviewDto = new ReviewDto
        {
            Id = review.Id,
            ProductId = review.ProductId,
            ProductName = product.Name,
            BuyerId = review.BuyerId,
            BuyerEmail = buyerEmail,
            Rating = review.Rating,
            Comment = review.Comment,
            CreatedAt = review.CreatedAt
        };

        return ServiceResult<ReviewDto>.Ok(reviewDto);
    }

    public async Task<ServiceResult<List<ReviewDto>>> GetProductReviewsAsync(
        int productId, CancellationToken ct = default)
    {
        // Business rule: product must exist even for the public endpoint
        var product = await _repo.FindProductAsync(productId, ct);
        if (product == null)
            return ServiceResult<List<ReviewDto>>.NotFound($"Product with ID {productId} was not found.");

        var reviews = await _repo.GetReviewsByProductAsync(productId, ct);
        return ServiceResult<List<ReviewDto>>.Ok(reviews);
    }

    public async Task<ServiceResult<List<ReviewDto>>> GetSellerProductReviewsAsync(
        int productId, int sellerId, CancellationToken ct = default)
    {
        // Business rule: product must exist
        var product = await _repo.FindProductAsync(productId, ct);
        if (product == null)
            return ServiceResult<List<ReviewDto>>.NotFound($"Product with ID {productId} was not found.");

        // Business rule: seller can only view reviews for their own products
        if (product.SellerId != sellerId)
            return ServiceResult<List<ReviewDto>>.Forbidden(
                "You do not have permission to view reviews for this product.");

        var reviews = await _repo.GetReviewsForSellerProductAsync(productId, ct);
        return ServiceResult<List<ReviewDto>>.Ok(reviews);
    }

    public async Task<ServiceResult<List<ReviewDto>>> GetAllSellerReviewsAsync(
        int sellerId, CancellationToken ct = default)
    {
        var reviews = await _repo.GetAllReviewsBySellerAsync(sellerId, ct);
        return ServiceResult<List<ReviewDto>>.Ok(reviews);
    }
}
