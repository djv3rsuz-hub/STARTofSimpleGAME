using System.Diagnostics;
using System.IO;

namespace SimpleWPFGame.Logging;

public enum LogLevel
{
    Debug,
    Info,
    Warning,
    Error,
    Critical
}

public static class Logger
{
    private static readonly object _lock = new();
    private static string _logFilePath = string.Empty;
    private static bool _initialized;
    private static readonly Queue<string> _recentLogs = new(100);
    private const int MaxRecentLogs = 100;

    public static event Action<string, LogLevel>? OnLogMessage;

    public static void Initialize(string fileName)
    {
        try
        {
            var dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
            Directory.CreateDirectory(dir);
            _logFilePath = Path.Combine(dir, fileName);

            if (!File.Exists(_logFilePath))
                File.WriteAllText(_logFilePath, $"=== SimpleWPFGame Log Started {DateTime.Now:yyyy-MM-dd HH:mm:ss} ===\n");

            _initialized = true;
            Log($"Logger initialized at {_logFilePath}", LogLevel.Info);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[LOGGER INIT FAILED] {ex.Message}");
            _initialized = false;
        }
    }

    public static void Log(string message, LogLevel level = LogLevel.Debug, [System.Runtime.CompilerServices.CallerMemberName] string caller = "", [System.Runtime.CompilerServices.CallerFilePath] string filePath = "")
    {
        var fileName = Path.GetFileNameWithoutExtension(filePath);
        var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
        var entry = $"[{timestamp}] [{level,-8}] [{fileName}:{caller}] {message}";

        lock (_lock)
        {
            _recentLogs.Enqueue(entry);
            if (_recentLogs.Count > MaxRecentLogs)
                _recentLogs.Dequeue();
        }

        Debug.WriteLine(entry);

        if (level >= LogLevel.Warning)
            System.Diagnostics.Trace.WriteLine(entry);

        OnLogMessage?.Invoke(entry, level);

        if (!_initialized || string.IsNullOrEmpty(_logFilePath)) return;

        try
        {
            File.AppendAllText(_logFilePath, entry + "\n");
        }
        catch
        {
            // Silent fail on log write errors
        }
    }

    public static void LogError(string message, Exception ex, [System.Runtime.CompilerServices.CallerMemberName] string caller = "", [System.Runtime.CompilerServices.CallerFilePath] string filePath = "")
    {
        Log($"{message}: {ex.GetType().Name} - {ex.Message}", LogLevel.Error, caller, filePath);
        Log($"Stack: {ex.StackTrace}", LogLevel.Debug, caller, filePath);
    }

    public static string[] GetRecentLogs(int count = 50)
    {
        lock (_lock)
        {
            return _recentLogs.Take(Math.Min(count, _recentLogs.Count)).ToArray();
        }
    }

    public static void Shutdown()
    {
        Log("Logger shutting down", LogLevel.Info);
        _initialized = false;
    }
}
