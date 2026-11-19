using System.Text.Json;
using ECommerce.Api.Models;
using ECommerce.Api.Repositories.Interfaces;
using ECommerce.Api.Services.Interfaces;
using Microsoft.AspNetCore.Http;

namespace ECommerce.Api.Services;

/// <summary>
/// Implements IProductService — the home for all Products business logic.
/// Image validation, size parsing/deduplication, ownership checks, and stock synchronization live here.
/// Database reads and writes are delegated to IProductRepository.
/// </summary>
public class ProductService : IProductService
{
    private readonly IProductRepository _repo;
    private readonly IWebHostEnvironment _env;

    // These constants define business rules in one place.
    private const long MaxImageBytes = 5 * 1024 * 1024; // 5 MB
    private static readonly string[] AllowedExtensions = [".jpg", ".jpeg", ".png", ".webp", ".gif"];

    public ProductService(IProductRepository repo, IWebHostEnvironment env)
    {
        _repo = repo;
        _env = env;
    }

    // -------------------------------------------------------------------------
    // READ operations — no business rules, pure delegation to repo
    // -------------------------------------------------------------------------

    public async Task<ServiceResult<List<Product>>> GetAllProductsAsync(CancellationToken ct = default)
    {
        var products = await _repo.GetAllAsync(ct);
        return ServiceResult<List<Product>>.Ok(products);
    }

    public async Task<ServiceResult<List<Product>>> GetSellerProductsAsync(int sellerId, CancellationToken ct = default)
    {
        var products = await _repo.GetBySellerAsync(sellerId, ct);
        return ServiceResult<List<Product>>.Ok(products);
    }

    public async Task<ServiceResult<List<Product>>> GetProductsByCategoryAsync(string category, CancellationToken ct = default)
    {
        var products = await _repo.GetByCategoryAsync(category, ct);
        return ServiceResult<List<Product>>.Ok(products);
    }

    // -------------------------------------------------------------------------
    // CREATE
    // -------------------------------------------------------------------------

    public async Task<ServiceResult<Product>> AddProductAsync(int sellerId, CreateProductDto dto, CancellationToken ct = default)
    {
        // Business rule: image is mandatory
        if (dto.Image == null || dto.Image.Length == 0)
            return ServiceResult<Product>.BadRequest("Product image is required.");

        // Business rule: validate the image file
        var imageValidation = ValidateImageFile(dto.Image);
        if (imageValidation != null)
            return ServiceResult<Product>.BadRequest(imageValidation);

        // Business rule: quantity range
        if (dto.Quantity < 0 || dto.Quantity > 100000)
            return ServiceResult<Product>.BadRequest("Stock quantity must be between 0 and 100,000.");

        // Business rule: parse + clean sizes; apply business defaults
        var parsedSizes = ParseAndCleanSizes(dto.SizesJson, dto.Quantity);
        var totalQuantity = parsedSizes.Sum(s => s.Quantity);

        // Infrastructure: save the image to disk
        var saveResult = await SaveImageAsync(dto.Image, ct);
        if (saveResult.Error != null)
            return ServiceResult<Product>.Fail(StatusCodes.Status500InternalServerError, saveResult.Error);

        // Build the entity — SellerId comes from validated JWT claim, never from request body
        var product = new Product
        {
            Name = dto.Name,
            Description = dto.Description,
            Category = dto.Category,
            Price = dto.Price,
            Quantity = totalQuantity,
            ImageUrl = saveResult.RelativeUrl,
            SellerId = sellerId,
            Sizes = parsedSizes.Select(s => new ProductSize
            {
                Size = s.Size,
                Quantity = s.Quantity
            }).ToList()
        };

        await _repo.AddAsync(product, ct);
        return ServiceResult<Product>.Created(product);
    }

    // -------------------------------------------------------------------------
    // UPDATE
    // -------------------------------------------------------------------------

