using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Hakim.Pipelines.Behaviors;

/// <summary>
/// Measures how long each request takes and logs a warning when handling
/// exceeds the configured performance threshold
/// (see <see cref="PipelineOptions.PerformanceThreshold"/>).
/// </summary>
/// <typeparam name="TRequest">The type of the request.</typeparam>
/// <typeparam name="TResponse">The type of the response.</typeparam>
public sealed class PerformanceBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ILogger<PerformanceBehavior<TRequest, TResponse>> _logger;
    private readonly PipelineOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="PerformanceBehavior{TRequest, TResponse}"/> class.
    /// </summary>
    /// <param name="logger">The logger used to write performance warnings.</param>
    /// <param name="options">The pipeline options containing the performance threshold.</param>
    public PerformanceBehavior(
        ILogger<PerformanceBehavior<TRequest, TResponse>> logger,
        PipelineOptions options)
    {
        _logger = logger;
        _options = options;
    }

    /// <inheritdoc />
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        var response = await next();
        stopwatch.Stop();

        if (stopwatch.Elapsed > _options.PerformanceThreshold)
        {
            _logger.LogWarning(
                "Long running request: {RequestName} took {ElapsedMilliseconds} ms, " +
                "which exceeds the threshold of {ThresholdMilliseconds} ms",
                typeof(TRequest).Name,
                stopwatch.ElapsedMilliseconds,
                (long)_options.PerformanceThreshold.TotalMilliseconds);
        }

        return response;
    }
}
