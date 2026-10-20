using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Caching.Distributed;

namespace OrderEngine.Api.Filters;

public class IdempotencyFilterAttribute : Attribute, IAsyncActionFilter
{
    private const string HeaderName = "Idempotency-Key";
    private static readonly TimeSpan ResponseTtl = TimeSpan.FromHours(24);

    private readonly IDistributedCache _cache;

    public IdempotencyFilterAttribute(IDistributedCache cache)
    {
        _cache = cache;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var idempotencyKey = context.HttpContext.Request.Headers[HeaderName].ToString();

        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            context.Result = new BadRequestObjectResult($"The {HeaderName} header is required.");
            return;
        }

        var cacheKey = $"idempotency:{idempotencyKey}";
        var cachedResponse = await _cache.GetStringAsync(cacheKey, context.HttpContext.RequestAborted);

        if (cachedResponse is not null)
        {
            var stored = JsonSerializer.Deserialize<StoredResponse>(cachedResponse)!;

            context.Result = new ContentResult
            {
                StatusCode = stored.StatusCode,
                Content = stored.Body,
                ContentType = "application/json"
            };
            return;
        }

        var executed = await next();

        if (executed.Result is ObjectResult { Value: not null } result && IsSuccess(result.StatusCode))
        {
            var response = new StoredResponse(
                result.StatusCode ?? StatusCodes.Status200OK,
                JsonSerializer.Serialize(result.Value));

            await _cache.SetStringAsync(
                cacheKey,
                JsonSerializer.Serialize(response),
                new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = ResponseTtl },
                context.HttpContext.RequestAborted);
        }
    }

    private static bool IsSuccess(int? statusCode) =>
        (statusCode ?? StatusCodes.Status200OK) is >= 200 and < 300;

    public sealed record StoredResponse(int StatusCode, string Body);
}
