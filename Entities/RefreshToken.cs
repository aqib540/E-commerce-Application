using System.ComponentModel.DataAnnotations;

namespace E_commerce_Application.Entities;

public class RefreshToken : BaseEntity
{
    [Required]
    public Guid UserId { get; set; }

    public User? User { get; set; }

    [Required]
    public string Token { get; set; } = default!;

    public DateTime Expires { get; set; }

    public bool IsRevoked { get; set; }

    public bool IsExpired => DateTime.UtcNow >= Expires;
}

