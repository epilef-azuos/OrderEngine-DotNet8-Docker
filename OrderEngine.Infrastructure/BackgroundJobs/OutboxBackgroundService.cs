using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OrderEngine.Infrastructure.Messaging;
using OrderEngine.Infrastructure.Persistence;

namespace OrderEngine.Infrastructure.BackgroundJobs;

public class OutboxBackgroundService : BackgroundService
{
    private const int BatchSize = 20;
    private const int MaxErrorLength = 2000;
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(5);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OutboxBackgroundService> _logger;

    public OutboxBackgroundService(IServiceScopeFactory scopeFactory, ILogger<OutboxBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessBatchAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Outbox batch failed; it will be retried on the next cycle.");
            }

            await Task.Delay(PollInterval, stoppingToken);
        }
    }

    private async Task ProcessBatchAsync(CancellationToken cancellationToken)
    {
        // BackgroundService é Singleton: o DbContext (Scoped) é obtido por escopo a cada ciclo.
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
        var publisher = scope.ServiceProvider.GetRequiredService<RabbitMqPublisher>();

        var messages = await context.OutboxMessages
            .Where(m => m.ProcessedOn == null)
            .OrderBy(m => m.OccurredOn)
            .Take(BatchSize)
            .ToListAsync(cancellationToken);

        if (messages.Count == 0)
            return;

        foreach (var message in messages)
        {
            try
            {
                await publisher.PublishAsync(message.Type, message.Content, cancellationToken);
                message.MarkAsProcessed();
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                message.MarkAsFailed(Truncate(ex.Message));
                _logger.LogError(ex, "Failed to publish outbox message {MessageId}.", message.Id);
            }
        }

        // Mensagens não são apagadas: só ProcessedOn/Error são atualizados (at-least-once).
        await context.SaveChangesAsync(cancellationToken);
    }

    private static string Truncate(string text) =>
        text.Length <= MaxErrorLength ? text : text[..MaxErrorLength];
}
