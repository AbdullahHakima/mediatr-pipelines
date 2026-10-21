# Contributing to Hakim.Pipelines

Contributions are welcome — bug reports, behavior proposals and pull
requests alike.

## Build and test

Requires the .NET 8 SDK:

```bash
dotnet build Hakim.Pipelines.sln
dotnet test Hakim.Pipelines.sln
```

Run the sample end-to-end before submitting:

```bash
dotnet run --project samples/Hakim.Pipelines.Sample
```

## Pull request expectations

- All tests green; new behaviors ship with happy-path **and** failure-path
  tests (see `tests/Hakim.Pipelines.Tests/` for the established style)
- Public APIs carry XML doc comments — the build treats missing docs as
  warnings, and warnings fail the review
- Add a `CHANGELOG.md` entry under "Unreleased"
- Note roadmap impact in the PR description if you ship or change a
  [ROADMAP.md](ROADMAP.md) item
- Keep behaviors single-purpose: if it needs two names, it is two behaviors

## Design rules

- Behaviors stay dependency-light: MediatR, FluentValidation (validation
  behaviors only) and Microsoft.Extensions abstractions
- No behavior may swallow an exception silently — log it, convert it to a
  `Result` failure where that is the contract, or rethrow
- Registration order is part of the public contract: unhandled-exception →
  logging → performance → validation → transaction → caching
