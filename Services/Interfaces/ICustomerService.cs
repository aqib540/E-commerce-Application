using E_commerce_Application.DTOs.Customers;

namespace E_commerce_Application.Services.Interfaces;

public interface ICustomerService
{
    Task<CustomerProfileDto> GetProfileAsync(Guid customerId);
    Task<CustomerProfileDto> UpdateProfileAsync(Guid customerId, UpdateCustomerProfileRequest request);
}


