using AutoMapper;
using AutoMapper.QueryableExtensions;
using E_commerce_Application.DbContext;
using E_commerce_Application.DTOs.Common;
using E_commerce_Application.DTOs.Orders;
using E_commerce_Application.Entities;
using E_commerce_Application.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace E_commerce_Application.Services;

public class OrderService : IOrderService
{
    private readonly ApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly ILogger<OrderService> _logger;
    private readonly IEmailService _emailService;

    public OrderService(
        ApplicationDbContext context,
        IMapper mapper,
        ILogger<OrderService> logger,
        IEmailService emailService)
    {
        _context = context;
        _mapper = mapper;
        _logger = logger;
        _emailService = emailService;
    }

    public async Task<OrderDto> CreateOrderAsync(Guid customerId, CreateOrderRequest request)
    {
        var customer = await GetCustomerAsync(customerId);

        var productIds = request.Items.Select(i => i.ProductId).ToList();
        var products = await _context.Products
            .Where(p => productIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id);

        if (products.Count != productIds.Distinct().Count())
        {
            throw new InvalidOperationException("One or more products were not found.");
        }

        await using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            decimal totalAmount = 0;
            var orderItems = new List<OrderItem>();

            foreach (var item in request.Items)
            {
                var product = products[item.ProductId];
                if (!product.IsActive || product.DeletedDate != null)
                {
                    throw new InvalidOperationException($"Product '{product.Name}' is not available.");
                }

                if (product.StockQuantity < item.Quantity)
                {
                    throw new InvalidOperationException($"Insufficient stock for product '{product.Name}'.");
                }

                product.StockQuantity -= item.Quantity;
                product.UpdatedDate = DateTime.UtcNow;

                var lineTotal = product.Price * item.Quantity;
                totalAmount += lineTotal;

                orderItems.Add(new OrderItem
                {
                    ProductId = product.Id,
                    Quantity = item.Quantity,
                    PriceAtOrder = product.Price
                });
            }

            var order = new Order
            {
                CustomerId = customerId,
                Items = orderItems,
                TotalAmount = totalAmount,
                OrderDate = DateTime.UtcNow,
                Status = OrderStatus.Pending
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            await SendOrderPlacedEmailAsync(customer, order);

            return await GetOrderDtoAsync(order.Id, true);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Failed to create order for customer {CustomerId}", customerId);
            throw;
        }
    }

    public async Task<OrderDto> GetOrderByIdAsync(Guid orderId, Guid requesterId, bool isAdmin)
    {
        var query = _context.Orders
            .Where(o => o.Id == orderId);

        if (!isAdmin)
        {
            query = query.Where(o => o.CustomerId == requesterId);
        }

        var order = await query
            .Include(o => o.Items).ThenInclude(i => i.Product)
            .Include(o => o.Customer)
            .FirstOrDefaultAsync();
        if (order is null)
        {
            throw new KeyNotFoundException("Order not found.");
        }

        return _mapper.Map<OrderDto>(order);
    }

    public async Task<PagedResult<OrderDto>> GetOrdersForCustomerAsync(Guid customerId, int pageNumber, int pageSize)
    {
        var query = _context.Orders
            .Where(o => o.CustomerId == customerId)
            .OrderByDescending(o => o.OrderDate);

        var total = await query.CountAsync();
        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ProjectTo<OrderDto>(_mapper.ConfigurationProvider)
            .ToListAsync();

        return new PagedResult<OrderDto>
        {
            Items = items,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = total
        };
    }

    public async Task<PagedResult<OrderDto>> GetAllOrdersAsync(int pageNumber, int pageSize, string? status)
    {
        var query = _context.Orders
            .OrderByDescending(o => o.OrderDate);

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<OrderStatus>(status, true, out var orderStatus))
        {
            query = query.Where(o => o.Status == orderStatus).OrderByDescending(o => o.OrderDate);
        }

        var total = await query.CountAsync();
        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ProjectTo<OrderDto>(_mapper.ConfigurationProvider)
            .ToListAsync();

