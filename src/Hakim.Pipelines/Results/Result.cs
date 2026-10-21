namespace Hakim.Pipelines.Results;

/// <summary>
/// Represents the outcome of an operation that either succeeded or failed
/// with one or more <see cref="Error"/>s, without throwing an exception.
/// </summary>
public class Result
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Result"/> class.
    /// </summary>
    /// <param name="isSuccess">Whether the operation succeeded.</param>
    /// <param name="errors">The errors that caused the failure. Must be empty for a success.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when a success is created with errors, or a failure is created without errors.
    /// </exception>
    protected Result(bool isSuccess, IEnumerable<Error> errors)
    {
        var errorList = errors?.ToArray() ?? [];

        if (isSuccess && errorList.Length > 0)
        {
            throw new InvalidOperationException("A successful result cannot contain errors.");
        }

        if (!isSuccess && errorList.Length == 0)
        {
            throw new InvalidOperationException("A failed result must contain at least one error.");
        }

        IsSuccess = isSuccess;
        Errors = errorList;
    }

    /// <summary>
    /// Gets a value indicating whether the operation succeeded.
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// Gets a value indicating whether the operation failed.
    /// </summary>
    public bool IsFailure => !IsSuccess;

    /// <summary>
    /// Gets the errors that caused the failure. Empty when the operation succeeded.
    /// </summary>
    public IReadOnlyList<Error> Errors { get; }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    /// <returns>A successful <see cref="Result"/>.</returns>
    public static Result Success() => new(true, []);

    /// <summary>
    /// Creates a failed result from a single error.
    /// </summary>
    /// <param name="error">The error that caused the failure.</param>
    /// <returns>A failed <see cref="Result"/>.</returns>
    public static Result Failure(Error error) => new(false, [error]);

    /// <summary>
    /// Creates a failed result from a collection of errors.
    /// </summary>
    /// <param name="errors">The errors that caused the failure.</param>
    /// <returns>A failed <see cref="Result"/>.</returns>
    public static Result Failure(IEnumerable<Error> errors) => new(false, errors);

    /// <summary>
    /// Creates a successful result carrying a value.
    /// </summary>
    /// <typeparam name="TValue">The type of the value.</typeparam>
    /// <param name="value">The value produced by the operation.</param>
    /// <returns>A successful <see cref="Result{TValue}"/>.</returns>
    public static Result<TValue> Success<TValue>(TValue value) => new(value, true, []);

    /// <summary>
    /// Creates a failed result from a single error.
    /// </summary>
    /// <typeparam name="TValue">The type of the value the operation would have produced.</typeparam>
    /// <param name="error">The error that caused the failure.</param>
    /// <returns>A failed <see cref="Result{TValue}"/>.</returns>
    public static Result<TValue> Failure<TValue>(Error error) => new(default, false, [error]);

    /// <summary>
    /// Creates a failed result from a collection of errors.
    /// </summary>
    /// <typeparam name="TValue">The type of the value the operation would have produced.</typeparam>
    /// <param name="errors">The errors that caused the failure.</param>
    /// <returns>A failed <see cref="Result{TValue}"/>.</returns>
    public static Result<TValue> Failure<TValue>(IEnumerable<Error> errors) => new(default, false, errors);
}

/// <summary>
/// Represents the outcome of an operation that produces a value of type
/// <typeparamref name="TValue"/> on success, or one or more <see cref="Error"/>s on failure.
/// </summary>
/// <typeparam name="TValue">The type of the value produced by the operation.</typeparam>
public sealed class Result<TValue> : Result
{
    private readonly TValue? _value;

    internal Result(TValue? value, bool isSuccess, IEnumerable<Error> errors)
        : base(isSuccess, errors)
    {
        _value = value;
    }

    /// <summary>
    /// Gets the value produced by the operation.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when accessed on a failed result.
    /// </exception>
    public TValue Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("The value of a failed result cannot be accessed.");

    /// <summary>
    /// Creates a successful result carrying a value.
    /// </summary>
    /// <param name="value">The value produced by the operation.</param>
    /// <returns>A successful <see cref="Result{TValue}"/>.</returns>
    public static Result<TValue> Success(TValue value) => new(value, true, []);

    /// <summary>
    /// Creates a failed result from a single error.
    /// </summary>
    /// <param name="error">The error that caused the failure.</param>
    /// <returns>A failed <see cref="Result{TValue}"/>.</returns>
    public static new Result<TValue> Failure(Error error) => new(default, false, [error]);

    /// <summary>
    /// Creates a failed result from a collection of errors.
    /// </summary>
    /// <param name="errors">The errors that caused the failure.</param>
    /// <returns>A failed <see cref="Result{TValue}"/>.</returns>
    public static new Result<TValue> Failure(IEnumerable<Error> errors) => new(default, false, errors);
}
