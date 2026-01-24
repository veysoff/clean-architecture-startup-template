namespace Application.Abstractions.Caching;

/// <summary>
/// Marker interface for queries that support caching.
/// Implement this interface on IQuery to enable automatic caching via QueryCachingDecorator.
/// </summary>
public interface ICachedQuery
{
    /// <summary>
    /// Unique cache key for this query.
    /// Should include all parameters that affect the result.
    /// Example: $"user:{UserId}" or $"todos:{UserId}:{IsCompleted}"
    /// </summary>
    string CacheKey { get; }

    /// <summary>
    /// Cache expiration time. If null, uses default expiration (5 minutes).
    /// </summary>
    TimeSpan? Expiration { get; }
}
