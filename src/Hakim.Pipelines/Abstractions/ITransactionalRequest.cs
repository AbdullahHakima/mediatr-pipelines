namespace Hakim.Pipelines.Abstractions;

/// <summary>
/// Marker interface for requests that must execute inside a unit of work.
/// When the transaction behavior is enabled and an <see cref="IUnitOfWork"/>
/// is registered, requests implementing this interface are wrapped in a
/// begin/commit (or rollback on exception) cycle.
/// </summary>
public interface ITransactionalRequest
{
}
