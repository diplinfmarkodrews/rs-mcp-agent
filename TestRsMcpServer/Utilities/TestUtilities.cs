using System;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace TestRsMcpServer.Utilities;

/// <summary>
/// Mock host environment for testing
/// </summary>
public class MockHostEnvironment : IHostEnvironment
{
    public string EnvironmentName { get; set; } = "Development";
    public string ApplicationName { get; set; } = "TestApplication";
    public string ContentRootPath { get; set; } = Directory.GetCurrentDirectory();
    public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
}

/// <summary>
/// Test logger provider for capturing log messages during tests
/// </summary>
public class TestLoggerProvider : ILoggerProvider
{
    private readonly System.Collections.Generic.List<string> _logMessages;

    public TestLoggerProvider(System.Collections.Generic.List<string> logMessages)
    {
        _logMessages = logMessages;
    }

    public ILogger CreateLogger(string categoryName)
    {
        return new TestLogger(_logMessages);
    }

    public void Dispose() { }
}

/// <summary>
/// Test logger implementation for capturing log messages
/// </summary>
public class TestLogger : ILogger
{
    private readonly System.Collections.Generic.List<string> _logMessages;

    public TestLogger(System.Collections.Generic.List<string> logMessages)
    {
        _logMessages = logMessages;
    }

    public IDisposable? BeginScope<TState>(TState state) => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        _logMessages.Add(formatter(state, exception));
    }
}
