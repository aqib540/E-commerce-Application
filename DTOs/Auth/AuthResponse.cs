namespace E_commerce_Application.DTOs.Auth;

public class AuthResponse
{
    public string AccessToken { get; set; } = default!;
    public DateTime ExpiresAt { get; set; }
    public string RefreshToken { get; set; } = default!;
    public DateTime RefreshTokenExpiresAt { get; set; }
    public string Role { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string Email { get; set; } = default!;
}

