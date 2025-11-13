using E_commerce_Application.DTOs.Common;
using E_commerce_Application.DTOs.Orders;

namespace E_commerce_Application.Services.Interfaces;

public interface IOrderService
{
    Task<OrderDto> CreateOrderAsync(Guid customerId, CreateOrderRequest request);
    Task<OrderDto> GetOrderByIdAsync(Guid orderId, Guid requesterId, bool isAdmin);
    Task<PagedResult<OrderDto>> GetOrdersForCustomerAsync(Guid customerId, int pageNumber, int pageSize);
    Task<PagedResult<OrderDto>> GetAllOrdersAsync(int pageNumber, int pageSize, string? status);
    Task<OrderDto> CancelOrderAsync(Guid orderId, Guid customerId);
    Task<OrderDto> UpdateOrderStatusAsync(Guid orderId, string status);
}

