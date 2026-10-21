namespace Hakim.Pipelines.Abstractions;

/// <summary>
/// Marker interface for requests (typically commands) that invalidate cached
/// responses. When the caching behavior is enabled, the cache entries listed
/// in <see cref="CacheKeysToInvalidate"/> are removed after the handler
/// completes successfully.
/// </summary>
public interface ICacheInvalidatorRequest
{
    /// <summary>
    /// Gets the cache keys to remove after the request has been handled.
    /// </summary>
    IReadOnlyList<string> CacheKeysToInvalidate { get; }
}
