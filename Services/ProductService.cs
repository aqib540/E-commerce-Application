using AutoMapper;
using AutoMapper.QueryableExtensions;
using E_commerce_Application.DbContext;
using E_commerce_Application.DTOs.Common;
using E_commerce_Application.DTOs.Products;
using E_commerce_Application.Entities;
using E_commerce_Application.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace E_commerce_Application.Services;

public class ProductService : IProductService
{
    private readonly ApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly IMemoryCache _cache;
    private readonly ILogger<ProductService> _logger;

    private static readonly string ProductCachePrefix = "product:";

    public ProductService(ApplicationDbContext context, IMapper mapper, IMemoryCache cache, ILogger<ProductService> logger)
    {
        _context = context;
        _mapper = mapper;
        _cache = cache;
        _logger = logger;
    }

    public async Task<PagedResult<ProductDto>> GetPagedAsync(string? searchTerm, Guid? categoryId, int pageNumber, int pageSize, string? sortBy, bool ascending, bool includeInactive = false)
    {
        IQueryable<Product> query = includeInactive
            ? _context.Products.IgnoreQueryFilters()
            : _context.Products;

        query = query.Include(p => p.Category);

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query = query.Where(p => p.Name.Contains(searchTerm));
        }

        if (categoryId.HasValue)
        {
            query = query.Where(p => p.CategoryId == categoryId.Value);
        }

        query = ApplySorting(query, sortBy, ascending);

        var totalCount = await query.CountAsync();

        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ProjectTo<ProductDto>(_mapper.ConfigurationProvider)
            .ToListAsync();

        return new PagedResult<ProductDto>
        {
            Items = items,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public async Task<ProductDto?> GetByIdAsync(Guid id, bool includeInactive = false)
    {
        var cacheKey = GetCacheKey(id, includeInactive);
        if (_cache.TryGetValue(cacheKey, out ProductDto? cached))
        {
            return cached;
        }

        IQueryable<Product> query = includeInactive
            ? _context.Products.IgnoreQueryFilters()
            : _context.Products;

        var product = await query
            .Include(p => p.Category)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (product is null)
        {
            return null;
        }

        var dto = _mapper.Map<ProductDto>(product);
        _cache.Set(cacheKey, dto, TimeSpan.FromMinutes(5));
        return dto;
    }

    public async Task<ProductDto> CreateAsync(CreateProductRequest request)
    {
        await ValidateDuplicateNameAsync(request.Name, request.CategoryId);
        await EnsureCategoryIsActiveAsync(request.CategoryId);

        var product = _mapper.Map<Product>(request);
        product.CreatedDate = DateTime.UtcNow;
        product.IsActive = request.IsActive;

        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        await _context.Entry(product).Reference(p => p.Category).LoadAsync();

        InvalidateProductCache(product.Id);

        return _mapper.Map<ProductDto>(product);
    }

    public async Task<ProductDto> UpdateAsync(Guid id, UpdateProductRequest request)
    {
        var product = await _context.Products.IgnoreQueryFilters().Include(p => p.Category).FirstOrDefaultAsync(p => p.Id == id);
        if (product is null)
        {
            throw new KeyNotFoundException("Product not found.");
        }

        await ValidateDuplicateNameAsync(request.Name, request.CategoryId, id);
        await EnsureCategoryIsActiveAsync(request.CategoryId);

        product.Name = request.Name;
        product.Description = request.Description;
        product.Price = request.Price;
        product.StockQuantity = request.StockQuantity;
        product.CategoryId = request.CategoryId;
        product.IsActive = request.IsActive;
        product.UpdatedDate = DateTime.UtcNow;
        if (request.IsActive)
        {
            product.DeletedDate = null;
        }
        else if (product.DeletedDate is null)
        {
            product.DeletedDate = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
        await _context.Entry(product).Reference(p => p.Category).LoadAsync();

        InvalidateProductCache(product.Id);

        return _mapper.Map<ProductDto>(product);
    }

    public async Task SoftDeleteAsync(Guid id)
    {
        var product = await _context.Products.IgnoreQueryFilters().FirstOrDefaultAsync(p => p.Id == id);
        if (product is null)
        {
            throw new KeyNotFoundException("Product not found.");
        }

        product.IsActive = false;
        product.DeletedDate = DateTime.UtcNow;
        product.UpdatedDate = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        InvalidateProductCache(product.Id);
    }

    private void InvalidateProductCache(Guid productId)
    {
        _cache.Remove(GetCacheKey(productId, includeInactive: false));
        _cache.Remove(GetCacheKey(productId, includeInactive: true));
    }

    private async Task EnsureCategoryIsActiveAsync(Guid categoryId)
    {
        var category = await _context.Categories.IgnoreQueryFilters().FirstOrDefaultAsync(c => c.Id == categoryId);
        if (category is null || category.DeletedDate != null)
        {
            throw new InvalidOperationException("Category is not available.");
        }
    }

    private async Task ValidateDuplicateNameAsync(string name, Guid categoryId, Guid? existingId = null)
    {
        var exists = await _context.Products.IgnoreQueryFilters()
            .AnyAsync(p => p.Name == name && p.CategoryId == categoryId && p.Id != existingId);

        if (exists)
        {
            throw new InvalidOperationException("Product with the same name already exists in this category.");
        }
    }

    private static IQueryable<Product> ApplySorting(IQueryable<Product> query, string? sortBy, bool ascending)
    {
        return sortBy?.ToLower() switch
        {
            "price" => ascending ? query.OrderBy(p => p.Price) : query.OrderByDescending(p => p.Price),
            "createddate" => ascending ? query.OrderBy(p => p.CreatedDate) : query.OrderByDescending(p => p.CreatedDate),
            "stockquantity" => ascending ? query.OrderBy(p => p.StockQuantity) : query.OrderByDescending(p => p.StockQuantity),
            _ => ascending ? query.OrderBy(p => p.Name) : query.OrderByDescending(p => p.Name)
        };
    }

    private static string GetCacheKey(Guid productId, bool includeInactive) => $"{ProductCachePrefix}{productId}:{includeInactive}";
}

