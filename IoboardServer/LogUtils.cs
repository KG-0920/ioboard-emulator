using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;
using System.Text;

namespace IoboardServer
{
    public enum LogType
    {
        Info,
        Warning,
        Error,
    }

    public static class LogUtils
    {
        private static readonly object _lock = new();

        private static string? _baseFilePath;
        private static string? _currentFilePath;
        private static long _maxFileSizeBytes = 10 * 1024 * 1024; // 10MB
        private static int _maxRotationFiles = 5;

        public static string? BaseFilePath => _baseFilePath;
        public static string? CurrentFilePath => _currentFilePath;
        public static long MaxFileSizeBytes => _maxFileSizeBytes;
        public static int MaxRotationFiles => _maxRotationFiles;

        public static void Initialize(string? filePath = null)
        {
            if (!string.IsNullOrEmpty(filePath))
            {
                _baseFilePath = filePath;
            }
            else
            {
                _baseFilePath = Path.Combine(AppContext.BaseDirectory, "debug_log.txt");
            }

            _currentFilePath = _baseFilePath;

            string configFile = Path.Combine(AppContext.BaseDirectory, "IoLogConfig.xml");
            if (File.Exists(configFile))
            {
                try
                {
                    XDocument doc = XDocument.Load(configFile);
                    XElement? root = doc.Element("IoLogConfig");
                    if (root != null)
                    {
                        long.TryParse(root.Element("MaxFileSizeBytes")?.Value, out _maxFileSizeBytes);
                        int.TryParse(root.Element("MaxRotationFiles")?.Value, out _maxRotationFiles);
                    }
                }
                catch
                {
                    // 無視してデフォルト値を使用
                }
            }
        }

        public static void Write(LogType type, string message)
        {
            lock (_lock)
            {
                try
                {
                    if (_currentFilePath == null) return;

                    RotateLogs(_baseFilePath!, _maxFileSizeBytes, _maxRotationFiles);

                    string logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [{type}] {message}\n";
                    File.AppendAllText(_currentFilePath, logEntry, Encoding.UTF8);
                }
                catch
                {
                    // ログ書き込み失敗時は無視（アプリの動作を止めない）
                }
            }
        }

        private static void RotateLogs(string baseFilePath, long maxFileSizeBytes, int maxRotationFiles)
        {
            try
            {
                FileInfo fi = new FileInfo(baseFilePath);
                if (!fi.Exists || fi.Length < maxFileSizeBytes) return;

                for (int i = maxRotationFiles - 1; i >= 1; i--)
                {
                    string src = baseFilePath + "_" + i;
                    string dst = baseFilePath + "_" + (i + 1);
                    if (File.Exists(src)) File.Move(src, dst, overwrite: true);
                }

                File.Move(baseFilePath, baseFilePath + "_1", overwrite: true);
            }
            catch
            {
                // ローテーション失敗時も無視（アプリの動作を止めない）
            }
        }
    }
}
