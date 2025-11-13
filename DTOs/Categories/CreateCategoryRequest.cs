using System.ComponentModel.DataAnnotations;

namespace E_commerce_Application.DTOs.Categories;

public class CreateCategoryRequest
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = default!;

    [MaxLength(500)]
    public string? Description { get; set; }
}

