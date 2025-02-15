using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace VRCHub;
public static class SimpleLogger
{
    public enum LogLevel
    {
        Debug, Info, Warn, Error
    }
    public static class Settings
    {
        public static LogLevel MinLogLevel { get; set; } = LogLevel.Debug;
        public static bool EnableConsoleColors { get; set; } = true;
        public static bool EnableTimestamps { get; set; } = true;
    }
    private class LogEntry
    {
        public DateTime Timestamp;
        public LogLevel Level;
        public string Message;
    }
    private static readonly ConcurrentQueue<LogEntry> _logQueue = new();
    private static readonly AutoResetEvent _logEvent = new(false);
    private static volatile bool _isRunning = true;
    static SimpleLogger() => Task.Factory.StartNew(ProcessQueue, TaskCreationOptions.LongRunning);
    private static void ProcessQueue()
    {
        while (_isRunning)
        {
            if (_logQueue.IsEmpty)
            {
                _logEvent.WaitOne(100);
                continue;
            }
            while (_logQueue.TryDequeue(out var entry))
            {
                WriteLog(entry);
            }
        }
    }
    private static void WriteLog(LogEntry entry)
    {
        ConsoleColor original = Console.ForegroundColor;
        if (Settings.EnableConsoleColors)
        {
            switch (entry.Level)
            {
                case LogLevel.Debug:
                    Console.ForegroundColor = ConsoleColor.Magenta;
                    break;
                case LogLevel.Info:
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    break;
                case LogLevel.Warn:
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    break;
                case LogLevel.Error:
                    Console.ForegroundColor = ConsoleColor.Red;
                    break;
            }
        }
        string timePrefix = Settings.EnableTimestamps ? $"[{entry.Timestamp:HH:mm:ss.fff}] " : "";
        Console.WriteLine($"{timePrefix}[{entry.Level}] {entry.Message}");
        if (Settings.EnableConsoleColors) Console.ForegroundColor = original;
    }
    public static void Log(LogLevel level, string message, params object[] args)
    {
        if (level < Settings.MinLogLevel) return;
        string formatted = (args != null && args.Length > 0) ? string.Format(message, args) : message;
        var entry = new LogEntry { Timestamp = DateTime.Now, Level = level, Message = formatted };
        _logQueue.Enqueue(entry);
        _logEvent.Set();
    }
    public static void Log(string message, params object[] args) => Log(LogLevel.Info, message, args);
    public static void Debug(string message, params object[] args) => Log(LogLevel.Debug, message, args);
    public static void Info(string message, params object[] args) => Log(LogLevel.Info, message, args);
    public static void Warn(string message, params object[] args) => Log(LogLevel.Warn, message, args);
    public static void Error(string message, params object[] args) => Log(LogLevel.Error, message, args);
    public static void Shutdown()
    {
        _isRunning = false; _logEvent.Set();
    }
}
