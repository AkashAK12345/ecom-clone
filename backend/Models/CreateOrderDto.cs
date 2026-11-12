using System.ComponentModel.DataAnnotations;

namespace ECommerce.Api.Models;

public class CreateOrderDto
{
    [Required(ErrorMessage = "ProductId is required.")]
    public int ProductId { get; set; }

    [Range(1, 100000, ErrorMessage = "Order quantity must be at least 1.")]
    public int Quantity { get; set; }
}

