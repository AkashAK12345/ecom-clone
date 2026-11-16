using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace ECommerce.Api.Models;

public class ProductSize
{
    public int Id { get; set; }

    [Required]
    public int ProductId { get; set; }

    [JsonIgnore]
    public Product? Product { get; set; }

    [Required]
    [StringLength(20)]
    public string Size { get; set; } = "";

    [Range(0, 100000)]
    public int Quantity { get; set; }
}

