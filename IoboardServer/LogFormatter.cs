using System;

namespace IoboardServer;

public static class LogFormatter
{
    public static string Format(LogType type, string message)
    {
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
        var level = type.ToString().ToUpper().PadRight(7);
        return $"[{timestamp}] [{level}] {message}";
    }
}