    public async Task<ServiceResult<Product>> UpdateProductAsync(int id, int sellerId, UpdateProductDto dto, CancellationToken ct = default)
    {
        // Business rule: quantity range
        if (dto.Quantity < 0 || dto.Quantity > 100000)
            return ServiceResult<Product>.BadRequest("Stock quantity must be between 0 and 100,000.");

        // Repository: fetch product with its current sizes
        var product = await _repo.GetByIdWithSizesAsync(id, ct);
        if (product == null)
            return ServiceResult<Product>.NotFound($"Product with ID {id} was not found.");

        // Business rule: ownership check — seller can only edit their own products
        if (product.SellerId != sellerId)
            return ServiceResult<Product>.Forbidden("You do not have permission to edit this product.");

        // Business rule: replace image only if a new one is provided
        if (dto.Image != null && dto.Image.Length > 0)
        {
            var imageValidation = ValidateImageFile(dto.Image);
            if (imageValidation != null)
                return ServiceResult<Product>.BadRequest(imageValidation);

            var saveResult = await SaveImageAsync(dto.Image, ct);
            if (saveResult.Error != null)
                return ServiceResult<Product>.Fail(StatusCodes.Status500InternalServerError, saveResult.Error);

            product.ImageUrl = saveResult.RelativeUrl;
        }

        // Apply scalar field updates
        product.Name = dto.Name;
        product.Description = dto.Description;
        product.Category = dto.Category;
        product.Price = dto.Price;

        // Business rule: reconcile size variants if the seller sent a new sizes payload
        if (!string.IsNullOrWhiteSpace(dto.SizesJson))
        {
            var parsedSizes = ParseAndCleanSizes(dto.SizesJson, dto.Quantity);
            var updatedSizeNames = new HashSet<string>(parsedSizes.Select(s => s.Size), StringComparer.OrdinalIgnoreCase);

            // Delete sizes the seller removed from the table
            product.Sizes.RemoveAll(s => !updatedSizeNames.Contains(s.Size));

            // Update quantities for existing sizes; insert newly added sizes
            foreach (var sizeDto in parsedSizes)
            {
                var existing = product.Sizes.FirstOrDefault(s =>
                    string.Equals(s.Size, sizeDto.Size, StringComparison.OrdinalIgnoreCase));

                if (existing != null)
                {
                    existing.Quantity = sizeDto.Quantity;
                }
                else
                {
                    product.Sizes.Add(new ProductSize
                    {
                        ProductId = product.Id,
                        Size = sizeDto.Size,
                        Quantity = sizeDto.Quantity
                    });
                }
            }

            // Business rule: aggregate stock = sum of all individual size stocks
            product.Quantity = product.Sizes.Sum(s => s.Quantity);
        }
        else
        {
            product.Quantity = dto.Quantity;
        }

        await _repo.SaveChangesAsync(ct);
        return ServiceResult<Product>.Ok(product);
    }

    // -------------------------------------------------------------------------
    // Private helpers — business logic only, no HTTP types
    // -------------------------------------------------------------------------

    /// <summary>
    /// Validates an uploaded image file against business rules.
    /// Returns an error message string if invalid, or null if the file is acceptable.
    /// </summary>
    private static string? ValidateImageFile(IFormFile image)
    {
        if (image.Length > MaxImageBytes)
            return "Image file size cannot exceed 5 MB.";

        var extension = Path.GetExtension(image.FileName).ToLowerInvariant();
        if (string.IsNullOrEmpty(extension) || !AllowedExtensions.Contains(extension))
            return "Invalid image format. Allowed formats are: .jpg, .jpeg, .png, .webp, .gif.";

        if (string.IsNullOrEmpty(image.ContentType) ||
            !image.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            return "Uploaded file is not a valid image.";

        return null;
    }

    /// <summary>
    /// Saves an uploaded image to wwwroot/uploads/products/ with a GUID filename.
    /// Returns the relative URL and any error message.
    /// </summary>
    private async Task<(string? RelativeUrl, string? Error)> SaveImageAsync(IFormFile image, CancellationToken ct)
    {
        var webRoot = _env.WebRootPath;
        if (string.IsNullOrEmpty(webRoot))
            webRoot = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");

        var uploadsFolder = Path.Combine(webRoot, "uploads", "products");
        if (!Directory.Exists(uploadsFolder))
            Directory.CreateDirectory(uploadsFolder);

        var extension = Path.GetExtension(image.FileName).ToLowerInvariant();
        var uniqueFileName = $"{Guid.NewGuid()}{extension}";
        var filePath = Path.Combine(uploadsFolder, uniqueFileName);

        try
        {
            using var stream = new FileStream(filePath, FileMode.Create);
            await image.CopyToAsync(stream, ct);
        }
        catch (Exception ex)
        {
            return (null, $"Failed to save image: {ex.Message}");
        }

        return ($"/uploads/products/{uniqueFileName}", null);
    }

    /// <summary>
    /// Deserializes the sizesJson string, trims whitespace from size names,
    /// removes duplicates case-insensitively, clamps quantities to 0–100,000,
    /// and falls back to a single "One Size" entry if the result is empty.
    /// </summary>
    private static List<ProductSizeDto> ParseAndCleanSizes(string? sizesJson, int fallbackQuantity)
    {
        var raw = new List<ProductSizeDto>();

        if (!string.IsNullOrWhiteSpace(sizesJson))
        {
            try
            {
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var parsed = JsonSerializer.Deserialize<List<ProductSizeDto>>(sizesJson, options);
                if (parsed != null) raw = parsed;
            }
            catch
            {
                // Malformed JSON: fall through to default "One Size"
            }
        }

        var cleaned = new List<ProductSizeDto>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var item in raw)
        {
            var name = item.Size?.Trim();
            if (string.IsNullOrWhiteSpace(name) || !seen.Add(name)) continue;

            cleaned.Add(new ProductSizeDto
            {
                Size = name,
                Quantity = Math.Clamp(item.Quantity, 0, 100000)
            });
        }

        if (cleaned.Count == 0)
        {
            cleaned.Add(new ProductSizeDto
            {
                Size = "One Size",
                Quantity = Math.Clamp(fallbackQuantity, 0, 100000)
            });
        }

        return cleaned;
    }
}
