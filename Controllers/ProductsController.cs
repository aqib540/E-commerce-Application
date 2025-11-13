using E_commerce_Application.DTOs.Common;
using E_commerce_Application.DTOs.Products;
using E_commerce_Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace E_commerce_Application.Controllers;

[ApiController]
[Route("api/products")]
[AllowAnonymous]
public class ProductsController : ControllerBase
{
    private readonly IProductService _productService;

    public ProductsController(IProductService productService)
    {
        _productService = productService;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<ProductDto>>> GetProducts(
        [FromQuery] string? name = null,
        [FromQuery] Guid? categoryId = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? sortBy = null,
        [FromQuery] bool ascending = true)
    {
        if (pageNumber <= 0 || pageSize <= 0)
        {
            return BadRequest(new { message = "Page number and size must be greater than zero." });
        }

        var result = await _productService.GetPagedAsync(name, categoryId, pageNumber, pageSize, sortBy, ascending);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ProductDto>> GetProduct(Guid id)
    {
        var product = await _productService.GetByIdAsync(id);
        if (product is null)
        {
            return NotFound(new { message = "Product not found." });
        }

        return Ok(product);
    }
}


