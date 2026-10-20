using System.IO;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OrderEngine.Application.Events;
using OrderEngine.Domain.Enums;
using OrderEngine.Infrastructure.Messaging;
using OrderEngine.Infrastructure.Persistence;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using StackExchange.Redis;

namespace OrderEngine.Worker;

public class OrderProcessingWorker : BackgroundService
{
    private const string QueueName = "order.processing.queue";
    private const string DeadLetterExchange = "order.dlx";
    private const string DeadLetterQueue = "order.dlq";
    private const string DeadLetterRoutingKey = "order.dlq";
    private const string OrderCreatedType = "OrderCreated";

    private static readonly TimeSpan LockExpiry = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan StockLockExpiry = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan StockProcessingDelay = TimeSpan.FromMilliseconds(500);
    private static readonly TimeSpan ProcessedKeyTtl = TimeSpan.FromDays(7);
    private static readonly TimeSpan ProcessingDelay = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan RequeueDelay = TimeSpan.FromSeconds(1);

    private readonly RabbitMqConnectionProvider _connectionProvider;
    private readonly IConnectionMultiplexer _redis;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OrderProcessingWorker> _logger;

    public OrderProcessingWorker(
        RabbitMqConnectionProvider connectionProvider,
        IConnectionMultiplexer redis,
        IServiceScopeFactory scopeFactory,
        ILogger<OrderProcessingWorker> logger)
    {
        _connectionProvider = connectionProvider;
        _redis = redis;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ConsumeUntilShutdownAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "RabbitMQ consumer failed; retrying in {Delay}.", RetryDelay);
            }

            await Task.Delay(RetryDelay, stoppingToken);
        }
    }

    private async Task ConsumeUntilShutdownAsync(CancellationToken stoppingToken)
    {
        var connection = await _connectionProvider.GetConnectionAsync(stoppingToken);
        var channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);

        await using (channel)
        {
            var channelClosed = new TaskCompletionSource();
            channel.ChannelShutdownAsync += (_, _) =>
            {
                channelClosed.TrySetResult();
                return Task.CompletedTask;
            };

            // Declara a exchange e a fila antes de consumir (idempotente).
            await channel.ExchangeDeclareAsync(
                RabbitMqPublisher.ExchangeName,
                ExchangeType.Fanout,
                durable: true,
                cancellationToken: stoppingToken);

            // DLX/DLQ: mensagens rejeitadas com requeue: false são preservadas na DLQ.
            await channel.ExchangeDeclareAsync(
                DeadLetterExchange,
                ExchangeType.Direct,
                durable: true,
                cancellationToken: stoppingToken);

            await channel.QueueDeclareAsync(
                DeadLetterQueue,
                durable: true,
                exclusive: false,
                autoDelete: false,
                cancellationToken: stoppingToken);

            await channel.QueueBindAsync(
                DeadLetterQueue,
                DeadLetterExchange,
                routingKey: DeadLetterRoutingKey,
                cancellationToken: stoppingToken);

            // Argumentos de DLX pertencem à declaração da fila, não ao bind.
            var queueArguments = new Dictionary<string, object?>
            {
                ["x-dead-letter-exchange"] = DeadLetterExchange,
                ["x-dead-letter-routing-key"] = DeadLetterRoutingKey
            };

            await channel.QueueDeclareAsync(
                QueueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: queueArguments,
                cancellationToken: stoppingToken);

            await channel.QueueBindAsync(
                QueueName,
                RabbitMqPublisher.ExchangeName,
                routingKey: string.Empty,
                cancellationToken: stoppingToken);

            // Uma mensagem por vez por worker: distribuição justa entre instâncias.
            await channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 1, global: false, cancellationToken: stoppingToken);

            var consumer = new AsyncEventingBasicConsumer(channel);
            consumer.ReceivedAsync += (_, ea) => HandleMessageAsync(channel, ea, stoppingToken);

            // autoAck: false — o ack só é enviado após o processamento concluir.
            await channel.BasicConsumeAsync(QueueName, autoAck: false, consumer: consumer, cancellationToken: stoppingToken);

            _logger.LogInformation("Listening on queue {Queue}.", QueueName);

            await Task.WhenAny(channelClosed.Task, Task.Delay(Timeout.Infinite, stoppingToken));
        }
    }

    private async Task HandleMessageAsync(IChannel channel, BasicDeliverEventArgs ea, CancellationToken stoppingToken)
    {
        try
        {
            if (ea.BasicProperties.Type != OrderCreatedType)
            {
                _logger.LogWarning("Ignoring message of unexpected type {MessageType}.", ea.BasicProperties.Type);
                await channel.BasicAckAsync(ea.DeliveryTag, multiple: false, stoppingToken);
                return;
            }

            var orderCreated = JsonSerializer.Deserialize<OrderCreatedEvent>(ea.Body.Span)
                ?? throw new InvalidDataException("The OrderCreated payload is empty.");

            var outcome = await ProcessOrderAsync(orderCreated.OrderId, stoppingToken);

            if (outcome == ProcessOutcome.LockUnavailable)
            {
                await Task.Delay(RequeueDelay, stoppingToken);
                await channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: true, stoppingToken);
                return;
            }

            await channel.BasicAckAsync(ea.DeliveryTag, multiple: false, stoppingToken);
        }
        catch (RedisException ex)
        {
            _logger.LogError(ex, "Redis unavailable while handling delivery {DeliveryTag}; requeueing.", ea.DeliveryTag);
            await Task.Delay(RequeueDelay, stoppingToken);
            await channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: true, stoppingToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Falha não controlada: sem requeue, para não entrar em loop infinito.
            _logger.LogError(ex, "Unhandled failure on delivery {DeliveryTag}; discarding without requeue.", ea.DeliveryTag);
            await channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false, stoppingToken);
        }
    }

    private async Task<ProcessOutcome> ProcessOrderAsync(Guid orderId, CancellationToken cancellationToken)
    {
        var db = _redis.GetDatabase();
        var processedKey = $"worker:processed:{orderId}";

        // Idempotência: entrega duplicada de uma mensagem já processada.
        if ((await db.StringGetAsync(processedKey)).HasValue)
        {
            _logger.LogInformation("Order {OrderId} was already processed; acknowledging duplicate.", orderId);
            return ProcessOutcome.Skipped;
        }

        // Chave distinta da do GET (lock:order:{id}) para não colidir com o lock de leitura da Fase 04.
        var lockKey = $"lock:order-processing:{orderId}";
        var lockValue = $"{Environment.MachineName}:{Guid.NewGuid()}";

        if (!await db.LockTakeAsync(lockKey, lockValue, LockExpiry))
        {
            _logger.LogInformation("Order {OrderId} is locked by another worker; requeueing.", orderId);
            return ProcessOutcome.LockUnavailable;
        }

        try
        {
            // Escopo próprio por mensagem: o DbContext é Scoped e o worker é Singleton.
            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<OrderDbContext>();

            var order = await dbContext.Orders.FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken);

            if (order is null)
            {
                _logger.LogWarning("Order {OrderId} not found; discarding message.", orderId);
                return ProcessOutcome.Skipped;
            }

            // Double-check no banco: cobre o caso em que a chave do Redis expirou ou foi perdida.
            if (order.Status is OrderStatus.Completed or OrderStatus.Failed)
            {
                _logger.LogInformation("Order {OrderId} is already {Status}; skipping.", orderId, order.Status);
                await db.StringSetAsync(processedKey, "1", ProcessedKeyTtl);
                return ProcessOutcome.Skipped;
            }

            _logger.LogInformation("Processing Order {OrderId}", orderId);

            if (order.Status == OrderStatus.Pending)
            {
                order.MarkAsProcessing();
                await dbContext.SaveChangesAsync(cancellationToken);
            }

            var products = order.Items.Select(i => i.Product).Distinct();

            if (!await TryReserveStockAsync(db, products, lockValue, cancellationToken))
            {
                _logger.LogInformation("Stock for order {OrderId} is locked by another order; requeueing.", orderId);
                return ProcessOutcome.LockUnavailable;
            }

            // Simula I/O pesado (pagamento).
            await Task.Delay(ProcessingDelay, cancellationToken);

            order.MarkAsCompleted();
            await dbContext.SaveChangesAsync(cancellationToken);

            await TryInvalidateReadCacheAsync(db, orderId);

            await db.StringSetAsync(processedKey, "1", ProcessedKeyTtl);

            return ProcessOutcome.Processed;
        }
        finally
        {
            await TryReleaseLockAsync(lockKey, lockValue);
        }
    }

    private async Task<bool> TryReserveStockAsync(
        IDatabase db,
        IEnumerable<string> products,
        string lockValue,
        CancellationToken cancellationToken)
    {
        // Um lock de produto por vez: nunca se segura um produto enquanto se espera outro.
        foreach (var product in products)
        {
            var lockKey = $"lock:stock:{product}";

            if (!await db.LockTakeAsync(lockKey, lockValue, StockLockExpiry))
                return false;

            try
            {
                // Simula a baixa de estoque do produto.
                await Task.Delay(StockProcessingDelay, cancellationToken);
            }
            finally
            {
                await TryReleaseLockAsync(lockKey, lockValue);
            }
        }

        return true;
    }

    private async Task TryInvalidateReadCacheAsync(IDatabase db, Guid orderId)
    {
        try
        {
            // Chave gravada pelo IDistributedCache da API (sem InstanceName).
            await db.KeyDeleteAsync($"order:{orderId}");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // A BD já está correta; o cache expira sozinho em até 5 minutos.
            _logger.LogWarning(ex, "Could not invalidate the read cache of order {OrderId}.", orderId);
        }
    }

    private async Task TryReleaseLockAsync(string lockKey, string lockValue)
    {
        try
        {
            await _redis.GetDatabase().LockReleaseAsync(lockKey, lockValue);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Se a liberação falhar, a trava expira sozinha após LockExpiry.
            _logger.LogWarning(ex, "Could not release lock {LockKey}; it will expire automatically.", lockKey);
        }
    }

    private enum ProcessOutcome
    {
        Processed,
        Skipped,
        LockUnavailable
    }
}
