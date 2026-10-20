using OrderEngine.Application.DTOs;

namespace OrderEngine.Application.Events;

public sealed record OrderCreatedEvent(Guid OrderId, decimal TotalAmount, IReadOnlyList<OrderItemDto> Items);
