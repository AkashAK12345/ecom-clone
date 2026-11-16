using System.ComponentModel.DataAnnotations;

namespace ECommerce.Api.Models;

public class CreateOrderDto
{
    [Required(ErrorMessage = "ProductId is required.")]
    public int ProductId { get; set; }

    [Required(ErrorMessage = "Size is required.")]
    [StringLength(20, ErrorMessage = "Size cannot exceed 20 characters.")]
    public string Size { get; set; } = "";

    [Range(1, 100000, ErrorMessage = "Order quantity must be at least 1.")]
    public int Quantity { get; set; }
}

