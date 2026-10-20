using OrderEngine.Domain.Enums;

namespace OrderEngine.Application.DTOs;

public sealed record OrderResponseDto(
    Guid Id,
    OrderStatus Status,
    decimal TotalAmount,
    IReadOnlyList<OrderItemResponseDto> Items);
