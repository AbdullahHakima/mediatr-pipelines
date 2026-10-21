# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.1.0] - 2026-10-21

### Added

- `LoggingBehavior` — structured request logging with elapsed time.
- `PerformanceBehavior` — warning when a request exceeds a configurable threshold (default 500 ms).
- `ValidationBehavior` — runs all FluentValidation validators and throws `ValidationException` on failure.
- `ResultValidationBehavior` — returns validation failures as a failed `Result` / `Result<T>` instead of throwing.
- Minimal `Result`, `Result<T>`, and `Error` types in `Hakim.Pipelines.Results`.
- `UnhandledExceptionBehavior` — logs unhandled exceptions and rethrows.
- `TransactionBehavior` — wraps `ITransactionalRequest` requests in an `IUnitOfWork` (begin/commit/rollback).
- `CachingBehavior` — caches `ICacheableRequest` responses in `IMemoryCache` and evicts keys for `ICacheInvalidatorRequest`.
- `AddPipelines(...)` registration extension with per-behavior enable/disable options via `PipelineOptions`.
- Console sample demonstrating validation, transactions, and caching end to end.
