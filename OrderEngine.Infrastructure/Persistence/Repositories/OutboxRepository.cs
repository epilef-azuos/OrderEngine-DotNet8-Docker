using OrderEngine.Domain.Entities;
using OrderEngine.Domain.Interfaces.Repositories;

namespace OrderEngine.Infrastructure.Persistence.Repositories;

public class OutboxRepository : IOutboxRepository
{
    private readonly OrderDbContext _context;

    public OutboxRepository(OrderDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(OutboxMessage message, CancellationToken cancellationToken = default)
    {
        await _context.OutboxMessages.AddAsync(message, cancellationToken);
    }
}
