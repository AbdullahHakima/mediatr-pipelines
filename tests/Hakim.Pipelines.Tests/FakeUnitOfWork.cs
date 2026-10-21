using Hakim.Pipelines.Abstractions;

namespace Hakim.Pipelines.Tests;

/// <summary>
/// An <see cref="IUnitOfWork"/> that records the calls it receives.
/// </summary>
public sealed class FakeUnitOfWork : IUnitOfWork
{
    public List<string> Calls { get; } = [];

    public Task BeginAsync(CancellationToken cancellationToken = default)
    {
        Calls.Add("Begin");
        return Task.CompletedTask;
    }

    public Task CommitAsync(CancellationToken cancellationToken = default)
    {
        Calls.Add("Commit");
        return Task.CompletedTask;
    }

    public Task RollbackAsync(CancellationToken cancellationToken = default)
    {
        Calls.Add("Rollback");
        return Task.CompletedTask;
    }
}
