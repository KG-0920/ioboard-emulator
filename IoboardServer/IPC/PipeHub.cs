using System;
using System.Collections.Concurrent;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Common;

namespace IoboardServer.IPC
{
    /// <summary>
    /// NamedPipe サーバ。
    /// - 接続ごとにIDを付与
    /// - クライアントから "HELLO_RSW n" を受け取り、その接続IDに RSW を紐付け
    /// - "WRITE p v" を受けたら、RSWフィルタ（SetRswFilter）に一致する場合のみ既存OnWriteを発火
    /// - "INPUT p v" を指定RSWのクライアントのみに送信
    /// 既存互換：
    ///   - OnWrite(port,val) は残します（RSWフィルタ未設定時は従来どおり全てで発火）
    ///   - BroadcastInput(port,val) も残し、RSWフィルタが設定されている場合はそのRSWにのみ送信
    /// 拡張：
    ///   - OnWriteRsw(rsw,port,val) を追加（必要なら将来利用）
    /// </summary>
    public sealed class PipeHub : IDisposable
    {
        private readonly ConcurrentDictionary<int, NamedPipeServerStream> _clients = new();
        private readonly ConcurrentDictionary<int, int> _clientRsw = new(); // clientId -> rsw
        private int _nextId;
        private volatile bool _running;

        // この PipeHub インスタンスが担当する RSW（null なら全体）
        private int? _rswFilter;

        // 既存互換イベント
        public event Action<int, int>? OnWrite; // (port, val)
        public event Action<string>? OnLog;

        // 拡張イベント（必要なら購読）
        public event Action<int, int, int>? OnWriteRsw; // (rsw, port, val)

        public void SetRswFilter(int rsw) => _rswFilter = rsw;

        public void Start()
        {
            if (_running) return;
            _running = true;
            _ = Task.Run(AcceptLoop);
        }

        private async Task AcceptLoop()
        {
            OnLog?.Invoke("[Pipe] AcceptLoop started");
            while (_running)
            {
                var server = new NamedPipeServerStream(
                    PipeConfig.PipeName,
                    PipeDirection.InOut,
                    NamedPipeServerStream.MaxAllowedServerInstances,
                    PipeTransmissionMode.Byte,
                    PipeOptions.Asynchronous);

                try
                {
                    await server.WaitForConnectionAsync().ConfigureAwait(false);
                }
                catch
                {
                    try { server.Dispose(); } catch { }
                    continue;
                }

                int id = Interlocked.Increment(ref _nextId);
                _clients[id] = server;
                OnLog?.Invoke($"[Pipe] accepted id={id}");

                _ = Task.Run(() => ReadLoop(id, server));
            }
        }

        private async Task ReadLoop(int id, NamedPipeServerStream s)
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

                    while (true)
                    {
                        var txt = sb.ToString();
                        int nl = txt.IndexOf('\n');
                        if (nl < 0) break;

                        var line = txt.Substring(0, nl).TrimEnd('\r');
                        sb.Remove(0, nl + 1);

                        HandleLine(id, line);
                    }
                }
            }
            catch (IOException) { /* disconnect */ }
            catch (Exception ex)
            {
                OnLog?.Invoke($"[Pipe] error id={id} {ex.Message}");
            }
            finally
            {
                try { s.Dispose(); } catch { }
                _clients.TryRemove(id, out _);
                _clientRsw.TryRemove(id, out _);
                OnLog?.Invoke($"[Pipe] closed id={id}");
            }
        }

        private void HandleLine(int id, string line)
        {
            OnLog?.Invoke($"[<=Pipe] {line}");

            var parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) return;

            var cmd = parts[0].ToUpperInvariant();
            switch (cmd)
            {
                case "HELLO_RSW":
                {
                    if (parts.Length >= 2 && int.TryParse(parts[1], out var rsw))
                    {
                        _clientRsw[id] = rsw;
                        OnLog?.Invoke($"[Pipe] id={id} mapped to RSW={rsw}");
                    }
                    return;
                }

                case "WRITE":
                {
                    if (parts.Length >= 3 &&
                        int.TryParse(parts[1], out var port) &&
                        int.TryParse(parts[2], out var val))
                    {
                        int rsw = _clientRsw.TryGetValue(id, out var r) ? r : 0;

                        // 既存互換：RSWフィルタ未設定なら従来どおり全通知、設定済みなら一致時のみ
                        if (_rswFilter is null || _rswFilter.Value == rsw)
                        {
                            OnWrite?.Invoke(port, val);
                        }
                        // 拡張イベント
                        OnWriteRsw?.Invoke(rsw, port, val);
                    }
                    return;
                }

                default:
                    // 予期しない行はログのみ
                    return;
            }
        }

        // ===== 送信 =====

        /// <summary>
        /// 既存互換：全クライアントへ（または RSWフィルタ設定時は該当RSWへ）INPUT を配信。
        /// </summary>
        public void BroadcastInput(int port, int val)
        {
            if (_rswFilter is int rsw)
            {
                SendInputToRsw(rsw, port, val);
            }
            else
            {
                SendToClients(_ => true, $"{PipeConfig.CmdInput} {port} {val}\n");
            }
        }

        /// <summary>拡張：指定 RSW のみへ INPUT を配信。</summary>
        public void BroadcastInput(int rsw, int port, int val) => SendInputToRsw(rsw, port, val);

        private void SendInputToRsw(int rsw, int port, int val)
        {
            SendToClients(id => _clientRsw.TryGetValue(id, out var r) && r == rsw,
                          $"{PipeConfig.CmdInput} {port} {val}\n");
        }

        private void SendToClients(Func<int, bool> predicate, string line)
        {
            var bytes = Encoding.UTF8.GetBytes(line);
            foreach (var kv in _clients)
            {
                if (!predicate(kv.Key)) continue;
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
            _clientRsw.Clear();
        }
    }
}
