using ECommerce.Api.Models;

namespace ECommerce.Api.Repositories.Interfaces;

/// <summary>
/// Encapsulates all database operations for the Products and ProductSizes tables.
/// No business logic lives here — only EF Core queries, includes, inserts, updates, and saves.
/// </summary>
public interface IProductRepository
{
    /// <summary>Returns all products with their size variants, ordered by size ID.</summary>
    Task<List<Product>> GetAllAsync(CancellationToken ct = default);

    /// <summary>Returns all products belonging to a specific seller, with size variants.</summary>
    Task<List<Product>> GetBySellerAsync(int sellerId, CancellationToken ct = default);

    /// <summary>Returns all products in a category, with size variants.</summary>
    Task<List<Product>> GetByCategoryAsync(string category, CancellationToken ct = default);

    /// <summary>
    /// Returns a single product by ID, eagerly loading its size variants.
    /// Returns null if not found.
    /// </summary>
    Task<Product?> GetByIdWithSizesAsync(int id, CancellationToken ct = default);

    /// <summary>Adds a new product (including its Sizes collection) and saves.</summary>
    Task AddAsync(Product product, CancellationToken ct = default);

    /// <summary>Saves all pending changes to the database.</summary>
    Task SaveChangesAsync(CancellationToken ct = default);
}
