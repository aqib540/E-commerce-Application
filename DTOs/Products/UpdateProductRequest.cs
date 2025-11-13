using System.ComponentModel.DataAnnotations;

namespace E_commerce_Application.DTOs.Products;

public class UpdateProductRequest
{
    [Required]
    [MaxLength(150)]
    public string Name { get; set; } = default!;

    [MaxLength(500)]
    public string? Description { get; set; }

    [Range(0, double.MaxValue)]
    public decimal Price { get; set; }

    [Required]
    public Guid CategoryId { get; set; }

    [Range(0, int.MaxValue)]
    public int StockQuantity { get; set; }

    public bool IsActive { get; set; }
}

