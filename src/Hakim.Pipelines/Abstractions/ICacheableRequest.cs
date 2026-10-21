namespace Hakim.Pipelines.Abstractions;

/// <summary>
/// Marker interface for requests whose response can be cached. When the
/// caching behavior is enabled, a request implementing this interface is
/// served from the cache on subsequent calls with the same
/// <see cref="CacheKey"/>, without executing the handler again.
/// </summary>
public interface ICacheableRequest
{
    /// <summary>
    /// Gets the key that uniquely identifies this request in the cache,
    /// including every input that affects the response
    /// (for example, <c>$"products:{id}"</c>).
    /// </summary>
    string CacheKey { get; }

    /// <summary>
    /// Gets how long the response stays in the cache. When <see langword="null"/>,
    /// the configured default cache duration is used.
    /// </summary>
    TimeSpan? CacheDuration { get; }
}
