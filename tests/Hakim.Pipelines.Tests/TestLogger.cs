using Microsoft.Extensions.Logging;

namespace Hakim.Pipelines.Tests;

/// <summary>
/// An <see cref="ILogger{T}"/> that records every entry so tests can assert
/// on what a behavior logged.
/// </summary>
public sealed class TestLogger<T> : ILogger<T>
{
    public List<(LogLevel Level, string Message, Exception? Exception)> Entries { get; } = [];

    public IDisposable? BeginScope<TState>(TState state)
        where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        Entries.Add((logLevel, formatter(state, exception), exception));
    }
}
