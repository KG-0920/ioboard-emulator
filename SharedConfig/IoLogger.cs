// File: SharedConfig/IoLogger.cs
// Ver : v1.0 (2025-09-12 JST)
// Why : 異常時の状況を ioboard_log.txt へ記録（最小差分・追加のみ）

using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;

namespace SharedConfig
{
    public static class IoLogger
    {
        private static readonly object _sync = new();
        private static string LogPath =>
            Path.Combine(AppContext.BaseDirectory ?? Environment.CurrentDirectory, "ioboard_log.txt");

        /// <summary>情報</summary>
        public static void Info(string msg) => Write("INFO", msg);

        /// <summary>警告</summary>
        public static void Warn(string msg) => Write("WARN", msg);

        /// <summary>エラー（例外付き）</summary>
        public static void Error(string msg, Exception? ex = null)
            => Write("ERROR", ex == null ? msg : msg + Environment.NewLine + ex);

        private static void Write(string level, string msg)
        {
            try
            {
                var line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [{level}] " +
                           $"pid={Environment.ProcessId} tid={Environment.CurrentManagedThreadId} " +
                           $"{msg}{Environment.NewLine}";
                lock (_sync)
                {
                    // 10MB 超で簡易ローテーション
                    var path = LogPath;
                    try
                    {
                        var fi = new FileInfo(path);
                        if (fi.Exists && fi.Length > 10 * 1024 * 1024)
                        {
                            var bak = Path.Combine(fi.DirectoryName!, $"ioboard_log_{DateTime.Now:yyyyMMdd_HHmmss}.txt");
                            File.Move(path, bak, overwrite: false);
                        }
                    }
                    catch { /* ignore rotation error */ }

                    File.AppendAllText(path, line, Encoding.UTF8);
                }
            }
            catch
            {
                // ここで例外は飲み込む（ログ自体でアプリを止めない）
            }
        }
    }
}
