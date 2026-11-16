using System.ComponentModel.DataAnnotations;

namespace ECommerce.Api.Models;

public class Review
{
    public int Id { get; set; }

    [Required]
    public int ProductId { get; set; }

    public Product? Product { get; set; }

    [Required]
    public int BuyerId { get; set; }

    public User? Buyer { get; set; }

    [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5.")]
    public int Rating { get; set; }

    [Required(ErrorMessage = "Comment is required.")]
    [StringLength(1000, MinimumLength = 1, ErrorMessage = "Comment cannot exceed 1000 characters.")]
    public string Comment { get; set; } = "";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

