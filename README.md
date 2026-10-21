# Hakim.Pipelines

**The MediatR pipeline behaviors every project rewrites — written once, tested, and configurable.**

If you use MediatR with CQRS, you have probably copied the same handful of pipeline behaviors from project to project: request logging, a slow-request warning, FluentValidation wiring, a transaction wrapper, a response cache. Hakim.Pipelines packages those behaviors as a small, dependency-light library so you can stop rewriting them.

It also ships a second validation flavor most behavior packs don't have: validation failures returned as a `Result` instead of thrown as an exception — because invalid user input is an expected outcome, not an exceptional one.

> **Status:** 0.1.0 is the first release. The package is not on NuGet yet; the install command below goes live with the first publish. Until then, clone the repo and reference the project.

## Install

```bash
dotnet add package Hakim.Pipelines
```

## Quickstart

Register MediatR as usual, then add the pipelines:

```csharp
using MediatR;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssemblyContaining<Program>());

builder.Services.AddPipelines();

builder.Services.AddValidatorsFromAssemblyContaining<Program>(); // FluentValidation
```

That is enough to get logging, performance warnings, validation, transactions, and caching. Every behavior only engages when it has something to do: transactions wrap requests marked `ITransactionalRequest`, caching applies to requests marked `ICacheableRequest`, and validation runs only when a validator is registered for the request.

## The behaviors

### Logging

Logs when a request starts and when it finishes, with the elapsed time.

```
info: Handling CreateProductCommand
info: Handled CreateProductCommand in 12 ms
```

### Performance

Logs a warning when a request takes longer than the threshold (500 ms by default):

```
warn: Long running request: GetSalesReportQuery took 1830 ms, which exceeds the threshold of 500 ms
```

### Validation (throwing)

Runs all FluentValidation validators registered for the request. On failure it throws a `ValidationException` and the handler never runs — the classic MediatR template behavior.

```csharp
public sealed class CreateProductCommandValidator : AbstractValidator<CreateProductCommand>
{
    public CreateProductCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty();
        RuleFor(x => x.Price).GreaterThan(0);
    }
}
```

### Validation (Result-based) — the one that doesn't throw

Swap one line of configuration and validation failures come back as values:

```csharp
builder.Services.AddPipelines(options =>
{
    options.EnableValidation = false;      // turn the throwing behavior off
    options.EnableResultValidation = true; // turn the Result behavior on
});
```

Your requests return the package's minimal `Result` / `Result<T>` type:

```csharp
public sealed record CreateProductCommand(string Name, decimal Price)
    : IRequest<Result<int>>, ITransactionalRequest;

public sealed class CreateProductCommandHandler : IRequestHandler<CreateProductCommand, Result<int>>
{
    public async Task<Result<int>> Handle(CreateProductCommand request, CancellationToken ct)
    {
        // The request is guaranteed valid here — invalid requests never reach the handler.
        var id = await repository.AddAsync(request, ct);
        return Result.Success(id);
    }
}

// At the caller:
Result<int> result = await mediator.Send(new CreateProductCommand("", -5));

result.IsFailure; // true
result.Errors;    // [ { Code = "NotEmptyValidator", Message = "'Name' must not be empty." }, ... ]
```

No exception is thrown for bad input, and the handler is never executed. The `Result` type is deliberately small: `IsSuccess` / `IsFailure`, `Errors`, `Value`, and static `Success` / `Failure` factories. Bring your own if you already have one — the throwing behavior works with any response type.

### Transactions

Mark a request with `ITransactionalRequest` and implement `IUnitOfWork` over your DbContext (or session) once:

```csharp
public sealed class EfUnitOfWork(AppDbContext db) : IUnitOfWork
{
    public async Task BeginAsync(CancellationToken ct = default)
        => await db.Database.BeginTransactionAsync(ct);

    public async Task CommitAsync(CancellationToken ct = default)
        => await db.Database.CurrentTransaction!.CommitAsync(ct);

    public async Task RollbackAsync(CancellationToken ct = default)
        => await db.Database.CurrentTransaction!.RollbackAsync(ct);
}
```

Register it (`services.AddScoped<IUnitOfWork, EfUnitOfWork>()`) and transactional requests get begin → commit on success, begin → rollback on exception. If no `IUnitOfWork` is registered, the behavior quietly does nothing.

### Caching

Implement `ICacheableRequest` on a query:

```csharp
public sealed record GetProductQuery(int Id) : IRequest<ProductDto>, ICacheableRequest
{
    public string CacheKey => $"products:{Id}";
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(10); // null = default (5 min)
}
```

The first call runs the handler and stores the response in `IMemoryCache`; later calls with the same key return the stored response. Commands evict entries by implementing `ICacheInvalidatorRequest`:

```csharp
public sealed record UpdateProductPriceCommand(int Id, decimal Price)
    : IRequest, ICacheInvalidatorRequest
{
    public IReadOnlyList<string> CacheKeysToInvalidate => [$"products:{Id}"];
}
```

Keys are evicted only after the handler completes successfully.

### Unhandled exceptions

Logs any exception that escapes the pipeline (with the request name) and rethrows it, so your normal error handling still applies. It is registered first, so it wraps all the other behaviors.

## Configuration

All configuration goes through `AddPipelines(options => ...)`.

| Option | Default | Description |
| --- | --- | --- |
| `EnableUnhandledException` | `true` | Log and rethrow unhandled exceptions. |
| `EnableLogging` | `true` | Log request start/completion with timing. |
| `EnablePerformance` | `true` | Warn on slow requests. |
| `EnableValidation` | `true` | Throwing FluentValidation behavior. |
| `EnableResultValidation` | `false` | Result-based validation behavior. Enable either this or `EnableValidation`, not both. |
| `EnableTransaction` | `true` | Unit-of-work wrapping for `ITransactionalRequest`. |
| `EnableCaching` | `true` | Response caching for `ICacheableRequest` / eviction for `ICacheInvalidatorRequest`. |
| `PerformanceThreshold` | 500 ms | Requests slower than this log a warning. |
| `DefaultCacheDuration` | 5 min | Cache duration for requests that don't specify one. |

Behaviors execute in registration order: unhandled exception → logging → performance → validation → transaction → caching → your handler.

## Sample

`samples/Hakim.Pipelines.Sample` is a small console app that runs the whole pipeline end to end: a validated, transactional create command (once valid, once invalid against the Result-based validation), a cached query called twice, and a price update that invalidates the cache.

```bash
dotnet run --project samples/Hakim.Pipelines.Sample
```

## Requirements

- .NET 8
- MediatR 12.x (the last open-source MediatR release line)
- FluentValidation 11.x

## License

MIT — see [LICENSE](LICENSE). Copyright (c) 2026 Abdullah Hakim.
