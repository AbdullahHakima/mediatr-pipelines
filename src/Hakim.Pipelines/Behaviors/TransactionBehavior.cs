using Hakim.Pipelines.Abstractions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Hakim.Pipelines.Behaviors;

/// <summary>
/// Wraps requests that implement <see cref="ITransactionalRequest"/> in a
/// unit of work: the unit of work begins before the handler runs, commits
/// when the handler succeeds, and rolls back when the handler throws.
/// Non-transactional requests pass through untouched.
/// </summary>
/// <typeparam name="TRequest">The type of the request.</typeparam>
/// <typeparam name="TResponse">The type of the response.</typeparam>
/// <remarks>
/// Requires an <see cref="IUnitOfWork"/> implementation to be registered in
/// dependency injection. When none is registered, the behavior is a no-op.
/// </remarks>
public sealed class TransactionBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IUnitOfWork? _unitOfWork;
    private readonly ILogger<TransactionBehavior<TRequest, TResponse>> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="TransactionBehavior{TRequest, TResponse}"/> class.
    /// </summary>
    /// <param name="logger">The logger used to write transaction logs.</param>
    /// <param name="unitOfWork">
    /// The unit of work to manage. Optional: when no implementation is
    /// registered in dependency injection, the behavior passes requests through.
    /// </param>
    public TransactionBehavior(
        ILogger<TransactionBehavior<TRequest, TResponse>> logger,
        IUnitOfWork? unitOfWork = null)
    {
        _logger = logger;
        _unitOfWork = unitOfWork;
    }

    /// <inheritdoc />
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (request is not ITransactionalRequest || _unitOfWork is null)
        {
            return await next();
        }

        _logger.LogDebug("Beginning unit of work for {RequestName}", typeof(TRequest).Name);

        await _unitOfWork.BeginAsync(cancellationToken);

        try
        {
            var response = await next();
            await _unitOfWork.CommitAsync(cancellationToken);

            _logger.LogDebug("Committed unit of work for {RequestName}", typeof(TRequest).Name);

            return response;
        }
        catch
        {
            await _unitOfWork.RollbackAsync(cancellationToken);

            _logger.LogDebug("Rolled back unit of work for {RequestName}", typeof(TRequest).Name);

            throw;
        }
    }
}
