using ECommerce.Api.Data;
using ECommerce.Api.Models;
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

    [HttpPost]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderDto dto)
    {
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
}

