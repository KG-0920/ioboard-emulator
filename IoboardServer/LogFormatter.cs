using System;

namespace IoboardServer;

public static class LogFormatter
{
    public static string Format(LogType type, string message)
    {
        string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
        string typeLabel = type.ToString().ToUpper();
        return $"[{timestamp}] [{typeLabel}] {message}";
    }
}
