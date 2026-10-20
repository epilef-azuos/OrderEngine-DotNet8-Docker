using OrderEngine.Application.DTOs;

namespace OrderEngine.Application.Interfaces;

public interface IOrderReadStore
{
    Task<OrderResponseDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
}
