namespace Hakim.Pipelines.Results;

/// <summary>
/// Represents an error that caused an operation to fail.
/// </summary>
/// <param name="Code">A machine-readable error code, unique to the kind of failure.</param>
/// <param name="Message">A human-readable description of the failure.</param>
public sealed record Error(string Code, string Message)
{
    /// <summary>
    /// Represents the absence of an error. Used by successful results.
    /// </summary>
    public static readonly Error None = new(string.Empty, string.Empty);

    /// <summary>
    /// Represents an error caused by an unexpectedly null value.
    /// </summary>
    public static readonly Error NullValue = new(
        "Error.NullValue",
        "The specified result value is null.");
}
