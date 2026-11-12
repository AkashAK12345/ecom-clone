using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ECommerce.Api.Models;

public class Order
{
    public int Id { get; set; }

    [Required]
    public int ProductId { get; set; }

    public Product? Product { get; set; }

    [Range(1, 100000)]
    public int Quantity { get; set; }

    [Column(TypeName = "numeric")]
    public decimal UnitPrice { get; set; }

    [Column(TypeName = "numeric")]
    public decimal TotalPrice { get; set; }

    public DateTime OrderDate { get; set; } = DateTime.UtcNow;
}

