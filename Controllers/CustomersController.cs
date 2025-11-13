using E_commerce_Application.DTOs.Customers;
using E_commerce_Application.Extensions;
using E_commerce_Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace E_commerce_Application.Controllers;

[ApiController]
[Route("api/customers/me")]
[Authorize(Roles = "Customer")]
public class CustomersController : ControllerBase
{
    private readonly ICustomerService _customerService;
    private readonly ILogger<CustomersController> _logger;

    public CustomersController(ICustomerService customerService, ILogger<CustomersController> logger)
    {
        _customerService = customerService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<CustomerProfileDto>> GetProfile()
    {
        try
        {
            var customerId = User.GetUserId();
            var profile = await _customerService.GetProfileAsync(customerId);
            return Ok(profile);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Customer profile not found for current user");
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while fetching customer profile");
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An error occurred while processing the request." });
        }
    }

    [HttpPut]
    public async Task<ActionResult<CustomerProfileDto>> UpdateProfile(UpdateCustomerProfileRequest request)
    {
        try
        {
            var customerId = User.GetUserId();
            var profile = await _customerService.UpdateProfileAsync(customerId, request);
            return Ok(profile);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Customer profile update failed");
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Customer profile update constraint violation");
            return Conflict(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while updating customer profile");
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An error occurred while processing the request." });
        }
    }
}


