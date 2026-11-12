using System.ComponentModel.DataAnnotations;

namespace ECommerce.Api.Models;

public class RegisterDto
{
    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Invalid email address.")]
    [StringLength(200, ErrorMessage = "Email cannot exceed 200 characters.")]
    public string Email { get; set; } = "";

    [Required(ErrorMessage = "Password is required.")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be between 6 and 100 characters.")]
    public string Password { get; set; } = "";

    [Required(ErrorMessage = "Role is required.")]
    [RegularExpression("^(Buyer|Seller)$", ErrorMessage = "Role must be 'Buyer' or 'Seller'.")]
    public string Role { get; set; } = "";
}

