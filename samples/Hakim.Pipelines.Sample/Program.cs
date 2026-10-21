using FluentValidation;
using Hakim.Pipelines;
using Hakim.Pipelines.Abstractions;
using Hakim.Pipelines.Results;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

// ---------------------------------------------------------------------------
// Hakim.Pipelines sample
//
// Wires MediatR + AddPipelines with the Result-based validation flavor,
// then runs five sends end to end:
//   1. a valid, transactional CreateProductCommand
//   2. an invalid CreateProductCommand (validation failure returned, not thrown)
//   3. a cached GetProductQuery, sent twice (handler runs once)
//   4. an UpdateProductPriceCommand that invalidates the cache
//   5. the same query again (handler runs again, new price)
// ---------------------------------------------------------------------------

var services = new ServiceCollection();

services.AddLogging(builder => builder
    .AddConsole()
    .SetMinimumLevel(LogLevel.Debug));

services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<Program>());

services.AddPipelines(options =>
{
    options.EnableValidation = false;       // throwing flavor off...
    options.EnableResultValidation = true;  // ...Result flavor on
    options.PerformanceThreshold = TimeSpan.FromMilliseconds(250);
});

services.AddSingleton<IUnitOfWork, ConsoleUnitOfWork>();
services.AddSingleton<ProductStore>();
services.AddTransient<IValidator<CreateProductCommand>, CreateProductCommandValidator>();

await using var provider = services.BuildServiceProvider();
var mediator = provider.GetRequiredService<IMediator>();

Console.WriteLine();
Console.WriteLine("== 1. Create a valid product (transactional) ==");
var created = await mediator.Send(new CreateProductCommand("Industrial Sewing Machine X200", 12500m));
Console.WriteLine(created.IsSuccess
    ? $"Created product with id {created.Value}."
    : $"Failed: {string.Join(", ", created.Errors)}");

Console.WriteLine();
Console.WriteLine("== 2. Create an invalid product (empty name, negative price) ==");
var invalid = await mediator.Send(new CreateProductCommand("", -5m));
Console.WriteLine(invalid.IsFailure
    ? "Returned a failure Result — no exception thrown, handler never ran."
    : "Unexpectedly succeeded.");
foreach (var error in invalid.Errors)
{
    Console.WriteLine($"   [{error.Code}] {error.Message}");
}

Console.WriteLine();
Console.WriteLine("== 3. Read the product twice (second read is a cache hit) ==");
var first = await mediator.Send(new GetProductQuery(1));
var second = await mediator.Send(new GetProductQuery(1));
Console.WriteLine($"First read:  {first.Value}");
Console.WriteLine($"Second read: {second.Value}");

Console.WriteLine();
Console.WriteLine("== 4. Update the price (invalidates the cached product) ==");
var updated = await mediator.Send(new UpdateProductPriceCommand(1, 11999m));
Console.WriteLine(updated.IsSuccess ? "Price updated." : "Update failed.");

Console.WriteLine();
Console.WriteLine("== 5. Read again (cache was invalidated, handler runs) ==");
var third = await mediator.Send(new GetProductQuery(1));
Console.WriteLine($"Third read:  {third.Value}");

Console.WriteLine();
Console.WriteLine("Done.");

// ---------------------------------------------------------------------------
// Domain for the sample
// ---------------------------------------------------------------------------

public sealed record ProductDto(int Id, string Name, decimal Price);

public sealed class ProductStore
{
    private readonly Dictionary<int, ProductDto> _products = new();
    private int _nextId;

    public int Add(string name, decimal price)
    {
        var id = ++_nextId;
        _products[id] = new ProductDto(id, name, price);
        return id;
    }

    public ProductDto? Get(int id) => _products.GetValueOrDefault(id);

    public void UpdatePrice(int id, decimal price)
    {
        if (_products.TryGetValue(id, out var product))
        {
            _products[id] = product with { Price = price };
        }
    }
}

public sealed class ConsoleUnitOfWork : IUnitOfWork
{
    public Task BeginAsync(CancellationToken cancellationToken = default)
    {
        Console.WriteLine("   [unit of work] Begin");
        return Task.CompletedTask;
    }

    public Task CommitAsync(CancellationToken cancellationToken = default)
    {
        Console.WriteLine("   [unit of work] Commit");
        return Task.CompletedTask;
    }

    public Task RollbackAsync(CancellationToken cancellationToken = default)
    {
        Console.WriteLine("   [unit of work] Rollback");
        return Task.CompletedTask;
    }
}

// ---------------------------------------------------------------------------
// Requests, handlers, validators
// ---------------------------------------------------------------------------

public sealed record CreateProductCommand(string Name, decimal Price)
    : IRequest<Result<int>>, ITransactionalRequest;

public sealed class CreateProductCommandValidator : AbstractValidator<CreateProductCommand>
{
    public CreateProductCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty();
        RuleFor(x => x.Price).GreaterThan(0);
    }
}

public sealed class CreateProductCommandHandler(ProductStore store)
    : IRequestHandler<CreateProductCommand, Result<int>>
{
    public Task<Result<int>> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        var id = store.Add(request.Name, request.Price);
        return Task.FromResult(Result.Success(id));
    }
}

public sealed record GetProductQuery(int Id) : IRequest<Result<ProductDto>>, ICacheableRequest
{
    public string CacheKey => $"products:{Id}";

    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(10);
}

public sealed class GetProductQueryHandler(ProductStore store)
    : IRequestHandler<GetProductQuery, Result<ProductDto>>
{
    public Task<Result<ProductDto>> Handle(GetProductQuery request, CancellationToken cancellationToken)
    {
        Console.WriteLine($"   [handler] GetProductQuery executed for id {request.Id}");

        var product = store.Get(request.Id);

        return Task.FromResult(product is null
            ? Result.Failure<ProductDto>(new Error("Product.NotFound", $"Product {request.Id} was not found."))
            : Result.Success(product));
    }
}

public sealed record UpdateProductPriceCommand(int Id, decimal Price)
    : IRequest<Result>, ICacheInvalidatorRequest
{
    public IReadOnlyList<string> CacheKeysToInvalidate => [$"products:{Id}"];
}

public sealed class UpdateProductPriceCommandHandler(ProductStore store)
    : IRequestHandler<UpdateProductPriceCommand, Result>
{
    public Task<Result> Handle(UpdateProductPriceCommand request, CancellationToken cancellationToken)
    {
        store.UpdatePrice(request.Id, request.Price);
        return Task.FromResult(Result.Success());
    }
}
