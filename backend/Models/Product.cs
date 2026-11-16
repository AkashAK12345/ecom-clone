using System.ComponentModel.DataAnnotations;

namespace ECommerce.Api.Models;

public class Product
{
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = "";

    [StringLength(1000)]
    public string? Description { get; set; }

    [Required]
    [StringLength(50)]
    public string Category { get; set; } = "";

    [Range(0.01, 1000000)]
    public decimal Price { get; set; }

    [Range(0, 100000)]
    public int Quantity { get; set; } = 10;

    [StringLength(500)]
    public string? ImageUrl { get; set; }

    public int SellerId { get; set; }

    public User Seller { get; set; }

    public List<ProductSize> Sizes { get; set; } = new();
}