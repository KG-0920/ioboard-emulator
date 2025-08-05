using System;
using System.Diagnostics;
using System.IO.Pipes;
using System.Threading;

namespace IoboardServer
{
    public static class SingleInstanceHelper
    {
        private static Mutex? _mutex;
        private const string MutexName = "Global\\IoboardServerAppMutex";
        private const string PipeName = "IoboardServerPipe";

        public static bool IsOnlyInstance()
        {
            bool createdNew;
            _mutex = new Mutex(true, MutexName, out createdNew);
            return createdNew;
        }

        public static void Release()
        {
            try
            {
                _mutex?.ReleaseMutex();
            }
            catch
            {
                // 無視
            }
            _mutex = null;
        }

        public static void NotifyExistingInstanceToShutdown()
        {
            try
            {
                using var client = new NamedPipeClientStream(".", PipeName, PipeDirection.Out);
                client.Connect(1000); // 最大1秒待つ
                using var writer = new StreamWriter(client) { AutoFlush = true };
                writer.WriteLine("SHUTDOWN");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"NotifyExistingInstanceToShutdown failed: {ex.Message}");
            }
        }

        public static void StartListeningForShutdown(Action onShutdownRequested)
        {
            new Thread(() =>
            {
                try
                {
                    using var server = new NamedPipeServerStream(PipeName, PipeDirection.In);
                    using var reader = new StreamReader(server);
                    string? line = reader.ReadLine();
                    if (line == "SHUTDOWN")
                    {
                        onShutdownRequested?.Invoke();
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Shutdown listener error: {ex.Message}");
                }
            })
            {
                IsBackground = true
            }.Start();
        }
    }
}
