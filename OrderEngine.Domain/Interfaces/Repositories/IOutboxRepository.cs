using OrderEngine.Domain.Entities;

namespace OrderEngine.Domain.Interfaces.Repositories;

public interface IOutboxRepository
{
    Task AddAsync(OutboxMessage message, CancellationToken cancellationToken = default);
}
