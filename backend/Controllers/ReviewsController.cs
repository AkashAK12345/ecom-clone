using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using ECommerce.Api.Data;
using ECommerce.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReviewsController : ControllerBase
{
    private readonly AppDbContext _db;

    public ReviewsController(AppDbContext db)
    {
        _db = db;
    }

    private int? GetCurrentUserId()
    {
        var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                   ?? User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                   ?? User.FindFirst("sub")?.Value;

        if (int.TryParse(idClaim, out var userId))
        {
            return userId;
        }

        return null;
    }

    // POST /api/reviews - Only authenticated buyers can submit reviews
    [HttpPost]
    [Authorize(Roles = "Buyer")]
    public async Task<IActionResult> CreateReview([FromBody] CreateReviewDto dto)
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

        var product = await _db.Products.FindAsync(dto.ProductId);
        if (product == null)
        {
            return NotFound($"Product with ID {dto.ProductId} was not found.");
        }

        var alreadyReviewed = await _db.Reviews
            .AnyAsync(r => r.ProductId == dto.ProductId && r.BuyerId == buyerId.Value);

        if (alreadyReviewed)
        {
            return Conflict("You have already reviewed this product.");
        }

        var review = new Review
        {
            ProductId = dto.ProductId,
            BuyerId = buyerId.Value,
            Rating = dto.Rating,
            Comment = dto.Comment.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        _db.Reviews.Add(review);
        await _db.SaveChangesAsync();

        var buyer = await _db.Users.FindAsync(buyerId.Value);

        return Ok(new ReviewDto
        {
            Id = review.Id,
            ProductId = review.ProductId,
            ProductName = product.Name,
            BuyerId = review.BuyerId,
            BuyerEmail = buyer?.Email ?? "",
            Rating = review.Rating,
            Comment = review.Comment,
            CreatedAt = review.CreatedAt
        });
    }

    // GET /api/reviews/product/{productId} - Public: buyers can view reviews
    [HttpGet("product/{productId}")]
    public async Task<IActionResult> GetProductReviews(int productId)
    {
        var product = await _db.Products.FindAsync(productId);
        if (product == null)
        {
            return NotFound($"Product with ID {productId} was not found.");
        }

        var reviews = await _db.Reviews
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
            .ToListAsync();

        return Ok(reviews);
    }

    // GET /api/reviews/seller/{productId} - Sellers can view reviews only for their own products
    [HttpGet("seller/{productId}")]
    [Authorize(Roles = "Seller")]
    public async Task<IActionResult> GetSellerProductReviews(int productId)
    {
        var sellerId = GetCurrentUserId();
        if (sellerId == null)
        {
            return Unauthorized("Invalid seller token.");
        }

        var product = await _db.Products.FindAsync(productId);
        if (product == null)
        {
            return NotFound($"Product with ID {productId} was not found.");
        }

        // Enforce ownership check in the API
        if (product.SellerId != sellerId.Value)
        {
            return StatusCode(StatusCodes.Status403Forbidden, "You do not have permission to view reviews for this product.");
        }

        var reviews = await _db.Reviews
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
            .ToListAsync();

        return Ok(reviews);
    }

    // GET /api/reviews/seller - Sellers view reviews across all their own products
    [HttpGet("seller")]
    [Authorize(Roles = "Seller")]
    public async Task<IActionResult> GetAllSellerReviews()
    {
        var sellerId = GetCurrentUserId();
        if (sellerId == null)
        {
            return Unauthorized("Invalid seller token.");
        }

        var reviews = await _db.Reviews
            .Include(r => r.Buyer)
            .Include(r => r.Product)
            .Where(r => r.Product != null && r.Product.SellerId == sellerId.Value)
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
            .ToListAsync();

        return Ok(reviews);
    }
}

