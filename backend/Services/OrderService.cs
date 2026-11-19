using ECommerce.Api.Models;
using ECommerce.Api.Repositories.Interfaces;
using ECommerce.Api.Services.Interfaces;
using Microsoft.AspNetCore.Http;

namespace ECommerce.Api.Services;

/// <summary>
/// Implements IOrderService — the home for all order business rules.
///
/// Design decisions preserved from the original controller:
///   - Price is always sourced from the database, never trusted from the request body.
///   - BuyerId is passed in by the controller from the JWT claim, never from the DTO.
///   - Stock deduction and order insert are wrapped in a DB transaction (owned by the repository).
///   - Aggregate product stock (Product.Quantity) is kept in sync after each size deduction.
///   - OrderDate is stamped server-side with DateTime.UtcNow.
/// </summary>
public class OrderService : IOrderService
{
    private readonly IOrderRepository _repo;

    public OrderService(IOrderRepository repo)
    {
        _repo = repo;
    }

    public async Task<ServiceResult<CreateOrderResult>> CreateOrderAsync(
        int buyerId, CreateOrderDto dto, CancellationToken ct = default)
    {
        // Business rule: basic input guards (DTO [Range] covers these at model binding time
        // but the original controller had explicit checks too — keep them as a safety net)
        if (dto.Quantity <= 0)
            return ServiceResult<CreateOrderResult>.BadRequest("Order quantity must be at least 1.");

        if (string.IsNullOrWhiteSpace(dto.Size))
            return ServiceResult<CreateOrderResult>.BadRequest("A size must be selected to place an order.");

        // Repository: fetch product and its tracked size entities
        var product = await _repo.GetProductWithSizesAsync(dto.ProductId, ct);
        if (product == null)
            return ServiceResult<CreateOrderResult>.NotFound($"Product with ID {dto.ProductId} was not found.");

        // Business rule: the requested size must exist on this product
        var sizeOption = product.Sizes
            .FirstOrDefault(s => string.Equals(s.Size, dto.Size.Trim(), StringComparison.OrdinalIgnoreCase));

        if (sizeOption == null)
            return ServiceResult<CreateOrderResult>.BadRequest(
                $"Size '{dto.Size}' is not available for product '{product.Name}'.");

        // Business rule: size must have stock
        if (sizeOption.Quantity <= 0)
            return ServiceResult<CreateOrderResult>.BadRequest(
                $"Product '{product.Name}' in size '{sizeOption.Size}' is out of stock.");

        // Business rule: cannot order more than what is in stock
        if (dto.Quantity > sizeOption.Quantity)
            return ServiceResult<CreateOrderResult>.BadRequest(
                $"Requested quantity ({dto.Quantity}) exceeds available stock ({sizeOption.Quantity}) for size '{sizeOption.Size}'.");

        // Business rule: deduct size-specific stock and sync aggregate product quantity
        // These mutations are on EF-tracked entities, so they are included in the
        // next SaveChangesAsync() call inside CreateOrderInTransactionAsync.
        sizeOption.Quantity -= dto.Quantity;
        product.Quantity = product.Sizes.Sum(s => s.Quantity);

        // Business rule: price is taken from the database, not the request body
        var order = new Order
        {
            ProductId = product.Id,
            BuyerId = buyerId,                       // from validated JWT claim
            Size = sizeOption.Size,
            Quantity = dto.Quantity,
            UnitPrice = product.Price,               // server-authoritative
            TotalPrice = product.Price * dto.Quantity,
            OrderDate = DateTime.UtcNow              // server-stamped
        };

        try
        {
            await _repo.CreateOrderInTransactionAsync(order, ct);
        }
        catch
        {
            return ServiceResult<CreateOrderResult>.Fail(
                StatusCodes.Status500InternalServerError,
                "An error occurred while placing the order.");
        }

        var result = new CreateOrderResult(
            OrderId: order.Id,
            ProductId: order.ProductId,
            ProductName: product.Name,
            Size: order.Size!,
            Quantity: order.Quantity,
            UnitPrice: order.UnitPrice,
            TotalPrice: order.TotalPrice,
            RemainingStock: sizeOption.Quantity,
            RemainingTotalStock: product.Quantity,
            Message: $"Order placed successfully for {order.Quantity} x {product.Name} (Size: {order.Size}, Total: ₹{order.TotalPrice})."
        );

        return ServiceResult<CreateOrderResult>.Ok(result);
    }

    public async Task<ServiceResult<List<OrderHistoryDto>>> GetBuyerOrdersAsync(
        int buyerId, CancellationToken ct = default)
    {
        var orders = await _repo.GetBuyerOrdersAsync(buyerId, ct);
        return ServiceResult<List<OrderHistoryDto>>.Ok(orders);
    }

    public async Task<ServiceResult<List<OrderHistoryDto>>> GetSellerOrdersAsync(
        int sellerId, CancellationToken ct = default)
    {
        var orders = await _repo.GetSellerOrdersAsync(sellerId, ct);
        return ServiceResult<List<OrderHistoryDto>>.Ok(orders);
    }
}
