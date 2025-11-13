using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace E_commerce_Application.Entities;

public enum OrderStatus
{
    Pending,
    Completed,
    Cancelled
}

public class Order : BaseEntity
{
    [Required]
    public Guid CustomerId { get; set; }

    public User? Customer { get; set; }

    [Required]
    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();

    [Range(0, double.MaxValue)]
    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalAmount { get; set; }

    public OrderStatus Status { get; set; } = OrderStatus.Pending;

    public DateTime OrderDate { get; set; } = DateTime.UtcNow;
}

public class OrderItem
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid OrderId { get; set; }

    public Order? Order { get; set; }

    [Required]
    public Guid ProductId { get; set; }

    public Product? Product { get; set; }

    [Range(1, int.MaxValue)]
    public int Quantity { get; set; }

    [Range(0, double.MaxValue)]
    [Column(TypeName = "decimal(18,2)")]
    public decimal PriceAtOrder { get; set; }
}

