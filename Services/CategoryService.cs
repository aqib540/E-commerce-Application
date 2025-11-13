using AutoMapper;
using AutoMapper.QueryableExtensions;
using E_commerce_Application.DbContext;
using E_commerce_Application.DTOs.Categories;
using E_commerce_Application.Entities;
using E_commerce_Application.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace E_commerce_Application.Services;

public class CategoryService : ICategoryService
{
    private readonly ApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly IMemoryCache _cache;
    private readonly ILogger<CategoryService> _logger;

    private const string CategoryCacheKey = "categories:all";

    public CategoryService(ApplicationDbContext context, IMapper mapper, IMemoryCache cache, ILogger<CategoryService> logger)
    {
        _context = context;
        _mapper = mapper;
        _cache = cache;
        _logger = logger;
    }

    public async Task<IReadOnlyCollection<CategoryDto>> GetAllAsync(bool includeDeleted = false)
    {
        if (!includeDeleted && _cache.TryGetValue(CategoryCacheKey, out IReadOnlyCollection<CategoryDto>? cached))
        {
            return cached ?? Array.Empty<CategoryDto>();
        }

        IQueryable<Category> query = includeDeleted
            ? _context.Categories.IgnoreQueryFilters()
            : _context.Categories;

        var categories = await query
            .ProjectTo<CategoryDto>(_mapper.ConfigurationProvider)
            .ToListAsync();

        if (!includeDeleted)
        {
            _cache.Set(CategoryCacheKey, categories, TimeSpan.FromMinutes(10));
        }

        return categories;
    }

    public async Task<CategoryDto?> GetByIdAsync(Guid id, bool includeDeleted = false)
    {
        IQueryable<Category> query = includeDeleted
            ? _context.Categories.IgnoreQueryFilters()
            : _context.Categories;

        var category = await query.FirstOrDefaultAsync(c => c.Id == id);
        return category is null ? null : _mapper.Map<CategoryDto>(category);
    }

    public async Task<CategoryDto> CreateAsync(CreateCategoryRequest request)
    {
        await ValidateDuplicateNameAsync(request.Name);

        var category = _mapper.Map<Category>(request);
        category.CreatedDate = DateTime.UtcNow;

        _context.Categories.Add(category);
        await _context.SaveChangesAsync();

        InvalidateCache();

        return _mapper.Map<CategoryDto>(category);
    }

    public async Task<CategoryDto> UpdateAsync(Guid id, UpdateCategoryRequest request)
    {
        var category = await _context.Categories.IgnoreQueryFilters().FirstOrDefaultAsync(c => c.Id == id);
        if (category is null)
        {
            throw new KeyNotFoundException("Category not found.");
        }

        await ValidateDuplicateNameAsync(request.Name, id);

        category.Name = request.Name;
        category.Description = request.Description;
        category.UpdatedDate = DateTime.UtcNow;
        if (category.DeletedDate != null)
        {
            category.DeletedDate = null;
        }

        await _context.SaveChangesAsync();

        InvalidateCache();

        return _mapper.Map<CategoryDto>(category);
    }

    public async Task SoftDeleteAsync(Guid id)
    {
        var category = await _context.Categories.IgnoreQueryFilters().FirstOrDefaultAsync(c => c.Id == id);
        if (category is null)
        {
            throw new KeyNotFoundException("Category not found.");
        }

        var hasActiveProducts = await _context.Products.IgnoreQueryFilters()
            .AnyAsync(p => p.CategoryId == id && p.DeletedDate == null);

        if (hasActiveProducts)
        {
            throw new InvalidOperationException("Cannot delete category with active products.");
        }

        category.DeletedDate = DateTime.UtcNow;
        category.UpdatedDate = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        InvalidateCache();
    }

    private async Task ValidateDuplicateNameAsync(string name, Guid? existingId = null)
    {
        var exists = await _context.Categories.IgnoreQueryFilters()
            .AnyAsync(c => c.Name == name && c.Id != existingId);

        if (exists)
        {
            throw new InvalidOperationException("Category with the same name already exists.");
        }
    }

    private void InvalidateCache()
    {
        _cache.Remove(CategoryCacheKey);
    }
}

