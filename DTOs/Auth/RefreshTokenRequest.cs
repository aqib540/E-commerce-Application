using System.ComponentModel.DataAnnotations;

namespace E_commerce_Application.DTOs.Auth;

public class RefreshTokenRequest
{
    [Required]
    public string RefreshToken { get; set; } = default!;
}

