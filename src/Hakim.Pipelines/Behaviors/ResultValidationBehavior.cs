using System.Reflection;
using FluentValidation;
using Hakim.Pipelines.Results;
using MediatR;

namespace Hakim.Pipelines.Behaviors;

/// <summary>
/// Runs every registered FluentValidation <see cref="IValidator{T}"/> for the
/// request before it reaches the handler. Unlike
/// <see cref="ValidationBehavior{TRequest, TResponse}"/>, validation failures
/// are returned as a failed <c>Result</c> / <c>Result&lt;T&gt;</c> instead of
/// throwing a <see cref="ValidationException"/>: invalid user input is an
/// expected outcome, not an exceptional one.
/// </summary>
/// <typeparam name="TRequest">The type of the request.</typeparam>
/// <typeparam name="TResponse">
/// The type of the response. The behavior only engages when this is
/// <c>Result</c> or <c>Result&lt;T&gt;</c>; any other response type passes
/// through untouched.
/// </typeparam>
/// <remarks>
/// Enable only one of the two validation behaviors.
/// </remarks>
public sealed class ResultValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private static readonly bool IsResultResponse = typeof(Result).IsAssignableFrom(typeof(TResponse));

    private static readonly MethodInfo GenericFailureMethod = typeof(Result)
        .GetMethods(BindingFlags.Public | BindingFlags.Static)
        .Single(method =>
            method.Name == nameof(Result.Failure) &&
            method.IsGenericMethodDefinition &&
            method.GetParameters().Length == 1 &&
            method.GetParameters()[0].ParameterType == typeof(IEnumerable<Error>));

    private readonly IEnumerable<IValidator<TRequest>> _validators;

    /// <summary>
    /// Initializes a new instance of the <see cref="ResultValidationBehavior{TRequest, TResponse}"/> class.
    /// </summary>
    /// <param name="validators">All validators registered for <typeparamref name="TRequest"/>.</param>
    public ResultValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    /// <inheritdoc />
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!IsResultResponse || !_validators.Any())
        {
            return await next();
        }

        var context = new ValidationContext<TRequest>(request);

        var validationResults = await Task.WhenAll(
            _validators.Select(validator => validator.ValidateAsync(context, cancellationToken)));

        var errors = validationResults
            .SelectMany(result => result.Errors)
            .Where(failure => failure is not null)
            .Select(failure => new Error(failure.ErrorCode, failure.ErrorMessage))
            .ToList();

        if (errors.Count != 0)
        {
            return CreateFailureResponse(errors);
        }

        return await next();
    }

    private static TResponse CreateFailureResponse(IReadOnlyList<Error> errors)
    {
        if (typeof(TResponse) == typeof(Result))
        {
            return (TResponse)(object)Result.Failure(errors);
        }

        // TResponse is Result<TValue>: build the matching generic failure.
        var valueType = typeof(TResponse).GetGenericArguments()[0];
        var failure = GenericFailureMethod
            .MakeGenericMethod(valueType)
            .Invoke(null, new object[] { errors });

        return (TResponse)failure!;
    }
}
