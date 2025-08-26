using System.Collections.Concurrent;
using System.IO.Pipes;
using System.Text;
using System.Threading.Tasks;
using Common;

namespace IoboardServer.IPC;

public sealed class PipeHub : IDisposable
{
    private readonly ConcurrentDictionary<int, NamedPipeServerStream> _clients = new();
    private int _nextId;
    private volatile bool _running;
    public event Action<int,int>? OnWrite; // (port, val) from client
    public event Action<string>? OnLog;

    public void Start()
    {
        _running = true;
        _ = AcceptLoopAsync();
        OnLog?.Invoke($"[Pipe] Listening: {PipeConfig.PipeName}");
    }

    private async Task AcceptLoopAsync()
    {
        while (_running)
        {
            var server = new NamedPipeServerStream(
                PipeConfig.PipeName, PipeDirection.InOut, NamedPipeServerStream.MaxAllowedServerInstances,
                PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
            await server.WaitForConnectionAsync().ConfigureAwait(false);
            var id = Interlocked.Increment(ref _nextId);
            _clients[id] = server;
            OnLog?.Invoke($"[Pipe] Client {id} connected");

            _ = Task.Run(() => HandleClientAsync(id, server));
        }
    }

    private async Task HandleClientAsync(int id, NamedPipeServerStream s)
    {
        var buf = new byte[1024];
        var sb = new StringBuilder();
        try
        {
            while (_running && s.IsConnected)
            {
                int n = await s.ReadAsync(buf, 0, buf.Length).ConfigureAwait(false);
                if (n <= 0) break;
                sb.Append(Encoding.UTF8.GetString(buf, 0, n));
                int nl;
                while ((nl = sb.ToString().IndexOf('\n')) >= 0)
                {
                    var line = sb.ToString()[..nl].Trim();
                    sb.Remove(0, nl + 1);
                    if (line.Length == 0) continue;
                    OnLog?.Invoke($"[Pipe<=] {line}");

                    var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length == 3 && parts[0].Equals(PipeConfig.CmdWrite, StringComparison.OrdinalIgnoreCase))
                    {
                        int port = int.TryParse(parts[1], out var p) ? p : -1;
                        int val  = int.TryParse(parts[2], out var v) ? v : -1;
                        if (port >= 0 && (val == 0 || val == 1))
                            OnWrite?.Invoke(port, val);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            OnLog?.Invoke($"[Pipe] Client {id} error: {ex.Message}");
        }
        finally
        {
            _clients.TryRemove(id, out _);
            try { s.Dispose(); } catch { }
            OnLog?.Invoke($"[Pipe] Client {id} disconnected");
        }
    }

    public void BroadcastInput(int port, int val)
    {
        var line = $"{PipeConfig.CmdInput} {port} {val}\n";
        var bytes = Encoding.UTF8.GetBytes(line);
        foreach (var kv in _clients)
        {
            try { kv.Value.Write(bytes, 0, bytes.Length); kv.Value.Flush(); }
            catch { /* ignore */ }
        }
        OnLog?.Invoke($"[Pipe=>] {line.Trim()}");
    }

    public void Dispose()
    {
        _running = false;
        foreach (var s in _clients.Values) { try { s.Dispose(); } catch { } }
        _clients.Clear();
    }
}
