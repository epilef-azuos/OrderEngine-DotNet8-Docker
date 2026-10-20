using MediatR;
using OrderEngine.Application.DTOs;

namespace OrderEngine.Application.Commands.Orders;

public sealed record CreateOrderCommand(IReadOnlyCollection<OrderItemDto> Items) : IRequest<Guid>;
