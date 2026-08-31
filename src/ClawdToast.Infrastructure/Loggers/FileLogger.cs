using Microsoft.Extensions.Logging;

namespace ClawdToast.Infrastructure.Loggers;

public sealed class FileLoggerProvider : ILoggerProvider, ISupportExternalScope
{
    private readonly StreamWriter _writer;
    private readonly Lock _lock = new();
    private readonly LoggerExternalScopeProvider _fallbackScopeProvider = new();
    private IExternalScopeProvider? _externalScopeProvider;
    private IExternalScopeProvider ScopeProvider => _externalScopeProvider ?? _fallbackScopeProvider;

    public FileLoggerProvider(string filePath)
    {
        var absolutePath = Path.Combine(AppContext.BaseDirectory, filePath);
        var directoryPath = Path.GetDirectoryName(absolutePath);

        if (!string.IsNullOrEmpty(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }

        if (!File.Exists(absolutePath))
        {
            File.Create(absolutePath).Dispose();
        }

        _writer = new StreamWriter(absolutePath, append: true)
        {
            AutoFlush = true
        };
    }

    public ILogger CreateLogger(string categoryName) => new FileLogger(this, categoryName);

    public void SetScopeProvider(IExternalScopeProvider scopeProvider)
    {
        _externalScopeProvider = scopeProvider;
    }

    internal IDisposable? PushScope<TState>(TState state) where TState : notnull
        => ScopeProvider.Push(state);

    internal void WriteLog<TState>(
        LogLevel logLevel,
        string categoryName,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        var message = formatter(state, exception);
        var timestamp = DateTime.UtcNow.ToString("O");

        lock (_lock)
        {
            _writer.Write($"{timestamp} [{logLevel}] {categoryName}: {message}");

            ScopeProvider.ForEachScope(
                (scope, writer) =>
                {
                    writer.Write($" => {scope}");
                },
                _writer);

            if (exception is not null)
            {
                _writer.WriteLine();
                _writer.Write(exception);
            }

            _writer.WriteLine();
        }
    }

    public void Dispose()
    {
        lock (_lock)
        {
            _writer.Dispose();
        }
    }
}

public sealed class FileLogger(FileLoggerProvider provider, string categoryName) : ILogger
{
    private readonly FileLoggerProvider _provider = provider;
    private readonly string _categoryName = categoryName;

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
        => _provider.PushScope(state);

    public bool IsEnabled(LogLevel logLevel)
        => logLevel is not LogLevel.None;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        _provider.WriteLog(
            logLevel,
            _categoryName,
            state,
            exception,
            formatter);
    }
}
