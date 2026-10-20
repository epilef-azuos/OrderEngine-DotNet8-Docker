using MediatR;
using OrderEngine.Application.DTOs;

namespace OrderEngine.Application.Queries.Orders;

public sealed record GetOrderByIdQuery(Guid Id) : IRequest<OrderResponseDto?>;
