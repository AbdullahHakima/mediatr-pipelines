using Hakim.Pipelines;
using Hakim.Pipelines.Behaviors;
using MediatR;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

// Placed in the Microsoft.Extensions.DependencyInjection namespace so the
// extension is discovered wherever IServiceCollection is used.
namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Registers the Hakim.Pipelines MediatR pipeline behaviors with dependency injection.
/// </summary>
public static class PipelineServiceCollectionExtensions
{
    /// <summary>
    /// Adds the Hakim.Pipelines behaviors to the MediatR pipeline.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">
    /// An optional delegate to enable or disable individual behaviors and
    /// tune settings such as the performance threshold. When omitted, the
    /// default options are used (all behaviors enabled except result-based
    /// validation).
    /// </param>
    /// <returns>The same service collection, for chaining.</returns>
    /// <example>
    /// <code>
    /// services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining&lt;Program&gt;());
    /// services.AddPipelines(options =>
    /// {
    ///     options.EnableValidation = false;       // use result-based validation instead
    ///     options.EnableResultValidation = true;
    ///     options.PerformanceThreshold = TimeSpan.FromSeconds(1);
    /// });
    /// </code>
    /// </example>
    /// <remarks>
    /// MediatR itself must be registered separately (for example with
    /// <c>AddMediatR</c>). Behaviors run in this order: unhandled exception,
    /// logging, performance, validation, transaction, caching. When caching is
    /// enabled and no <c>IMemoryCache</c> is registered yet, an in-memory
    /// cache is added.
    /// </remarks>
    public static IServiceCollection AddPipelines(
        this IServiceCollection services,
        Action<PipelineOptions>? configure = null)
    {
        var options = new PipelineOptions();
        configure?.Invoke(options);

        services.AddSingleton(options);

        // Behaviors resolve ILogger<T>; make sure logging abstractions are
        // available even in hosts that did not call AddLogging themselves.
        if (!services.Any(descriptor => descriptor.ServiceType == typeof(ILoggerFactory)))
        {
            services.AddLogging();
        }

        if (options.EnableUnhandledException)
        {
            services.AddTransient(
                typeof(IPipelineBehavior<,>),
                typeof(UnhandledExceptionBehavior<,>));
        }

        if (options.EnableLogging)
        {
            services.AddTransient(
                typeof(IPipelineBehavior<,>),
                typeof(LoggingBehavior<,>));
        }

        if (options.EnablePerformance)
        {
            services.AddTransient(
                typeof(IPipelineBehavior<,>),
                typeof(PerformanceBehavior<,>));
        }

        if (options.EnableValidation)
        {
            services.AddTransient(
                typeof(IPipelineBehavior<,>),
                typeof(ValidationBehavior<,>));
        }

        if (options.EnableResultValidation)
        {
            services.AddTransient(
                typeof(IPipelineBehavior<,>),
                typeof(ResultValidationBehavior<,>));
        }

        if (options.EnableTransaction)
        {
            services.AddTransient(
                typeof(IPipelineBehavior<,>),
                typeof(TransactionBehavior<,>));
        }

        if (options.EnableCaching)
        {
            services.TryAddSingleton<Microsoft.Extensions.Caching.Memory.IMemoryCache>(
                _ => new Microsoft.Extensions.Caching.Memory.MemoryCache(
                    new Microsoft.Extensions.Caching.Memory.MemoryCacheOptions()));

            services.AddTransient(
                typeof(IPipelineBehavior<,>),
                typeof(CachingBehavior<,>));
        }

        return services;
    }
}
