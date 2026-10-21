using Hakim.Pipelines.Abstractions;
using Hakim.Pipelines.Behaviors;
using MediatR;
using Microsoft.Extensions.Caching.Memory;

namespace Hakim.Pipelines.Tests;

public sealed record CreateOrderCommand : IRequest<string>, ITransactionalRequest;

public sealed record GetProductQuery(int Id) : IRequest<string>, ICacheableRequest
{
    public string CacheKey => $"products:{Id}";

    public TimeSpan? CacheDuration => null;
}

public sealed record UpdateProductCommand(int Id) : IRequest<string>, ICacheInvalidatorRequest
{
    public IReadOnlyList<string> CacheKeysToInvalidate => [$"products:{Id}"];
}

public sealed class TransactionBehaviorTests
{
    private static TransactionBehavior<CreateOrderCommand, string> CreateBehavior(
        FakeUnitOfWork unitOfWork)
    {
        return new TransactionBehavior<CreateOrderCommand, string>(
            new TestLogger<TransactionBehavior<CreateOrderCommand, string>>(),
            unitOfWork);
    }

    [Fact]
    public async Task Handle_TransactionalRequest_Success_BeginsAndCommits()
    {
        var unitOfWork = new FakeUnitOfWork();
        var behavior = CreateBehavior(unitOfWork);

        var response = await behavior.Handle(
            new CreateOrderCommand(),
            () => Task.FromResult("created"),
            CancellationToken.None);

        Assert.Equal("created", response);
        Assert.Equal(["Begin", "Commit"], unitOfWork.Calls);
    }

    [Fact]
    public async Task Handle_TransactionalRequest_HandlerThrows_BeginsAndRollsBack()
    {
        var unitOfWork = new FakeUnitOfWork();
        var behavior = CreateBehavior(unitOfWork);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            behavior.Handle(
                new CreateOrderCommand(),
                () => throw new InvalidOperationException("db down"),
                CancellationToken.None));

        Assert.Equal(["Begin", "Rollback"], unitOfWork.Calls);
    }

    [Fact]
    public async Task Handle_NonTransactionalRequest_DoesNotTouchUnitOfWork()
    {
        var unitOfWork = new FakeUnitOfWork();
        var behavior = new TransactionBehavior<PingRequest, string>(
            new TestLogger<TransactionBehavior<PingRequest, string>>(),
            unitOfWork);

        var response = await behavior.Handle(
            new PingRequest("hello"),
            () => Task.FromResult("pong"),
            CancellationToken.None);

        Assert.Equal("pong", response);
        Assert.Empty(unitOfWork.Calls);
    }

    [Fact]
    public async Task Handle_NoUnitOfWorkRegistered_PassesThrough()
    {
        var behavior = new TransactionBehavior<CreateOrderCommand, string>(
            new TestLogger<TransactionBehavior<CreateOrderCommand, string>>(),
            unitOfWork: null);

        var response = await behavior.Handle(
            new CreateOrderCommand(),
            () => Task.FromResult("created"),
            CancellationToken.None);

        Assert.Equal("created", response);
    }
}

public sealed class CachingBehaviorTests
{
    private static (CachingBehavior<GetProductQuery, string> Behavior, MemoryCache Cache) Create(
        PipelineOptions? options = null)
    {
        var cache = new MemoryCache(new MemoryCacheOptions());
        var behavior = new CachingBehavior<GetProductQuery, string>(
            cache,
            options ?? new PipelineOptions(),
            new TestLogger<CachingBehavior<GetProductQuery, string>>());

        return (behavior, cache);
    }

    [Fact]
    public async Task Handle_FirstCall_ExecutesHandler_SecondCall_ReturnsCachedResponse()
    {
        var (behavior, _) = Create();
        var handlerCalls = 0;

        Task<string> Next()
        {
            handlerCalls++;
            return Task.FromResult($"product (call {handlerCalls})");
        }

        var first = await behavior.Handle(new GetProductQuery(1), Next, CancellationToken.None);
        var second = await behavior.Handle(new GetProductQuery(1), Next, CancellationToken.None);

        Assert.Equal(1, handlerCalls);
        Assert.Equal(first, second);
    }

    [Fact]
    public async Task Handle_DifferentKeys_ExecuteHandlerSeparately()
    {
        var (behavior, _) = Create();
        var handlerCalls = 0;

        Task<string> Next()
        {
            handlerCalls++;
            return Task.FromResult($"product (call {handlerCalls})");
        }

        await behavior.Handle(new GetProductQuery(1), Next, CancellationToken.None);
        await behavior.Handle(new GetProductQuery(2), Next, CancellationToken.None);

        Assert.Equal(2, handlerCalls);
    }

    [Fact]
    public async Task Handle_InvalidatorRequest_EvictsCachedResponse()
    {
        var (behavior, cache) = Create();
        var invalidatorBehavior = new CachingBehavior<UpdateProductCommand, string>(
            cache,
            new PipelineOptions(),
            new TestLogger<CachingBehavior<UpdateProductCommand, string>>());
        var handlerCalls = 0;

        Task<string> Next()
        {
            handlerCalls++;
            return Task.FromResult($"product (call {handlerCalls})");
        }

        await behavior.Handle(new GetProductQuery(1), Next, CancellationToken.None);
        await invalidatorBehavior.Handle(
            new UpdateProductCommand(1),
            () => Task.FromResult("updated"),
            CancellationToken.None);
        var afterInvalidation = await behavior.Handle(
            new GetProductQuery(1), Next, CancellationToken.None);

        Assert.Equal(2, handlerCalls);
        Assert.Equal("product (call 2)", afterInvalidation);
    }

    [Fact]
    public async Task Handle_NonCacheableRequest_AlwaysExecutesHandler()
    {
        var cache = new MemoryCache(new MemoryCacheOptions());
        var behavior = new CachingBehavior<PingRequest, string>(
            cache,
            new PipelineOptions(),
            new TestLogger<CachingBehavior<PingRequest, string>>());
        var handlerCalls = 0;

        Task<string> Next()
        {
            handlerCalls++;
            return Task.FromResult("pong");
        }

        await behavior.Handle(new PingRequest("hello"), Next, CancellationToken.None);
        await behavior.Handle(new PingRequest("hello"), Next, CancellationToken.None);

        Assert.Equal(2, handlerCalls);
    }
}
