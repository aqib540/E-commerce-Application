using System.ComponentModel.DataAnnotations;

namespace E_commerce_Application.DTOs.Customers;

public class UpdateCustomerProfileRequest
{
    [Required]
    [MaxLength(150)]
    public string Name { get; set; } = default!;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = default!;
}

