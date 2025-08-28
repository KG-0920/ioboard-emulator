using System;
using System.IO.Pipes;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Common;

namespace IoboardEmulator
{
    internal sealed class PipeClient : IDisposable
    {
        private NamedPipeClientStream? _cli;
        private CancellationTokenSource? _cts;
        private readonly object _wlock = new();

        private volatile bool _connected;
        private volatile bool _helloSent;
        private int? _helloRsw;

        public event Action<int,int>? OnInput; // Server→Emu（入力通知）
        public event Action<string>? OnLog;

        public void Start()
        {
            _cts = new CancellationTokenSource();
            var ct = _cts.Token;
            _ = Task.Run(() => RunAsync(ct), ct);
        }

        private async Task RunAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    using var cli = new NamedPipeClientStream(
                        ".", PipeConfig.PipeName, PipeDirection.InOut, PipeOptions.Asynchronous);

                    _cli = cli;
                    OnLog?.Invoke("[EmuPipe] connecting...");
                    await cli.ConnectAsync(ct).ConfigureAwait(false);
                    _connected = true;
                    OnLog?.Invoke("[EmuPipe] connected");

                    // 接続直後に HELLO_RSW を必ず送る
                    if (_helloRsw.HasValue && !_helloSent)
                    {
                        SendRaw($"HELLO_RSW {_helloRsw.Value}\n");
                        _helloSent = true;
                    }

                    var buf = new byte[1024];
                    var sb = new StringBuilder();
                    while (!ct.IsCancellationRequested && cli.IsConnected)
                    {
                        int n = await cli.ReadAsync(buf.AsMemory(0, buf.Length), ct).ConfigureAwait(false);
                        if (n <= 0) break;

                        sb.Append(Encoding.UTF8.GetString(buf, 0, n));
                        while (true)
                        {
                            var all = sb.ToString();
                            int nl = all.IndexOf('\n');
                            if (nl < 0) break;

                            var line = all.Substring(0, nl).TrimEnd('\r');
                            sb.Remove(0, nl + 1);
                            HandleLine(line);
                        }
                    }
                }
                catch (OperationCanceledException) { /* ignore */ }
                catch (Exception ex)
                {
                    OnLog?.Invoke($"[EmuPipe] Read error: {ex.Message}");
                }
                finally
                {
                    _connected = false;
                    _helloSent = false; // 再接続時に再送する
                    try { _cli?.Dispose(); } catch { }
                    _cli = null;

                    if (!ct.IsCancellationRequested)
                        Thread.Sleep(300); // リトライ間隔
                }
            }
        }

        private void HandleLine(string line)
        {
            OnLog?.Invoke($"[<=EmuPipe] {line}");
            var parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 3 &&
                parts[0].Equals(PipeConfig.CmdInput, StringComparison.OrdinalIgnoreCase) &&
                int.TryParse(parts[1], out var port) &&
                int.TryParse(parts[2], out var val))
            {
                OnInput?.Invoke(port, val);
            }
        }

        public void SendWrite(int port, int val)
        {
            SendRaw($"WRITE {port} {val}\n");
        }

        public void SendHelloRsw(int rsw)
        {
            _helloRsw = rsw;
            if (_connected && !_helloSent)
            {
                SendRaw($"HELLO_RSW {rsw}\n");
                _helloSent = true;
            }
        }

        private void SendRaw(string line)
        {
            try
            {
                var cli = _cli;
                if (cli == null || !cli.IsConnected) return;
                var bytes = Encoding.UTF8.GetBytes(line);
                lock (_wlock)
                {
                    cli.Write(bytes, 0, bytes.Length);
                    cli.Flush();
                }
                OnLog?.Invoke($"[EmuPipe=>] {line.Trim()}");
            }
            catch (Exception ex)
            {
                OnLog?.Invoke($"[EmuPipe] Write error: {ex.Message}");
            }
        }

        public void Dispose()
        {
            try { _cts?.Cancel(); } catch { }
            try { _cli?.Dispose(); } catch { }
        }
    }
}
