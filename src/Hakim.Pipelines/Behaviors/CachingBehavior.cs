using Hakim.Pipelines.Abstractions;
using MediatR;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Hakim.Pipelines.Behaviors;

/// <summary>
/// Caches responses of requests that implement <see cref="ICacheableRequest"/>
/// in an <see cref="IMemoryCache"/>: the first call executes the handler and
/// stores the response; later calls with the same cache key return the stored
/// response without executing the handler.
/// Requests that implement <see cref="ICacheInvalidatorRequest"/> evict their
/// listed cache keys after the handler completes successfully.
/// </summary>
/// <typeparam name="TRequest">The type of the request.</typeparam>
/// <typeparam name="TResponse">The type of the response.</typeparam>
public sealed class CachingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IMemoryCache _cache;
    private readonly PipelineOptions _options;
    private readonly ILogger<CachingBehavior<TRequest, TResponse>> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="CachingBehavior{TRequest, TResponse}"/> class.
    /// </summary>
    /// <param name="cache">The memory cache used to store responses.</param>
    /// <param name="options">The pipeline options containing the default cache duration.</param>
    /// <param name="logger">The logger used to write cache logs.</param>
    public CachingBehavior(
        IMemoryCache cache,
        PipelineOptions options,
        ILogger<CachingBehavior<TRequest, TResponse>> logger)
    {
        _cache = cache;
        _options = options;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (request is ICacheableRequest cacheable &&
            _cache.TryGetValue<TResponse>(cacheable.CacheKey, out var cachedResponse))
        {
            _logger.LogDebug(
                "Cache hit for {RequestName} with key {CacheKey}",
                typeof(TRequest).Name,
                cacheable.CacheKey);

            return cachedResponse!;
        }

        var response = await next();

        if (request is ICacheableRequest cacheableRequest)
        {
            var duration = cacheableRequest.CacheDuration ?? _options.DefaultCacheDuration;

            _cache.Set(cacheableRequest.CacheKey, response, duration);

            _logger.LogDebug(
                "Cached response for {RequestName} with key {CacheKey} for {CacheDuration}",
                typeof(TRequest).Name,
                cacheableRequest.CacheKey,
                duration);
        }

        if (request is ICacheInvalidatorRequest invalidator)
        {
            foreach (var key in invalidator.CacheKeysToInvalidate)
            {
                _cache.Remove(key);
            }

            _logger.LogDebug(
                "Invalidated {KeyCount} cache keys after {RequestName}",
                invalidator.CacheKeysToInvalidate.Count,
                typeof(TRequest).Name);
        }

        return response;
    }
}
