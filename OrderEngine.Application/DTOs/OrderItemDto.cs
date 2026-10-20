namespace OrderEngine.Application.DTOs;

public sealed record OrderItemDto(string Product, int Quantity, decimal Price);
