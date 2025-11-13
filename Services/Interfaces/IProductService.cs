using E_commerce_Application.DTOs.Common;
using E_commerce_Application.DTOs.Products;

namespace E_commerce_Application.Services.Interfaces;

public interface IProductService
{
    Task<PagedResult<ProductDto>> GetPagedAsync(string? searchTerm, Guid? categoryId, int pageNumber, int pageSize, string? sortBy, bool ascending, bool includeInactive = false);
    Task<ProductDto?> GetByIdAsync(Guid id, bool includeInactive = false);
    Task<ProductDto> CreateAsync(CreateProductRequest request);
    Task<ProductDto> UpdateAsync(Guid id, UpdateProductRequest request);
    Task SoftDeleteAsync(Guid id);
}

