using ECommerce.Api.Data;
using ECommerce.Api.Models;
using ECommerce.Api.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Api.Repositories;

/// <summary>
/// Implements IOrderRepository using EF Core + AppDbContext.
/// Owns the DB transaction for order creation: stock mutation and order insert
/// must succeed or fail together — a committed order with no stock deduction, or
/// a stock deduction with no order row, would both corrupt the system state.
/// </summary>
public class OrderRepository : IOrderRepository
{
    private readonly AppDbContext _db;

    public OrderRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<Product?> GetProductWithSizesAsync(int productId, CancellationToken ct = default)
    {
        return await _db.Products
            .Include(p => p.Sizes)
            .FirstOrDefaultAsync(p => p.Id == productId, ct);
    }

    public async Task CreateOrderInTransactionAsync(Order order, CancellationToken ct = default)
    {
        // The Product and its Sizes are already tracked by EF Core (fetched in GetProductWithSizesAsync),
        // so SaveChangesAsync() will persist the stock mutation AND the new Order row in one round-trip.
        await using var transaction = await _db.Database.BeginTransactionAsync(ct);
        try
        {
            _db.Orders.Add(order);
            await _db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
        }
        catch
        {
            await transaction.RollbackAsync(ct);
            throw; // Re-throw so OrderService can catch and return a 500 ServiceResult
        }
    }

    public async Task<List<OrderHistoryDto>> GetBuyerOrdersAsync(int buyerId, CancellationToken ct = default)
    {
        return await _db.Orders
            .Include(o => o.Product)
            .Where(o => o.BuyerId == buyerId)
            .OrderByDescending(o => o.OrderDate)
            .Select(o => new OrderHistoryDto
            {
                Id = o.Id,
                ProductId = o.ProductId,
                ProductName = o.Product != null ? o.Product.Name : "Product",
                Size = o.Size ?? "One Size",
                Quantity = o.Quantity,
                UnitPrice = o.UnitPrice,
                TotalPrice = o.TotalPrice,
                OrderDate = o.OrderDate
            })
            .ToListAsync(ct);
    }

    public async Task<List<OrderHistoryDto>> GetSellerOrdersAsync(int sellerId, CancellationToken ct = default)
    {
        return await _db.Orders
            .Include(o => o.Product)
            .Where(o => o.Product != null && o.Product.SellerId == sellerId)
            .OrderByDescending(o => o.OrderDate)
            .Select(o => new OrderHistoryDto
            {
                Id = o.Id,
                ProductId = o.ProductId,
                ProductName = o.Product != null ? o.Product.Name : "Product",
                Size = o.Size ?? "One Size",
                Quantity = o.Quantity,
                UnitPrice = o.UnitPrice,
                TotalPrice = o.TotalPrice,
                OrderDate = o.OrderDate
            })
            .ToListAsync(ct);
    }
}
