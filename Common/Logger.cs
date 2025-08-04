using System;
using System.IO;

namespace Common
{
    public static class Logger
    {
        private static readonly object _lock = new();
        private static readonly string _logFile = "ioboard_log.txt";

        public static void Log(string message)
        {
            var logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {message}";

            lock (_lock)
            {
                Console.WriteLine(logEntry);
                File.AppendAllText(_logFile, logEntry + Environment.NewLine);
            }
        }
    }
}
