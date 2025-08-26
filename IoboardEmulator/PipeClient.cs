using System.IO.Pipes;
using System.Text;
using Common;

namespace IoboardEmulator;

internal sealed class PipeClient : IDisposable
{
    private NamedPipeClientStream? _cli;
    private CancellationTokenSource? _cts;

    public event Action<int,int>? OnInput; // Server→Emu（入力状態）
    public event Action<string>? OnLog;

    public void Start()
    {
        _cts = new CancellationTokenSource();
        _ = Task.Run(() => ConnectLoopAsync(_cts.Token));
    }

    private async Task ConnectLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                _cli = new NamedPipeClientStream(".", PipeConfig.PipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
                await _cli.ConnectAsync(2000, ct).ConfigureAwait(false);
                OnLog?.Invoke($"[EmuPipe] Connected");

                _ = Task.Run(() => ReadLoopAsync(_cli, ct));
                while (_cli.IsConnected && !ct.IsCancellationRequested)
                    await Task.Delay(500, ct).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                OnLog?.Invoke($"[EmuPipe] Connect error: {ex.Message}");
                await Task.Delay(1000, ct).ConfigureAwait(false);
            }
        }
    }

    private async Task ReadLoopAsync(NamedPipeClientStream cli, CancellationToken ct)
    {
        var buf = new byte[1024];
        var sb = new StringBuilder();
        try
        {
            while (!ct.IsCancellationRequested && cli.IsConnected)
            {
                int n = await cli.ReadAsync(buf.AsMemory(0, buf.Length), ct).ConfigureAwait(false);
                if (n <= 0) break;
                sb.Append(Encoding.UTF8.GetString(buf, 0, n));
                int nl;
                while ((nl = sb.ToString().IndexOf('\n')) >= 0)
                {
                    var line = sb.ToString()[..nl].Trim();
                    sb.Remove(0, nl + 1);
                    if (line.Length == 0) continue;
                    OnLog?.Invoke($"[EmuPipe<=] {line}");

                    var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length == 3 && parts[0].Equals(PipeConfig.CmdInput, StringComparison.OrdinalIgnoreCase))
                    {
                        if (int.TryParse(parts[1], out var port) && int.TryParse(parts[2], out var val))
                        {
                            if (port >= 0 && port < 256 && (val == 0 || val == 1))
                            {
                                OnInput?.Invoke(port, val);
                                // ★ 受信確認のACKを返す（Serverログに出ます）
                                SendRaw($"ACK {port} {val}\n");
                            }
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            OnLog?.Invoke($"[EmuPipe] Read error: {ex.Message}");
        }
    }

    public void SendWrite(int port, int val)
    {
        SendRaw($"{PipeConfig.CmdWrite} {port} {val}\n");
    }

    private void SendRaw(string line)
    {
        try
        {
            if (_cli is not { IsConnected: true }) { OnLog?.Invoke("[EmuPipe] not connected"); return; }
            var bytes = Encoding.UTF8.GetBytes(line);
            _cli.Write(bytes, 0, bytes.Length); _cli.Flush();
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
