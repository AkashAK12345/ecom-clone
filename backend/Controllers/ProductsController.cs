using ECommerce.Api.Models;
using ECommerce.Api.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace ECommerce.Api.Controllers;

/// <summary>
/// Thin HTTP layer for the Products domain.
/// Responsibilities: receive requests, read JWT claims, call IProductService, return HTTP responses.
/// No business logic, no database access, no file I/O here.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly IProductService _productService;

    public ProductsController(IProductService productService)
    {
        _productService = productService;
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
    // GET /api/products — public buyer catalog
    // -------------------------------------------------------------------------

    [HttpGet]
    public async Task<IActionResult> GetProducts(CancellationToken ct)
    {
        var result = await _productService.GetAllProductsAsync(ct);
        return Ok(result.Value);
    }

    // -------------------------------------------------------------------------
    // GET /api/products/seller — seller's own products
    // -------------------------------------------------------------------------

    [HttpGet("seller")]
    [Authorize(Roles = "Seller")]
    public async Task<IActionResult> GetSellerProducts(CancellationToken ct)
    {
        var sellerId = GetCurrentUserId();
        if (sellerId == null)
            return Unauthorized("Invalid seller token.");

        var result = await _productService.GetSellerProductsAsync(sellerId.Value, ct);
        return Ok(result.Value);
    }

    // -------------------------------------------------------------------------
    // GET /api/products/category/{category} — public category filter
    // -------------------------------------------------------------------------

    [HttpGet("category/{category}")]
    public async Task<IActionResult> GetByCategory(string category, CancellationToken ct)
    {
        var result = await _productService.GetProductsByCategoryAsync(category, ct);
        return Ok(result.Value);
    }

    // -------------------------------------------------------------------------
    // POST /api/products — add a new product (seller only)
    // -------------------------------------------------------------------------

    [HttpPost]
    [Consumes("multipart/form-data")]
    [Authorize(Roles = "Seller")]
    public async Task<IActionResult> AddProduct([FromForm] CreateProductDto dto, CancellationToken ct)
    {
        var sellerId = GetCurrentUserId();
        if (sellerId == null)
            return Unauthorized("Invalid seller token.");

        var result = await _productService.AddProductAsync(sellerId.Value, dto, ct);

        if (!result.IsSuccess)
            return StatusCode(result.StatusCode, result.Error);

        return CreatedAtAction(nameof(GetProducts), new { id = result.Value!.Id }, result.Value);
    }

    // -------------------------------------------------------------------------
    // PUT /api/products/{id} — update an existing product (seller only)
    // -------------------------------------------------------------------------

    [HttpPut("{id}")]
    [Consumes("multipart/form-data")]
    [Authorize(Roles = "Seller")]
    public async Task<IActionResult> UpdateProduct(int id, [FromForm] UpdateProductDto dto, CancellationToken ct)
    {
        var sellerId = GetCurrentUserId();
        if (sellerId == null)
            return Unauthorized("Invalid seller token.");

        var result = await _productService.UpdateProductAsync(id, sellerId.Value, dto, ct);

        if (!result.IsSuccess)
            return StatusCode(result.StatusCode, result.Error);

        return Ok(result.Value);
    }
}
