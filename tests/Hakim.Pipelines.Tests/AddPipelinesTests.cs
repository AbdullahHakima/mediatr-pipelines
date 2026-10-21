using FluentValidation;
using Hakim.Pipelines.Abstractions;
using Hakim.Pipelines.Results;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Hakim.Pipelines.Tests;

public sealed class HandlerTracker
{
    public int PingCount { get; set; }

    public int GetProductCount { get; set; }
}

public sealed class PingRequestHandler : IRequestHandler<PingRequest, string>
{
    private readonly HandlerTracker _tracker;

    public PingRequestHandler(HandlerTracker tracker) => _tracker = tracker;

    public Task<string> Handle(PingRequest request, CancellationToken cancellationToken)
    {
        _tracker.PingCount++;
        return Task.FromResult($"pong:{request.Message}");
    }
}

public sealed record CreateThingCommand(string Name) : IRequest<Result<int>>, ITransactionalRequest;

public sealed class CreateThingCommandHandler : IRequestHandler<CreateThingCommand, Result<int>>
{
    public Task<Result<int>> Handle(CreateThingCommand request, CancellationToken cancellationToken)
    {
        return Task.FromResult(Result.Success(42));
    }
}

public sealed class CreateThingCommandValidator : AbstractValidator<CreateThingCommand>
{
    public CreateThingCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithErrorCode("Name.Required");
    }
}

public sealed class GetProductQueryHandler : IRequestHandler<GetProductQuery, string>
{
    private readonly HandlerTracker _tracker;

    public GetProductQueryHandler(HandlerTracker tracker) => _tracker = tracker;

    public Task<string> Handle(GetProductQuery request, CancellationToken cancellationToken)
    {
        _tracker.GetProductCount++;
        return Task.FromResult($"product-{request.Id} (call {_tracker.GetProductCount})");
    }
}

public sealed class AddPipelinesTests
{
    private static ServiceProvider BuildProvider(
        Action<PipelineOptions>? configure = null,
        Action<ServiceCollection>? registerExtras = null)
    {
        var services = new ServiceCollection();
        services.AddSingleton<HandlerTracker>();
        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(AddPipelinesTests).Assembly));
        services.AddTransient<IValidator<PingRequest>, PingRequestValidator>();
        services.AddTransient<IValidator<CreateThingCommand>, CreateThingCommandValidator>();
        services.AddPipelines(configure);
        registerExtras?.Invoke(services);

        return services.BuildServiceProvider();
    }

    [Fact]
    public async Task DefaultPipelines_ValidRequest_ReachesHandler()
    {
        await using var provider = BuildProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        var response = await mediator.Send(new PingRequest("hello"));

        Assert.Equal("pong:hello", response);
    }

    [Fact]
    public async Task DefaultPipelines_InvalidRequest_ThrowsValidationException()
    {
        await using var provider = BuildProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        await Assert.ThrowsAsync<ValidationException>(() => mediator.Send(new PingRequest("")));
    }

    [Fact]
    public async Task ResultValidationFlavor_InvalidRequest_ReturnsFailureWithoutThrowing()
    {
        await using var provider = BuildProvider(options =>
        {
            options.EnableValidation = false;
            options.EnableResultValidation = true;
        });
        var mediator = provider.GetRequiredService<IMediator>();

        var failure = await mediator.Send(new CreateThingCommand(""));
        var success = await mediator.Send(new CreateThingCommand("thing"));

        Assert.True(failure.IsFailure);
        Assert.Contains(failure.Errors, error => error.Code == "Name.Required");
        Assert.True(success.IsSuccess);
        Assert.Equal(42, success.Value);
    }

    [Fact]
    public async Task DefaultPipelines_CacheableQuery_SecondSendIsServedFromCache()
    {
        await using var provider = BuildProvider();
        var mediator = provider.GetRequiredService<IMediator>();
        var tracker = provider.GetRequiredService<HandlerTracker>();

        var first = await mediator.Send(new GetProductQuery(5));
        var second = await mediator.Send(new GetProductQuery(5));

        Assert.Equal(first, second);
        Assert.Equal(1, tracker.GetProductCount);
    }

    [Fact]
    public async Task DefaultPipelines_TransactionalCommand_UsesRegisteredUnitOfWork()
    {
        var unitOfWork = new FakeUnitOfWork();
        await using var provider = BuildProvider(
            registerExtras: services => services.AddSingleton<IUnitOfWork>(unitOfWork));
        var mediator = provider.GetRequiredService<IMediator>();

        var result = await mediator.Send(new CreateThingCommand("thing"));

        Assert.True(result.IsSuccess);
        Assert.Equal(["Begin", "Commit"], unitOfWork.Calls);
    }

    [Fact]
    public async Task PipelinesCanBeDisabled_InvalidRequest_ReachesHandlerWhenValidationOff()
    {
        await using var provider = BuildProvider(options =>
        {
            options.EnableValidation = false;
        });
        var mediator = provider.GetRequiredService<IMediator>();

        var response = await mediator.Send(new PingRequest(""));

        Assert.Equal("pong:", response);
    }
}
