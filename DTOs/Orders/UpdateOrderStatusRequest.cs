using System.ComponentModel.DataAnnotations;

namespace E_commerce_Application.DTOs.Orders;

public class UpdateOrderStatusRequest
{
    [Required]
    public string Status { get; set; } = default!;
}

