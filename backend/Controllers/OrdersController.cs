using ECommerce.Api.Models;
using ECommerce.Api.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace ECommerce.Api.Controllers;

/// <summary>
/// Thin HTTP layer for the Orders domain.
/// Responsibilities: receive requests, read JWT claims, call IOrderService, return HTTP responses.
/// No business logic, no database access, no stock math here.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _orderService;

    public OrdersController(IOrderService orderService)
    {
        _orderService = orderService;
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
    // POST /api/orders — buyer places an order
    // -------------------------------------------------------------------------

    [HttpPost]
    [Authorize(Roles = "Buyer")]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderDto dto, CancellationToken ct)
    {
        var buyerId = GetCurrentUserId();
        if (buyerId == null)
            return Unauthorized("Invalid buyer token.");

        var result = await _orderService.CreateOrderAsync(buyerId.Value, dto, ct);

        if (!result.IsSuccess)
            return StatusCode(result.StatusCode, result.Error);

        // Preserve the exact anonymous-object response shape the Angular frontend expects
        var r = result.Value!;
        return Ok(new
        {
            orderId = r.OrderId,
            productId = r.ProductId,
            productName = r.ProductName,
            size = r.Size,
            quantity = r.Quantity,
            unitPrice = r.UnitPrice,
            totalPrice = r.TotalPrice,
            remainingStock = r.RemainingStock,
            remainingTotalStock = r.RemainingTotalStock,
            message = r.Message
        });
    }

    // -------------------------------------------------------------------------
    // GET /api/orders/buyer — buyer's own order history
    // -------------------------------------------------------------------------

    [HttpGet("buyer")]
    [Authorize(Roles = "Buyer")]
    public async Task<IActionResult> GetBuyerOrders(CancellationToken ct)
    {
        var buyerId = GetCurrentUserId();
        if (buyerId == null)
            return Unauthorized("Invalid buyer token.");

        var result = await _orderService.GetBuyerOrdersAsync(buyerId.Value, ct);
        return Ok(result.Value);
    }

    // -------------------------------------------------------------------------
    // GET /api/orders/seller — seller's order history (only their products)
    // -------------------------------------------------------------------------

    [HttpGet("seller")]
    [Authorize(Roles = "Seller")]
    public async Task<IActionResult> GetSellerOrders(CancellationToken ct)
    {
        var sellerId = GetCurrentUserId();
        if (sellerId == null)
            return Unauthorized("Invalid seller token.");

        var result = await _orderService.GetSellerOrdersAsync(sellerId.Value, ct);
        return Ok(result.Value);
    }
}
