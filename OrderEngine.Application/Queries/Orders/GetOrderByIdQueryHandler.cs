using System.Text.Json;
using MediatR;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using OrderEngine.Application.DTOs;
using OrderEngine.Application.Interfaces;
using StackExchange.Redis;

namespace OrderEngine.Application.Queries.Orders;

public class GetOrderByIdQueryHandler : IRequestHandler<GetOrderByIdQuery, OrderResponseDto?>
{
    private static readonly TimeSpan CacheBaseTtl = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan LockExpiry = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan LockWaitTimeout = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan LockRetryDelay = TimeSpan.FromMilliseconds(100);

    private readonly IOrderReadStore _orderReadStore;
    private readonly IDistributedCache _cache;
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<GetOrderByIdQueryHandler> _logger;

    public GetOrderByIdQueryHandler(
        IOrderReadStore orderReadStore,
        IDistributedCache cache,
        IConnectionMultiplexer redis,
        ILogger<GetOrderByIdQueryHandler> logger)
    {
        _orderReadStore = orderReadStore;
        _cache = cache;
        _redis = redis;
        _logger = logger;
    }

    public async Task<OrderResponseDto?> Handle(GetOrderByIdQuery request, CancellationToken cancellationToken)
    {
        var cacheKey = $"order:{request.Id}";
        var lockKey = $"lock:order:{request.Id}";

        var cached = await TryGetFromCacheAsync(cacheKey, cancellationToken);
        if (cached is not null)
            return cached;

        var lockValue = $"{Environment.MachineName}:{Guid.NewGuid()}";
        var waitDeadline = DateTime.UtcNow + LockWaitTimeout;

        while (true)
        {
            var lockAcquired = await TryTakeLockAsync(lockKey, lockValue);

            // Redis indisponível: sem lock nem cache, a leitura segue direto para a base.
            if (lockAcquired is null)
                return await _orderReadStore.GetByIdAsync(request.Id, cancellationToken);

            if (lockAcquired.Value)
                return await LoadUnderLockAsync(request.Id, cacheKey, lockKey, lockValue, cancellationToken);

            // Outra requisição está recarregando: aguarda brevemente e tenta ler o cache recém-preenchido.
            cached = await TryGetFromCacheAsync(cacheKey, cancellationToken);
            if (cached is not null)
                return cached;

            if (DateTime.UtcNow >= waitDeadline)
            {
                _logger.LogWarning(
                    "Timed out waiting for the reload lock of order {OrderId}; reading from the database.",
                    request.Id);
                return await _orderReadStore.GetByIdAsync(request.Id, cancellationToken);
            }

            await Task.Delay(LockRetryDelay, cancellationToken);
        }
    }

    private async Task<OrderResponseDto?> LoadUnderLockAsync(
        Guid id,
        string cacheKey,
        string lockKey,
        string lockValue,
        CancellationToken cancellationToken)
    {
        try
        {
            // Double-check: outra requisição pode ter preenchido o cache enquanto esperávamos o lock.
            var cached = await TryGetFromCacheAsync(cacheKey, cancellationToken);
            if (cached is not null)
                return cached;

            var order = await _orderReadStore.GetByIdAsync(id, cancellationToken);
            if (order is not null)
                await TrySetCacheAsync(cacheKey, order, cancellationToken);

            return order;
        }
        finally
        {
            await TryReleaseLockAsync(lockKey, lockValue);
        }
    }

    private async Task<OrderResponseDto?> TryGetFromCacheAsync(string cacheKey, CancellationToken cancellationToken)
    {
        try
        {
            var json = await _cache.GetStringAsync(cacheKey, cancellationToken);
            return json is null ? null : JsonSerializer.Deserialize<OrderResponseDto>(json);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Cache read failed for {CacheKey}; falling back to the database.", cacheKey);
            return null;
        }
    }

    private async Task TrySetCacheAsync(string cacheKey, OrderResponseDto order, CancellationToken cancellationToken)
    {
        // Jitter evita que chaves gravadas juntas expirem no mesmo instante.
        var ttl = CacheBaseTtl + TimeSpan.FromSeconds(Random.Shared.Next(0, 60));

        try
        {
            await _cache.SetStringAsync(
                cacheKey,
                JsonSerializer.Serialize(order),
                new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = ttl },
                cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Cache write failed for {CacheKey}; the response is still returned.", cacheKey);
        }
    }

    private async Task<bool?> TryTakeLockAsync(string lockKey, string lockValue)
    {
        try
        {
            return await _redis.GetDatabase().LockTakeAsync(lockKey, lockValue, LockExpiry);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Could not take the reload lock {LockKey}; Redis may be unavailable.", lockKey);
            return null;
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
            _logger.LogWarning(ex, "Could not release the reload lock {LockKey}; it will expire automatically.", lockKey);
        }
    }
}
