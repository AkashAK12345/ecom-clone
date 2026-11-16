using System.ComponentModel.DataAnnotations;

namespace ECommerce.Api.Models;

public class ProductSizeDto
{
    [Required(ErrorMessage = "Size is required.")]
    [StringLength(20, ErrorMessage = "Size cannot exceed 20 characters.")]
    public string Size { get; set; } = "";

    [Range(0, 100000, ErrorMessage = "Quantity must be between 0 and 100,000.")]
    public int Quantity { get; set; }
}

