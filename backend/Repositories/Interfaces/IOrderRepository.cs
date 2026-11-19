using ECommerce.Api.Models;

namespace ECommerce.Api.Repositories.Interfaces;

/// <summary>
/// Encapsulates all database operations for the Orders table.
/// Transaction management lives here because the create-order operation must
/// atomically update stock AND insert the order row; a half-saved state is
/// worse than a failure.
/// No business rules here — only EF Core queries, inserts, and saves.
/// </summary>
public interface IOrderRepository
{
    /// <summary>
    /// Fetches the product and its size variants inside an open DB transaction
    /// so that the stock check and stock deduction see a consistent snapshot.
    /// Returns null if the product does not exist.
    /// </summary>
    Task<Product?> GetProductWithSizesAsync(int productId, CancellationToken ct = default);

    /// <summary>
    /// Atomically:
    ///   1. Persists the mutated stock on sizeOption and product (SaveChanges covers these tracked entities).
    ///   2. Inserts the new Order row.
    ///   3. Commits the transaction.
    /// Rolls back and rethrows if anything fails so the caller can return 500.
    /// </summary>
    Task CreateOrderInTransactionAsync(Order order, CancellationToken ct = default);

    /// <summary>
    /// Returns all orders for the given buyer, ordered newest-first,
    /// projected to OrderHistoryDto to avoid exposing raw entities.
    /// </summary>
    Task<List<OrderHistoryDto>> GetBuyerOrdersAsync(int buyerId, CancellationToken ct = default);

    /// <summary>
    /// Returns all orders whose product belongs to the given seller, ordered newest-first,
    /// projected to OrderHistoryDto. Buyer personal data is intentionally excluded.
    /// </summary>
    Task<List<OrderHistoryDto>> GetSellerOrdersAsync(int sellerId, CancellationToken ct = default);
}
