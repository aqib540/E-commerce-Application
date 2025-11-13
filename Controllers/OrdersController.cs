using E_commerce_Application.DTOs.Common;
using E_commerce_Application.DTOs.Orders;
using E_commerce_Application.Extensions;
using E_commerce_Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace E_commerce_Application.Controllers;

[ApiController]
[Route("api/orders")]
[Authorize(Roles = "Customer")]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _orderService;
    private readonly ILogger<OrdersController> _logger;

    public OrdersController(IOrderService orderService, ILogger<OrdersController> logger)
    {
        _orderService = orderService;
        _logger = logger;
    }

    [HttpPost]
    public async Task<ActionResult<OrderDto>> CreateOrder(CreateOrderRequest request)
    {
        var customerId = User.GetUserId();

        try
        {
            var order = await _orderService.CreateOrderAsync(customerId, request);
            return CreatedAtAction(nameof(GetOrderById), new { id = order.Id }, order);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Order creation failed for user {UserId}", customerId);
            return BadRequest(new { message = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Order creation failed for user {UserId}", customerId);
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while creating order for user {UserId}", customerId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An error occurred while processing the request." });
        }
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<OrderDto>>> GetOrders(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20)
    {
        if (pageNumber <= 0 || pageSize <= 0)
        {
            return BadRequest(new { message = "Page number and size must be greater than zero." });
        }

        var customerId = User.GetUserId();
        var result = await _orderService.GetOrdersForCustomerAsync(customerId, pageNumber, pageSize);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<OrderDto>> GetOrderById(Guid id)
    {
        try
        {
            var customerId = User.GetUserId();
            var order = await _orderService.GetOrderByIdAsync(id, customerId, isAdmin: false);
            return Ok(order);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Order lookup failed for {OrderId}", id);
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while retrieving order {OrderId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An error occurred while processing the request." });
        }
    }

    [HttpPost("{id:guid}/cancel")]
    public async Task<ActionResult<OrderDto>> CancelOrder(Guid id)
    {
        var customerId = User.GetUserId();

        try
        {
            var order = await _orderService.CancelOrderAsync(id, customerId);
            return Ok(order);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Order cancel failed for {OrderId}", id);
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Order cancel validation failed for {OrderId}", id);
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while cancelling order {OrderId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An error occurred while processing the request." });
        }
    }
}


