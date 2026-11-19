using ECommerce.Api.Models;
using ECommerce.Api.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace ECommerce.Api.Controllers;

/// <summary>
/// Thin HTTP layer for the Reviews domain.
/// Responsibilities: receive requests, read JWT claims, call IReviewService, return HTTP responses.
/// No business logic, no database access here.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ReviewsController : ControllerBase
{
    private readonly IReviewService _reviewService;

    public ReviewsController(IReviewService reviewService)
    {
        _reviewService = reviewService;
    }

    // -------------------------------------------------------------------------
    // Helper: extract authenticated user ID from JWT claims
    // -------------------------------------------------------------------------

    private int? GetCurrentUserId()
    {
        var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                   ?? User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                   ?? User.FindFirst("sub")?.Value;

        return int.TryParse(idClaim, out var id) ? id : null;
    }

    // -------------------------------------------------------------------------
    // POST /api/reviews — authenticated buyers submit a review
    // -------------------------------------------------------------------------

    [HttpPost]
    [Authorize(Roles = "Buyer")]
    public async Task<IActionResult> CreateReview([FromBody] CreateReviewDto dto, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var buyerId = GetCurrentUserId();
        if (buyerId == null)
        {
            return Unauthorized("Invalid buyer token.");
        }

        var result = await _reviewService.CreateReviewAsync(buyerId.Value, dto, ct);

        if (!result.IsSuccess)
        {
            return StatusCode(result.StatusCode, result.Error);
        }

        return Ok(result.Value);
    }

    // -------------------------------------------------------------------------
    // GET /api/reviews/product/{productId} — public: buyers can view reviews
    // -------------------------------------------------------------------------

    [HttpGet("product/{productId}")]
    public async Task<IActionResult> GetProductReviews(int productId, CancellationToken ct)
    {
        var result = await _reviewService.GetProductReviewsAsync(productId, ct);

        if (!result.IsSuccess)
        {
            return StatusCode(result.StatusCode, result.Error);
        }

        return Ok(result.Value);
    }

    // -------------------------------------------------------------------------
    // GET /api/reviews/seller/{productId} — sellers view reviews for their product
    // -------------------------------------------------------------------------

    [HttpGet("seller/{productId}")]
    [Authorize(Roles = "Seller")]
    public async Task<IActionResult> GetSellerProductReviews(int productId, CancellationToken ct)
    {
        var sellerId = GetCurrentUserId();
        if (sellerId == null)
        {
            return Unauthorized("Invalid seller token.");
        }

        var result = await _reviewService.GetSellerProductReviewsAsync(productId, sellerId.Value, ct);

        if (!result.IsSuccess)
        {
            return StatusCode(result.StatusCode, result.Error);
        }

        return Ok(result.Value);
    }

    // -------------------------------------------------------------------------
    // GET /api/reviews/seller — sellers view reviews across all their products
    // -------------------------------------------------------------------------

    [HttpGet("seller")]
    [Authorize(Roles = "Seller")]
    public async Task<IActionResult> GetAllSellerReviews(CancellationToken ct)
    {
        var sellerId = GetCurrentUserId();
        if (sellerId == null)
        {
            return Unauthorized("Invalid seller token.");
        }

        var result = await _reviewService.GetAllSellerReviewsAsync(sellerId.Value, ct);

        return Ok(result.Value);
    }
}
