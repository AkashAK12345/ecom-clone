using System.ComponentModel.DataAnnotations;

namespace ECommerce.Api.Models;

public class CreateReviewDto
{
    [Required]
    public int ProductId { get; set; }

    [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5.")]
    public int Rating { get; set; }

    [Required(ErrorMessage = "Comment is required.")]
    [StringLength(1000, MinimumLength = 1, ErrorMessage = "Comment must be between 1 and 1000 characters.")]
    public string Comment { get; set; } = "";
}

