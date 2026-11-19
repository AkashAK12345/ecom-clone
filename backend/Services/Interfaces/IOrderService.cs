using ECommerce.Api.Models;

namespace ECommerce.Api.Services.Interfaces;

/// <summary>
/// Carries the data the controller needs to build the CreateOrder success response.
/// Keeping this as a typed record prevents the service from touching IActionResult.
/// </summary>
public record CreateOrderResult(
    int OrderId,
    int ProductId,
    string ProductName,
    string Size,
    int Quantity,
    decimal UnitPrice,
    decimal TotalPrice,
    int RemainingStock,
    int RemainingTotalStock,
    string Message
);

/// <summary>
/// Defines all business operations for the Orders domain.
/// Business rules (stock checks, price sourcing, aggregate sync) live here.
/// </summary>
public interface IOrderService
{
    /// <summary>
    /// Places an order for the authenticated buyer.
    /// Rules enforced:
    ///   - Quantity must be at least 1 and Size must be non-empty.
    ///   - Product must exist.
    ///   - Requested size must exist on the product.
    ///   - That size must have stock > 0.
    ///   - Requested quantity must not exceed available size stock.
    ///   - Unit price and total price are taken from the DB; the buyer cannot supply them.
    ///   - BuyerId is taken from the JWT claim; the request body cannot supply it.
    ///   - Stock deduction and order insert happen in a single DB transaction.
    /// </summary>
    Task<ServiceResult<CreateOrderResult>> CreateOrderAsync(int buyerId, CreateOrderDto dto, CancellationToken ct = default);

    /// <summary>Returns the order history for the authenticated buyer (newest first).</summary>
    Task<ServiceResult<List<OrderHistoryDto>>> GetBuyerOrdersAsync(int buyerId, CancellationToken ct = default);

    /// <summary>
    /// Returns orders for products owned by the authenticated seller (newest first).
    /// Buyer personal data is intentionally excluded from the result.
    /// </summary>
    Task<ServiceResult<List<OrderHistoryDto>>> GetSellerOrdersAsync(int sellerId, CancellationToken ct = default);
}
