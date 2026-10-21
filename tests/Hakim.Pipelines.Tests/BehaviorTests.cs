using FluentValidation;
using Hakim.Pipelines.Behaviors;
using Hakim.Pipelines.Results;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Hakim.Pipelines.Tests;

public sealed record PingRequest(string Message) : IRequest<string>;

public sealed record CreateUserCommand(string Name) : IRequest<Result<int>>;

public sealed record CreateUserPlainCommand(string Name) : IRequest<Result>;

public sealed class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithErrorCode("Name.Required");
    }
}

public sealed class CreateUserPlainCommandValidator : AbstractValidator<CreateUserPlainCommand>
{
    public CreateUserPlainCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithErrorCode("Name.Required");
    }
}

public sealed class PingRequestValidator : AbstractValidator<PingRequest>
{
    public PingRequestValidator()
    {
        RuleFor(x => x.Message).NotEmpty();
    }
}

public sealed class LoggingBehaviorTests
{
    [Fact]
    public async Task Handle_LogsStartAndCompletion_AndReturnsResponse()
    {
        var logger = new TestLogger<LoggingBehavior<PingRequest, string>>();
        var behavior = new LoggingBehavior<PingRequest, string>(logger);

        var response = await behavior.Handle(
            new PingRequest("hello"),
            () => Task.FromResult("pong"),
            CancellationToken.None);

        Assert.Equal("pong", response);
        Assert.Contains(logger.Entries, e =>
            e.Level == LogLevel.Information && e.Message.Contains("Handling PingRequest"));
        Assert.Contains(logger.Entries, e =>
            e.Level == LogLevel.Information && e.Message.Contains("Handled PingRequest"));
    }
}

public sealed class PerformanceBehaviorTests
{
    private static PerformanceBehavior<PingRequest, string> CreateBehavior(
        TestLogger<PerformanceBehavior<PingRequest, string>> logger,
        TimeSpan threshold)
    {
        return new PerformanceBehavior<PingRequest, string>(
            logger,
            new PipelineOptions { PerformanceThreshold = threshold });
    }

    [Fact]
    public async Task Handle_FastRequest_LogsNoWarning()
    {
        var logger = new TestLogger<PerformanceBehavior<PingRequest, string>>();
        var behavior = CreateBehavior(logger, TimeSpan.FromSeconds(5));

        var response = await behavior.Handle(
            new PingRequest("hello"),
            () => Task.FromResult("pong"),
            CancellationToken.None);

        Assert.Equal("pong", response);
        Assert.DoesNotContain(logger.Entries, e => e.Level == LogLevel.Warning);
    }

    [Fact]
    public async Task Handle_SlowRequest_LogsWarning()
    {
        var logger = new TestLogger<PerformanceBehavior<PingRequest, string>>();
        var behavior = CreateBehavior(logger, TimeSpan.FromMilliseconds(10));

        var response = await behavior.Handle(
            new PingRequest("hello"),
            async () =>
            {
                await Task.Delay(100);
                return "pong";
            },
            CancellationToken.None);

        Assert.Equal("pong", response);
        Assert.Contains(logger.Entries, e =>
            e.Level == LogLevel.Warning && e.Message.Contains("Long running request"));
    }
}

public sealed class UnhandledExceptionBehaviorTests
{
    [Fact]
    public async Task Handle_Success_ReturnsResponse()
    {
        var logger = new TestLogger<UnhandledExceptionBehavior<PingRequest, string>>();
        var behavior = new UnhandledExceptionBehavior<PingRequest, string>(logger);

        var response = await behavior.Handle(
            new PingRequest("hello"),
            () => Task.FromResult("pong"),
            CancellationToken.None);

        Assert.Equal("pong", response);
        Assert.Empty(logger.Entries);
    }

    [Fact]
    public async Task Handle_Exception_LogsAndRethrows()
    {
        var logger = new TestLogger<UnhandledExceptionBehavior<PingRequest, string>>();
        var behavior = new UnhandledExceptionBehavior<PingRequest, string>(logger);
        var exception = new InvalidOperationException("boom");

        var thrown = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            behavior.Handle(
                new PingRequest("hello"),
                () => throw exception,
                CancellationToken.None));

