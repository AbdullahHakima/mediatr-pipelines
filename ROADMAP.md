# Hakim.Pipelines Roadmap

> Living document — updated with every release. Ideas and contributions are
> welcome; see [CONTRIBUTING.md](CONTRIBUTING.md).

## v0.1.0 — Shipped

- `LoggingBehavior` — structured request logging with timing
- `PerformanceBehavior` — warning over a configurable threshold (500 ms default)
- `ValidationBehavior` — FluentValidation, throws `ValidationException`
- `ResultValidationBehavior` — FluentValidation returning `Result` /
  `Result<T>` failures without throwing
- `TransactionBehavior` — `ITransactionalRequest` + `IUnitOfWork` abstraction
- `CachingBehavior` — `ICacheableRequest` over `IMemoryCache`, with
  `ICacheInvalidatorRequest` eviction
- `UnhandledExceptionBehavior` — log-and-rethrow safety net
- Minimal `Result` / `Result<T>` / `Error` types
- One-line registration: `services.AddPipelines(...)` with per-behavior toggles

## v0.2 — Planned

- **Hakim.Pipelines.EntityFrameworkCore** — a ready-made `IUnitOfWork`
  implementation over EF Core `DbContext` (begin/commit/rollback wired to
  the database transaction), so `TransactionBehavior` works out of the box
- **OpenTelemetry behavior** — an `ActivitySource`-based behavior emitting
  spans per request (name, duration, outcome), following OpenTelemetry
  semantic conventions, no vendor dependency
- **Retry behavior** — opt-in per request (`IRetryableRequest`) with
  configurable attempts/backoff for transient failures

## v0.3 — Planned

- **Mediator alternatives** — support notes and, where the abstraction
  allows, compatibility guidance for projects migrating between MediatR and
  the open-source Mediator (martinothamar) or Wolverine
- **Notification pipeline helpers** — behaviors that apply to
  `INotification` handlers as well as requests
- **Analyzer pack (stretch)** — Roslyn analyzers that flag requests missing
  validators or transactional markers at build time
