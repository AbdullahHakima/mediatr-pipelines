namespace Hakim.Pipelines.Abstractions;

/// <summary>
/// Abstraction over a transactional unit of work (for example, an EF Core
/// <c>DbContext</c> transaction or a session). Implement this interface in
/// your infrastructure layer and register it in dependency injection to let
/// the transaction behavior manage request transactions.
/// </summary>
public interface IUnitOfWork
{
    /// <summary>
    /// Begins a new unit of work.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task BeginAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Commits all changes made inside the current unit of work.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task CommitAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Discards all changes made inside the current unit of work.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task RollbackAsync(CancellationToken cancellationToken = default);
}
