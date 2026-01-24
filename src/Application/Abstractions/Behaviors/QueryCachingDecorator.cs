using System.Text.Json;
using Application.Abstractions.Caching;
using Application.Abstractions.Messaging;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using SharedKernel;

namespace Application.Abstractions.Behaviors;

/// <summary>
/// Decorator that adds caching functionality to queries implementing ICachedQuery.
/// Uses IDistributedCache (Redis) to cache query results.
/// </summary>
internal sealed class QueryCachingDecorator<TQuery, TResponse>(
    IQueryHandler<TQuery, TResponse> innerHandler,
    IDistributedCache cache,
    ILogger<QueryCachingDecorator<TQuery, TResponse>> logger)
    : IQueryHandler<TQuery, TResponse>
    where TQuery : IQuery<TResponse>, ICachedQuery
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = false
    };

    private static readonly TimeSpan DefaultExpiration = TimeSpan.FromMinutes(5);

    public async Task<Result<TResponse>> Handle(TQuery query, CancellationToken cancellationToken)
    {
        string cacheKey = query.CacheKey;

        // Try to get from cache
        string? cachedValue = await cache.GetStringAsync(cacheKey, cancellationToken);

        if (cachedValue is not null)
        {
            logger.LogDebug("Cache hit for key: {CacheKey}", cacheKey);

            try
            {
                Result<TResponse>? cachedResult = JsonSerializer.Deserialize<Result<TResponse>>(
                    cachedValue,
                    SerializerOptions);

                if (cachedResult is not null)
                {
                    return cachedResult;
                }

                logger.LogWarning("Failed to deserialize cached value for key: {CacheKey}", cacheKey);
            }
            catch (JsonException ex)
            {
                logger.LogWarning(ex, "Failed to deserialize cached value for key: {CacheKey}", cacheKey);
                await cache.RemoveAsync(cacheKey, cancellationToken);
            }
        }

        logger.LogDebug("Cache miss for key: {CacheKey}", cacheKey);

        // Execute query
        Result<TResponse> result = await innerHandler.Handle(query, cancellationToken);

        // Cache successful results only
        if (result.IsSuccess)
        {
            TimeSpan expiration = query.Expiration ?? DefaultExpiration;

            var cacheOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiration
            };

            try
            {
                string serializedResult = JsonSerializer.Serialize(result, SerializerOptions);
                await cache.SetStringAsync(cacheKey, serializedResult, cacheOptions, cancellationToken);

                logger.LogDebug(
                    "Cached result for key: {CacheKey} with expiration: {Expiration}",
                    cacheKey,
                    expiration);
            }
            catch (JsonException ex)
            {
                logger.LogWarning(ex, "Failed to serialize result for caching, key: {CacheKey}", cacheKey);
            }
        }

        return result;
    }
}
