namespace Hakim.Pipelines;

/// <summary>
/// Configures which pipeline behaviors <c>AddPipelines</c> registers and how
/// they behave. Pass an options delegate to <c>AddPipelines</c> to change the
/// defaults.
/// </summary>
public sealed class PipelineOptions
{
    /// <summary>
    /// Gets or sets whether the unhandled exception behavior is registered.
    /// Default: <see langword="true"/>.
    /// </summary>
    public bool EnableUnhandledException { get; set; } = true;

    /// <summary>
    /// Gets or sets whether the logging behavior is registered.
    /// Default: <see langword="true"/>.
    /// </summary>
    public bool EnableLogging { get; set; } = true;

    /// <summary>
    /// Gets or sets whether the performance behavior is registered.
    /// Default: <see langword="true"/>.
    /// </summary>
    public bool EnablePerformance { get; set; } = true;

    /// <summary>
    /// Gets or sets whether the throwing validation behavior is registered.
    /// Default: <see langword="true"/>.
    /// Enable either this or <see cref="EnableResultValidation"/>, not both.
    /// </summary>
    public bool EnableValidation { get; set; } = true;

    /// <summary>
    /// Gets or sets whether the result-based validation behavior is registered.
    /// Default: <see langword="false"/>.
    /// Enable either this or <see cref="EnableValidation"/>, not both.
    /// </summary>
    public bool EnableResultValidation { get; set; }

    /// <summary>
    /// Gets or sets whether the transaction behavior is registered.
    /// Default: <see langword="true"/>. The behavior only engages for
    /// requests implementing <c>ITransactionalRequest</c> when an
    /// <c>IUnitOfWork</c> is registered.
    /// </summary>
    public bool EnableTransaction { get; set; } = true;

    /// <summary>
    /// Gets or sets whether the caching behavior is registered.
    /// Default: <see langword="true"/>. The behavior only engages for
    /// requests implementing <c>ICacheableRequest</c> or
    /// <c>ICacheInvalidatorRequest</c>.
    /// </summary>
    public bool EnableCaching { get; set; } = true;

    /// <summary>
    /// Gets or sets the duration above which the performance behavior logs a
    /// warning. Default: 500 milliseconds.
    /// </summary>
    public TimeSpan PerformanceThreshold { get; set; } = TimeSpan.FromMilliseconds(500);

    /// <summary>
    /// Gets or sets the cache duration used for cacheable requests that do
    /// not specify their own. Default: 5 minutes.
    /// </summary>
    public TimeSpan DefaultCacheDuration { get; set; } = TimeSpan.FromMinutes(5);
}
