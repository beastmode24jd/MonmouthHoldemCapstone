using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using NUnit.Framework;

namespace MH.Capstone.Tests.Acceptance;

/// <summary>
/// An <see cref="ILoggerProvider"/> that writes WebApp log messages to
/// <see cref="TestContext.Progress"/> so they appear immediately in the live
/// <c>dotnet test</c> console output.
/// <para>
/// Added to the <see cref="Microsoft.AspNetCore.Builder.WebApplicationBuilder"/> in
/// <see cref="TestWebAppHost"/> before the WebApp is configured, so it captures both
/// startup messages and all request-time log output across scenario runs.
/// </para>
/// </summary>
[ExcludeFromCodeCoverage]
internal sealed class TestOutputLoggerProvider : ILoggerProvider
{
    private readonly LogLevel _minimumLevel;

    public TestOutputLoggerProvider(LogLevel minimumLevel = LogLevel.Information)
    {
        _minimumLevel = minimumLevel;
    }

    public ILogger CreateLogger(string categoryName) =>
        new TestOutputLogger(categoryName, _minimumLevel);

    public void Dispose() { }
}

[ExcludeFromCodeCoverage]
internal sealed class TestOutputLogger(string categoryName, LogLevel minimumLevel) : ILogger
{
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => logLevel >= minimumLevel;

    public void Log<TState>(
        LogLevel logLevel, EventId eventId, TState state,
        Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
            return;

        var level = logLevel switch
        {
            LogLevel.Trace       => "trce",
            LogLevel.Debug       => "dbug",
            LogLevel.Information => "info",
            LogLevel.Warning     => "warn",
            LogLevel.Error       => "FAIL",
            LogLevel.Critical    => "CRIT",
            _                    => "????"
        };

        var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
        var message = formatter(state, exception);

        TestContext.Progress.WriteLine($"[WebApp {timestamp} {level}] {categoryName}: {message}");

        if (exception is not null)
            TestContext.Progress.WriteLine($"[WebApp {timestamp} excp] {exception}");
    }
}
