using System.ComponentModel.DataAnnotations;

namespace ECommerce.Api.Models;

public class User
{
    public int Id { get; set; }

    [Required]
    [StringLength(200)]
    public string Email { get; set; } = "";

    [Required]
    public string PasswordHash { get; set; } = "";

    [Required]
    [StringLength(20)]
    public string Role { get; set; } = ""; // "Buyer" or "Seller"

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

