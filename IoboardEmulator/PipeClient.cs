// IoboardEmulator / PipeClient.cs 置き換え
using System;
using System.IO.Pipes;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace IoboardEmulator
{
    internal sealed class PipeClient : IDisposable
    {
        public event Action<int,int>? OnInput; // (port,val)
        public event Action<string>? OnLog;

        readonly string _pipeName =
            Environment.GetEnvironmentVariable("IOBOARD_PIPE_NAME") ?? "ioboard_pipe";

        NamedPipeClientStream? _stream;
        CancellationTokenSource? _cts;
        Task? _recvTask;

        public void Start()
        {
            // ★ RSWごとにインスタンス生成される（static なし）
            _stream = new NamedPipeClientStream(".", _pipeName,
                        PipeDirection.InOut, PipeOptions.Asynchronous);
            _stream.Connect(3000); // 必要に応じて待ち時間調整
            _cts = new CancellationTokenSource();
            _recvTask = Task.Run(() => RecvLoop(_cts.Token));
            OnLog?.Invoke("[Pipe] connected");
        }

        public void SendHelloRsw(int rsw) => SendLine($"HELLO_RSW {rsw}");
        public void SendWrite(int port, int val) => SendLine($"WRITE {port} {val}");

        void SendLine(string line)
        {
            if (_stream is not { IsConnected: true }) return;
            var bytes = Encoding.UTF8.GetBytes(line + "\n");
            _stream.Write(bytes, 0, bytes.Length);
            _stream.Flush();
            OnLog?.Invoke($"[Pipe->] {line}");
        }

        async Task RecvLoop(CancellationToken ct)
        {
            var s = _stream!;
            var buf = new byte[1024];
            var sb = new StringBuilder();

            try
            {
                while (!ct.IsCancellationRequested && s.IsConnected)
                {
                    int n = await s.ReadAsync(buf, 0, buf.Length, ct).ConfigureAwait(false);
                    if (n <= 0) break;
                    sb.Append(Encoding.UTF8.GetString(buf, 0, n));

                    for (;;)
                    {
                        var str = sb.ToString();
                        var nl = str.IndexOf('\n');
                        if (nl < 0) break;

                        var line = str[..nl].TrimEnd();
                        sb.Remove(0, nl + 1);
                        if (line.Length == 0) continue;

                        OnLog?.Invoke($"[Pipe<=] {line}");

                        // "INPUT <port> <val>"
                        var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length >= 3 && parts[0].Equals("INPUT", StringComparison.OrdinalIgnoreCase)
                            && int.TryParse(parts[1], out var port)
                            && int.TryParse(parts[2], out var val))
                        {
                            OnInput?.Invoke(port, val);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                OnLog?.Invoke($"[Pipe<=] error {ex.Message}");
            }
        }

        public void Dispose()
        {
            try { _cts?.Cancel(); } catch { }
            try { _stream?.Dispose(); } catch { }
            _cts = null; _stream = null; _recvTask = null;
        }
    }
}
