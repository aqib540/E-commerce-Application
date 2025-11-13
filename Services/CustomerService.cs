using AutoMapper;
using E_commerce_Application.DbContext;
using E_commerce_Application.DTOs.Customers;
using E_commerce_Application.Entities;
using E_commerce_Application.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace E_commerce_Application.Services;

public class CustomerService : ICustomerService
{
    private readonly ApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly ILogger<CustomerService> _logger;

    public CustomerService(ApplicationDbContext context, IMapper mapper, ILogger<CustomerService> logger)
    {
        _context = context;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<CustomerProfileDto> GetProfileAsync(Guid customerId)
    {
        var customer = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == customerId && u.Role == UserRole.Customer);

        if (customer is null)
        {
            throw new KeyNotFoundException("Customer not found.");
        }

        return _mapper.Map<CustomerProfileDto>(customer);
    }

    public async Task<CustomerProfileDto> UpdateProfileAsync(Guid customerId, UpdateCustomerProfileRequest request)
    {
        var customer = await _context.Users.FirstOrDefaultAsync(u => u.Id == customerId && u.Role == UserRole.Customer);
        if (customer is null)
        {
            throw new KeyNotFoundException("Customer not found.");
        }

        var emailChanged = !string.Equals(customer.Email, request.Email, StringComparison.OrdinalIgnoreCase);
        if (emailChanged)
        {
            var emailExists = await _context.Users
                .AnyAsync(u => u.Email == request.Email && u.Id != customer.Id);

            if (emailExists)
            {
                throw new InvalidOperationException("Email is already in use.");
            }
        }

        customer.Name = request.Name;
        customer.Email = request.Email;
        customer.UpdatedDate = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        _logger.LogInformation("Updated profile for customer {CustomerId}", customerId);

        return _mapper.Map<CustomerProfileDto>(customer);
    }
}


