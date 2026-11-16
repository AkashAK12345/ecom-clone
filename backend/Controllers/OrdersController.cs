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
public class OrdersController : ControllerBase
{
    private readonly AppDbContext _db;

    public OrdersController(AppDbContext db)
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

    [HttpPost]
    [Authorize(Roles = "Buyer")]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderDto dto)
    {
        var buyerId = GetCurrentUserId();
        if (buyerId == null)
        {
            return Unauthorized("Invalid buyer token.");
        }

        if (dto.Quantity <= 0)
        {
            return BadRequest("Order quantity must be at least 1.");
        }

        await using var transaction = await _db.Database.BeginTransactionAsync();
        try
        {
            var product = await _db.Products.FindAsync(dto.ProductId);
            if (product == null)
            {
                return NotFound($"Product with ID {dto.ProductId} was not found.");
            }

            if (product.Quantity <= 0)
            {
                return BadRequest($"Product '{product.Name}' is out of stock.");
            }

            if (dto.Quantity > product.Quantity)
            {
                return BadRequest($"Requested quantity ({dto.Quantity}) exceeds available stock ({product.Quantity}).");
            }

            // Deduct stock
            product.Quantity -= dto.Quantity;

            // Save order using price directly from database
            var order = new Order
            {
                ProductId = product.Id,
                BuyerId = buyerId.Value,
                Quantity = dto.Quantity,
                UnitPrice = product.Price,
                TotalPrice = product.Price * dto.Quantity,
                OrderDate = DateTime.UtcNow
            };

            _db.Orders.Add(order);

            await _db.SaveChangesAsync();
            await transaction.CommitAsync();

            return Ok(new
            {
                orderId = order.Id,
                productId = order.ProductId,
                productName = product.Name,
                quantity = order.Quantity,
                unitPrice = order.UnitPrice,
                totalPrice = order.TotalPrice,
                remainingStock = product.Quantity,
                message = $"Order placed successfully for {order.Quantity} x {product.Name} (Total: ₹{order.TotalPrice})."
            });
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            return StatusCode(500, "An error occurred while placing the order.");
        }
    }

    [HttpGet("buyer")]
    [Authorize(Roles = "Buyer")]
    public async Task<IActionResult> GetBuyerOrders()
    {
        var buyerId = GetCurrentUserId();
        if (buyerId == null)
        {
            return Unauthorized("Invalid buyer token.");
        }

        var orders = await _db.Orders
            .Include(o => o.Product)
            .Where(o => o.BuyerId == buyerId.Value)
            .OrderByDescending(o => o.OrderDate)
            .Select(o => new OrderHistoryDto
            {
                Id = o.Id,
                ProductId = o.ProductId,
                ProductName = o.Product != null ? o.Product.Name : "Product",
                Quantity = o.Quantity,
                UnitPrice = o.UnitPrice,
                TotalPrice = o.TotalPrice,
                OrderDate = o.OrderDate
            })
            .ToListAsync();

        return Ok(orders);
    }

    [HttpGet("seller")]
    [Authorize(Roles = "Seller")]
    public async Task<IActionResult> GetSellerOrders()
    {
        var sellerId = GetCurrentUserId();
        if (sellerId == null)
        {
            return Unauthorized("Invalid seller token.");
        }

        var orders = await _db.Orders
            .Include(o => o.Product)
            .Where(o => o.Product != null && o.Product.SellerId == sellerId.Value)
            .OrderByDescending(o => o.OrderDate)
            .Select(o => new OrderHistoryDto
            {
                Id = o.Id,
                ProductId = o.ProductId,
                ProductName = o.Product != null ? o.Product.Name : "Product",
                Quantity = o.Quantity,
                UnitPrice = o.UnitPrice,
                TotalPrice = o.TotalPrice,
                OrderDate = o.OrderDate
            })
            .ToListAsync();

        return Ok(orders);
    }
}

