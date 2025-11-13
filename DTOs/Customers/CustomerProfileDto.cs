namespace E_commerce_Application.DTOs.Customers;

public class CustomerProfileDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = default!;
    public string Email { get; set; } = default!;
    public DateTime CreatedDate { get; set; }
}

