using OrderEngine.Domain.Interfaces;

namespace OrderEngine.Infrastructure.Persistence;

public class UnitOfWork : IUnitOfWork
{
    private readonly OrderDbContext _context;

    public UnitOfWork(OrderDbContext context)
    {
        _context = context;
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _context.SaveChangesAsync(cancellationToken);
}
