using E_commerce_Application.DTOs.Categories;

namespace E_commerce_Application.Services.Interfaces;

public interface ICategoryService
{
    Task<IReadOnlyCollection<CategoryDto>> GetAllAsync(bool includeDeleted = false);
    Task<CategoryDto?> GetByIdAsync(Guid id, bool includeDeleted = false);
    Task<CategoryDto> CreateAsync(CreateCategoryRequest request);
    Task<CategoryDto> UpdateAsync(Guid id, UpdateCategoryRequest request);
    Task SoftDeleteAsync(Guid id);
}