        return new PagedResult<OrderDto>
        {
            Items = items,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = total
        };
    }

    public async Task<OrderDto> CancelOrderAsync(Guid orderId, Guid customerId)
    {
        var order = await _context.Orders
            .Include(o => o.Items)
            .Include(o => o.Customer)
            .FirstOrDefaultAsync(o => o.Id == orderId && o.CustomerId == customerId);

        if (order is null)
        {
            throw new KeyNotFoundException("Order not found.");
        }

        if (order.Status != OrderStatus.Pending)
        {
            throw new InvalidOperationException("Only pending orders can be cancelled.");
        }

        await using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            order.Status = OrderStatus.Cancelled;
            order.UpdatedDate = DateTime.UtcNow;

            var productIds = order.Items.Select(i => i.ProductId).ToList();
            var products = await _context.Products.IgnoreQueryFilters()
                .Where(p => productIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id);

            foreach (var item in order.Items)
            {
                if (products.TryGetValue(item.ProductId, out var product))
                {
                    product.StockQuantity += item.Quantity;
                    product.UpdatedDate = DateTime.UtcNow;
                }
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            if (order.Customer != null)
            {
                await SendOrderCancelledEmailAsync(order.Customer, order);
            }

            return await GetOrderDtoAsync(order.Id, true);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Failed to cancel order {OrderId}", orderId);
            throw;
        }
    }

    public async Task<OrderDto> UpdateOrderStatusAsync(Guid orderId, string status)
    {
        if (!Enum.TryParse<OrderStatus>(status, true, out var newStatus))
        {
            throw new ArgumentException("Invalid order status.", nameof(status));
        }

        var order = await _context.Orders
            .Include(o => o.Items)
            .Include(o => o.Customer)
            .FirstOrDefaultAsync(o => o.Id == orderId);

        if (order is null)
        {
            throw new KeyNotFoundException("Order not found.");
        }

        if (order.Status == OrderStatus.Cancelled)
        {
            throw new InvalidOperationException("Cancelled orders cannot change status.");
        }

        await using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            if (newStatus == OrderStatus.Cancelled && order.Status != OrderStatus.Cancelled)
            {
                var productIds = order.Items.Select(i => i.ProductId).ToList();
                var products = await _context.Products.IgnoreQueryFilters()
                    .Where(p => productIds.Contains(p.Id))
                    .ToDictionaryAsync(p => p.Id);

                foreach (var item in order.Items)
                {
                    if (products.TryGetValue(item.ProductId, out var product))
                    {
                        product.StockQuantity += item.Quantity;
                        product.UpdatedDate = DateTime.UtcNow;
                    }
                }
            }

            order.Status = newStatus;
            order.UpdatedDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            if (newStatus == OrderStatus.Cancelled && order.Customer != null)
            {
                await SendOrderCancelledEmailAsync(order.Customer, order);
            }

            return await GetOrderDtoAsync(order.Id, true);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Failed to update order {OrderId} status", orderId);
            throw;
        }
    }

    private async Task<OrderDto> GetOrderDtoAsync(Guid orderId, bool loadNavigation)
    {
        var query = _context.Orders.Where(o => o.Id == orderId);
        if (loadNavigation)
        {
            query = query
                .Include(o => o.Items).ThenInclude(i => i.Product)
                .Include(o => o.Customer);
        }

        var order = await query.FirstAsync();
        return _mapper.Map<OrderDto>(order);
    }

    private async Task<User> GetCustomerAsync(Guid customerId)
    {
        var customer = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == customerId && u.Role == UserRole.Customer);

        if (customer is null)
        {
            throw new KeyNotFoundException("Customer not found.");
        }

        return customer;
    }

    private async Task SendOrderPlacedEmailAsync(User customer, Order order)
    {
        var subject = $"Order Confirmation - {order.Id}";
        var body = BuildOrderEmailBody(customer, order, "placed", order.OrderDate, order.TotalAmount);
        await _emailService.SendEmailAsync(customer.Email, subject, body);
    }

    private async Task SendOrderCancelledEmailAsync(User customer, Order order)
    {
        var subject = $"Order Cancelled - {order.Id}";
        var eventDate = order.UpdatedDate ?? DateTime.UtcNow;
        var body = BuildOrderEmailBody(customer, order, "cancelled", eventDate, order.TotalAmount);
        await _emailService.SendEmailAsync(customer.Email, subject, body);
    }

    private static string BuildOrderEmailBody(User customer, Order order, string action, DateTime eventDate, decimal totalAmount)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"Hi {customer.Name},");
        builder.AppendLine();
        builder.AppendLine($"Your order {order.Id} was {action} on {eventDate:yyyy-MM-dd HH:mm} UTC.");
        builder.AppendLine();
        builder.AppendLine("Order summary:");

        foreach (var item in order.Items)
        {
            builder.AppendLine($" - Product: {item.ProductId}, Quantity: {item.Quantity}, Price: {item.PriceAtOrder:C}");
        }

        builder.AppendLine();
        builder.AppendLine($"Total amount: {totalAmount:C}");
        builder.AppendLine();
        builder.AppendLine("Thank you for shopping with us!");

        return builder.ToString();
    }
}

