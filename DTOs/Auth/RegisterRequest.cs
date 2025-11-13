using System.ComponentModel.DataAnnotations;

namespace E_commerce_Application.DTOs.Auth;

public class RegisterRequest
{
    [Required]
    [MaxLength(150)]
    public string Name { get; set; } = default!;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = default!;

    [Required]
    [MinLength(8)]
    [RegularExpression(@"^(?=.*[A-Za-z])(?=.*\d)[A-Za-z\d@$!%*?&]{8,}$",
        ErrorMessage = "Password must be at least 8 characters long and contain both letters and numbers.")]
    public string Password { get; set; } = default!;
}

