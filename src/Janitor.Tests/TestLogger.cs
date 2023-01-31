using Microsoft.Extensions.Logging;

namespace Janitor.Tests;


public class TestLoggerProvider : ILoggerProvider
{
    private readonly List<string> _logMessages;


    public TestLoggerProvider(List<string> logMessages)
    {
        _logMessages = logMessages;
    }

    public ILogger CreateLogger(string categoryName) => new TestLogger(_logMessages);
    public void Dispose() { }
}

public class TestLogger : ILogger
{
    private readonly List<string> _logMessages;

    public TestLogger(List<string> logMessages)
    {
        _logMessages = logMessages;
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
    public bool IsEnabled(LogLevel logLevel) => true;
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        var message = formatter(state, exception);
        _logMessages.Add(message);
    }
}
