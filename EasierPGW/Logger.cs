using System;

public static class Logger
{
    public enum LogLevel
    {
        Debug,
        Info,
        Warning,
        Error
    }

    private static LogLevel _currentLogLevel = LogLevel.Debug;

    public static void SetLogLevel(LogLevel level)
    {
        _currentLogLevel = level;
    }

    public static void Log(string message, LogLevel level)
    {
        if (level >= _currentLogLevel)
        {
            Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{level}]: {message}");
        }
    }

    public static void Debug(string message)
    {
        Log(message, LogLevel.Debug);
    }

    public static void Info(string message)
    {
        Log(message, LogLevel.Info);
    }

    public static void Warning(string message)
    {
        Log(message, LogLevel.Warning);
    }

    public static void Error(string message)
    {
        Log(message, LogLevel.Error);
    }
}
