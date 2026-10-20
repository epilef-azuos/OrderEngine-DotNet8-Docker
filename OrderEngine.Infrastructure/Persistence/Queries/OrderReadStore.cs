using Microsoft.EntityFrameworkCore;
using OrderEngine.Application.DTOs;
using OrderEngine.Application.Interfaces;

namespace OrderEngine.Infrastructure.Persistence.Queries;

public class OrderReadStore : IOrderReadStore
{
    private readonly OrderDbContext _context;

    public OrderReadStore(OrderDbContext context)
    {
        _context = context;
    }

    public async Task<OrderResponseDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var order = await _context.Orders
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);

        if (order is null)
            return null;

        var items = order.Items
            .Select(i => new OrderItemResponseDto(i.Id, i.Product, i.Quantity, i.Price, i.Subtotal))
            .ToList();

        return new OrderResponseDto(order.Id, order.Status, order.TotalAmount, items);
    }
}
