namespace OrderEngine.Application.DTOs;

public sealed record OrderItemResponseDto(Guid Id, string Product, int Quantity, decimal Price, decimal Subtotal);
