namespace E_commerce_Application.DTOs.Orders;

public class OrderItemDto
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = default!;
    public int Quantity { get; set; }
    public decimal PriceAtOrder { get; set; }
}

