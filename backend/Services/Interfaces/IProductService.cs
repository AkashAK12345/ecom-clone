using ECommerce.Api.Models;
using Microsoft.AspNetCore.Http;

namespace ECommerce.Api.Services.Interfaces;

/// <summary>
/// Carries a typed result from a service call back to the controller.
/// The controller reads IsSuccess, StatusCode, and Error to decide the HTTP response.
/// </summary>
public class ServiceResult<T>
{
    public bool IsSuccess { get; private init; }
    public T? Value { get; private init; }
    public string? Error { get; private init; }
    public int StatusCode { get; private init; }

    public static ServiceResult<T> Ok(T value) =>
        new() { IsSuccess = true, Value = value, StatusCode = StatusCodes.Status200OK };

    public static ServiceResult<T> Created(T value) =>
        new() { IsSuccess = true, Value = value, StatusCode = StatusCodes.Status201Created };

    public static ServiceResult<T> Fail(int statusCode, string error) =>
        new() { IsSuccess = false, Error = error, StatusCode = statusCode };

    public static ServiceResult<T> NotFound(string error) =>
        Fail(StatusCodes.Status404NotFound, error);

    public static ServiceResult<T> BadRequest(string error) =>
        Fail(StatusCodes.Status400BadRequest, error);

    public static ServiceResult<T> Forbidden(string error) =>
        Fail(StatusCodes.Status403Forbidden, error);
}

/// <summary>
/// Defines all business operations for the Products domain.
/// Business rules (image validation, size parsing, ownership checks, stock sync) live here.
/// </summary>
public interface IProductService
{
    /// <summary>Returns all products (public buyer catalog).</summary>
    Task<ServiceResult<List<Product>>> GetAllProductsAsync(CancellationToken ct = default);

    /// <summary>Returns products belonging to the authenticated seller.</summary>
    Task<ServiceResult<List<Product>>> GetSellerProductsAsync(int sellerId, CancellationToken ct = default);

    /// <summary>Returns products filtered by category.</summary>
    Task<ServiceResult<List<Product>>> GetProductsByCategoryAsync(string category, CancellationToken ct = default);

    /// <summary>
    /// Creates a new product owned by the authenticated seller.
    /// Validates the image file, parses and validates size variants, saves the image to disk, and persists the product.
    /// </summary>
    Task<ServiceResult<Product>> AddProductAsync(int sellerId, CreateProductDto dto, CancellationToken ct = default);

    /// <summary>
    /// Updates an existing product.
    /// Verifies seller ownership (403 if mismatch), replaces image if provided,
    /// reconciles size variants (delete removed, update existing, insert new), and syncs aggregate stock.
    /// </summary>
    Task<ServiceResult<Product>> UpdateProductAsync(int id, int sellerId, UpdateProductDto dto, CancellationToken ct = default);
}