        Assert.Same(exception, thrown);
        Assert.Contains(logger.Entries, e =>
            e.Level == LogLevel.Error && e.Exception == exception);
    }
}

public sealed class ValidationBehaviorTests
{
    [Fact]
    public async Task Handle_ValidRequest_CallsNext()
    {
        var behavior = new ValidationBehavior<CreateUserCommand, Result<int>>(
            [new CreateUserCommandValidator()]);
        var nextCalled = false;

        var response = await behavior.Handle(
            new CreateUserCommand("Abdullah"),
            () =>
            {
                nextCalled = true;
                return Task.FromResult(Result.Success(1));
            },
            CancellationToken.None);

        Assert.True(nextCalled);
        Assert.True(response.IsSuccess);
    }

    [Fact]
    public async Task Handle_InvalidRequest_ThrowsValidationException_AndSkipsNext()
    {
        var behavior = new ValidationBehavior<CreateUserCommand, Result<int>>(
            [new CreateUserCommandValidator()]);
        var nextCalled = false;

        var exception = await Assert.ThrowsAsync<ValidationException>(() =>
            behavior.Handle(
                new CreateUserCommand(""),
                () =>
                {
                    nextCalled = true;
                    return Task.FromResult(Result.Success(1));
                },
                CancellationToken.None));

        Assert.False(nextCalled);
        Assert.Contains(exception.Errors, failure => failure.PropertyName == "Name");
    }

    [Fact]
    public async Task Handle_NoValidators_CallsNext()
    {
        var behavior = new ValidationBehavior<CreateUserCommand, Result<int>>([]);

        var response = await behavior.Handle(
            new CreateUserCommand(""),
            () => Task.FromResult(Result.Success(7)),
            CancellationToken.None);

        Assert.True(response.IsSuccess);
        Assert.Equal(7, response.Value);
    }
}

public sealed class ResultValidationBehaviorTests
{
    [Fact]
    public async Task Handle_ValidRequest_CallsNext()
    {
        var behavior = new ResultValidationBehavior<CreateUserCommand, Result<int>>(
            [new CreateUserCommandValidator()]);

        var response = await behavior.Handle(
            new CreateUserCommand("Abdullah"),
            () => Task.FromResult(Result.Success(3)),
            CancellationToken.None);

        Assert.True(response.IsSuccess);
        Assert.Equal(3, response.Value);
    }

    [Fact]
    public async Task Handle_InvalidRequest_ReturnsFailureResult_AndSkipsNext()
    {
        var behavior = new ResultValidationBehavior<CreateUserCommand, Result<int>>(
            [new CreateUserCommandValidator()]);
        var nextCalled = false;

        var response = await behavior.Handle(
            new CreateUserCommand(""),
            () =>
            {
                nextCalled = true;
                return Task.FromResult(Result.Success(3));
            },
            CancellationToken.None);

        Assert.False(nextCalled);
        Assert.True(response.IsFailure);
        Assert.Contains(response.Errors, error => error.Code == "Name.Required");
    }

    [Fact]
    public async Task Handle_InvalidRequest_WithPlainResult_ReturnsFailure()
    {
        var behavior = new ResultValidationBehavior<CreateUserPlainCommand, Result>(
            [new CreateUserPlainCommandValidator()]);

        var response = await behavior.Handle(
            new CreateUserPlainCommand(""),
            () => Task.FromResult(Result.Success()),
            CancellationToken.None);

        Assert.True(response.IsFailure);
        Assert.Contains(response.Errors, error => error.Code == "Name.Required");
    }

    [Fact]
    public async Task Handle_NonResultResponse_PassesThroughEvenWhenInvalid()
    {
        var behavior = new ResultValidationBehavior<PingRequest, string>(
            [new PingRequestValidator()]);

        var response = await behavior.Handle(
            new PingRequest(""),
            () => Task.FromResult("pong"),
            CancellationToken.None);

        Assert.Equal("pong", response);
    }
}
