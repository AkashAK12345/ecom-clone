using ECommerce.Api.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ECommerce.Api.Models;

namespace ECommerce.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    
    private readonly AppDbContext _db;
    private readonly IWebHostEnvironment _env;

    public ProductsController(AppDbContext db, IWebHostEnvironment env)
    {
        _db = db;
        _env = env;
    }

    [HttpPost]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> AddProduct([FromForm] CreateProductDto dto)
    {
        if (dto.Image == null || dto.Image.Length == 0)
        {
            return BadRequest("Product image is required.");
        }

        if (dto.Quantity < 0 || dto.Quantity > 100000)
        {
            return BadRequest("Stock quantity must be between 0 and 100,000.");
        }

        const long maxFileSize = 5 * 1024 * 1024; // 5 MB
        if (dto.Image.Length > maxFileSize)
        {
            return BadRequest("Image file size cannot exceed 5 MB.");
        }

        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
        var extension = Path.GetExtension(dto.Image.FileName).ToLowerInvariant();
        if (string.IsNullOrEmpty(extension) || !allowedExtensions.Contains(extension))
        {
            return BadRequest("Invalid image format. Allowed formats are: .jpg, .jpeg, .png, .webp, .gif.");
        }

        if (string.IsNullOrEmpty(dto.Image.ContentType) || !dto.Image.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest("Uploaded file is not a valid image.");
        }

        var webRoot = _env.WebRootPath;
        if (string.IsNullOrEmpty(webRoot))
        {
            webRoot = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
        }

        var uploadsFolder = Path.Combine(webRoot, "uploads", "products");
        if (!Directory.Exists(uploadsFolder))
        {
            Directory.CreateDirectory(uploadsFolder);
        }

        var uniqueFileName = $"{Guid.NewGuid()}{extension}";
        var filePath = Path.Combine(uploadsFolder, uniqueFileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await dto.Image.CopyToAsync(stream);
        }

        var relativeUrl = $"/uploads/products/{uniqueFileName}";

        var product = new Product
        {
            Name = dto.Name,
            Description = dto.Description,
            Category = dto.Category,
            Price = dto.Price,
            Quantity = dto.Quantity,
            ImageUrl = relativeUrl
        };

        _db.Products.Add(product);
        await _db.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetProducts),
            new { id = product.Id },
            product
        );
    }

    [HttpGet]
    public async Task<IActionResult> GetProducts()
    {
        var products = await _db.Products.ToListAsync();
        return Ok(products);
    }

    [HttpPut("{id}")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UpdateProduct(int id, [FromForm] UpdateProductDto dto)
    {
        var product = await _db.Products.FindAsync(id);
        if (product == null)
        {
            return NotFound($"Product with ID {id} was not found.");
        }

        if (dto.Quantity < 0 || dto.Quantity > 100000)
        {
            return BadRequest("Stock quantity must be between 0 and 100,000.");
        }

        if (dto.Image != null && dto.Image.Length > 0)
        {
            const long maxFileSize = 5 * 1024 * 1024; // 5 MB
            if (dto.Image.Length > maxFileSize)
            {
                return BadRequest("Image file size cannot exceed 5 MB.");
            }

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
            var extension = Path.GetExtension(dto.Image.FileName).ToLowerInvariant();
            if (string.IsNullOrEmpty(extension) || !allowedExtensions.Contains(extension))
            {
                return BadRequest("Invalid image format. Allowed formats are: .jpg, .jpeg, .png, .webp, .gif.");
            }

            if (string.IsNullOrEmpty(dto.Image.ContentType) || !dto.Image.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest("Uploaded file is not a valid image.");
            }

            var webRoot = _env.WebRootPath;
            if (string.IsNullOrEmpty(webRoot))
            {
                webRoot = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            }

            var uploadsFolder = Path.Combine(webRoot, "uploads", "products");
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            var uniqueFileName = $"{Guid.NewGuid()}{extension}";
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await dto.Image.CopyToAsync(stream);
            }

            product.ImageUrl = $"/uploads/products/{uniqueFileName}";
        }

        product.Name = dto.Name;
        product.Description = dto.Description;
        product.Category = dto.Category;
        product.Price = dto.Price;
        product.Quantity = dto.Quantity;

        await _db.SaveChangesAsync();

        return Ok(product);
    }
     [HttpGet("category/{category}")]
public async Task<IActionResult> GetByCategory(string category)
{
    var products = await _db.Products
        .Where(product => product.Category == category)
        .ToListAsync();

    return Ok(products);
}
}