using System.ComponentModel.DataAnnotations;

namespace E_commerce_Application.Entities;

public enum UserRole
{
    Admin,
    Customer
}

public class User : BaseEntity
{
    [Required]
    [MaxLength(150)]
    public string Name { get; set; } = default!;

    [Required]
    [MaxLength(150)]
    public string Email { get; set; } = default!;

    [Required]
    public string PasswordHash { get; set; } = default!;

    [Required]
    public UserRole Role { get; set; } = UserRole.Customer;

    public ICollection<Order> Orders { get; set; } = new List<Order>();
}

