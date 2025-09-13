// IoboardServer/IPC/PipeHub.cs
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace IoboardServer.IPC
{
    /// <summary>
    /// NamedPipe のハブ。
    /// - RSW(ロータリSW番号)ごとに複数クライアントを受け付け
    /// - クライアント→サーバ: "WRITE <port> <val>" を受信し購読者へ通知
    /// - サーバ→クライアント: "INPUT <port> <val>" を送信（BroadcastInput）
    /// - 接続時に "HELLO_RSW <rsw>" をサーバ→クライアントへ送出
    /// </summary>
    public sealed class PipeHub : IDisposable
    {
        public static PipeHub Instance { get; } = new PipeHub();

        private readonly CancellationTokenSource _cts = new();
        private readonly ConcurrentDictionary<int, List<ClientConn>> _clients = new(); // rsw -> connections
        private readonly ConcurrentDictionary<int, List<Action<int,int,int>>> _writeSubs = new(); // rsw -> handlers
        private readonly ConcurrentDictionary<int, List<Action<string>>> _logSubs = new(); // rsw -> loggers
        private int _nextId = 1;

        // 既知のパイプ名（旧/新 どちらでも接続可にする）
        private static readonly string[] PipeNamePatterns = new[]
        {
            "IoboardEmu_RSW_{0}",        // 例: IoboardEmu_RSW_0
            "IoboardEmulator_RSW{0}",    // 例: IoboardEmulator_RSW0
            "ioboard_emulator_rsw{0}"    // 例: ioboard_emulator_rsw0
        };

        private PipeHub()
        {
            // ひとまず RSW=0/1 を待受（必要に応じて拡張可）
            StartAcceptLoop(0);
            StartAcceptLoop(1);
        }

        public void Dispose()
        {
            _cts.Cancel();
            foreach (var list in _clients.Values)
            {
                foreach (var c in list.ToArray()) c.Dispose();
            }
        }

        /// <summary>
        /// WRITE通知の購読登録。onLog は任意。
        /// </summary>
        public void Subscribe(int rsw, Action<int,int,int> onWrite, Action<string>? onLog = null)
        {
            _writeSubs.AddOrUpdate(rsw,
                _ => new List<Action<int,int,int>> { onWrite },
                (_, list) => { list.Add(onWrite); return list; });

            if (onLog != null)
            {
                _logSubs.AddOrUpdate(rsw,
                    _ => new List<Action<string>> { onLog },
                    (_, list) => { list.Add(onLog); return list; });
            }

            Log(rsw, $"[Pipe] subscribed for RSW={rsw}");
        }

        /// <summary>
        /// サーバ→クライアントへの入力配信
        /// </summary>
        public void BroadcastInput(int rsw, int port, int val)
        {
            if (_clients.TryGetValue(rsw, out var list))
            {
                var dead = new List<ClientConn>();
                foreach (var c in list)
                {
                    try { c.SendLine($"INPUT {port} {val}"); }
                    catch { dead.Add(c); }
                }
                if (dead.Count > 0)
                {
                    foreach (var d in dead) RemoveClient(rsw, d);
                }
            }
            Log(rsw, $"[=>Pipe] INPUT {port} {val}");
        }

        private void StartAcceptLoop(int rsw)
        {
            // パイプ名の候補ごとに待受を立てる（null/空なら既定値へフォールバック）
            var patterns = PipeNamePatterns;
            if (patterns == null || patterns.Length == 0)
            {
                patterns = new[] { "IoboardEmu_RSW_{0}", "IoboardEmulator_RSW{0}", "ioboard_emulator_rsw{0}" };
                Log(rsw, "[Pipe] WARN: PipeNamePatterns was null/empty. Using fallback defaults.");
            }

            foreach (var pattern in patterns)
            {
                var pipeName = string.Format(pattern, rsw);
                Task.Run(async () =>
                {
                    Log(rsw, "[Pipe] AcceptLoop started");
                    while (!_cts.IsCancellationRequested)
                    {
                        NamedPipeServerStream? server = null;
                        try
                        {
                            server = new NamedPipeServerStream(
                                pipeName,
                                PipeDirection.InOut,
                                254, // Max instances
                                PipeTransmissionMode.Byte,
                                PipeOptions.Asynchronous);

                            await server.WaitForConnectionAsync(_cts.Token).ConfigureAwait(false);

                            var id = Interlocked.Increment(ref _nextId);
                            var conn = new ClientConn(id, rsw, server, line => OnLine(rsw, line), ex => OnDisconnected(rsw, id, ex));
                            AddClient(rsw, conn);
                            Log(rsw, $"[Pipe] accepted id={id}");
                            conn.SendLine($"HELLO_RSW {rsw}");
                            Log(rsw, "[<=Pipe] HELLO_RSW " + rsw);
                            _ = conn.ReadLoopAsync(_cts.Token);
                        }
                        catch (OperationCanceledException)
                        {
                            server?.Dispose();
                            break;
                        }
                        catch (Exception ex)
                        {
                            server?.Dispose();
                            Log(rsw, $"[Pipe] accept error: {ex.Message}");
                            await Task.Delay(200).ConfigureAwait(false);
                        }
                    }
                }, _cts.Token);
            }
        }

        private void OnLine(int rsw, string line)
        {
            // WRITE <port> <val> だけ拾う（その他はログ）
            if (string.IsNullOrWhiteSpace(line)) return;
            var parts = line.Trim().Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length >= 3 && parts[0].Equals("WRITE", StringComparison.OrdinalIgnoreCase)
                && int.TryParse(parts[1], out var port) && int.TryParse(parts[2], out var val))
            {
                Log(rsw, $"[<=Pipe] WRITE {port} {val}");
                if (_writeSubs.TryGetValue(rsw, out var subs))
                {
                    foreach (var h in subs.ToArray())
                    {
                        try { h(rsw, port, val); } catch { /* ignore */ }
                    }
                }
            }
            else
            {
                Log(rsw, $"[<=Pipe] {line}");
            }
        }

        private void AddClient(int rsw, ClientConn conn)
        {
            _clients.AddOrUpdate(rsw,
                _ => new List<ClientConn> { conn },
                (_, list) => { list.Add(conn); return list; });
        }

        private void RemoveClient(int rsw, ClientConn conn)
        {
            if (_clients.TryGetValue(rsw, out var list))
            {
                list.Remove(conn);
                conn.Dispose();
            }
        }

        private void OnDisconnected(int rsw, int id, Exception? ex)
        {
            Log(rsw, ex == null ? $"[Pipe] closed id={id}" : $"[Pipe] closed id={id} ({ex.Message})");
            if (_clients.TryGetValue(rsw, out var list))
            {
                var dead = list.Where(c => c.Id == id).ToList();
                foreach (var d in dead) RemoveClient(rsw, d);
            }
        }

        private void Log(int rsw, string msg)
        {
            if (_logSubs.TryGetValue(rsw, out var logs))
            {
                var line = $"[{DateTime.Now:HH:mm:ss.fff}] {msg}";
                foreach (var l in logs.ToArray())
                {
                    try { l(line); } catch { /* ignore */ }
                }
            }
        }

        // 単一接続のラッパ
        private sealed class ClientConn : IDisposable
        {
            public int Id { get; }
            private readonly int _rsw;
            private readonly NamedPipeServerStream _stream;
            private readonly StreamReader _reader;
            private readonly StreamWriter _writer;
            private readonly Action<string> _onLine;
            private readonly Action<Exception?> _onClose;

            public ClientConn(int id, int rsw, NamedPipeServerStream stream, Action<string> onLine, Action<Exception?> onClose)
            {
                Id = id;
                _rsw = rsw;
                _stream = stream;
                _reader = new StreamReader(stream, Encoding.UTF8);
                _writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true };
                _onLine = onLine;
                _onClose = onClose;
            }

            public async Task ReadLoopAsync(CancellationToken token)
            {
                try
                {
                    while (!token.IsCancellationRequested && _stream.IsConnected)
                    {
                        var line = await _reader.ReadLineAsync().ConfigureAwait(false);
                        if (line == null) break;
                        _onLine(line);
                    }
                    _onClose(null);
                }
                catch (Exception ex)
                {
                    _onClose(ex);
                }
            }

            public void SendLine(string line)
            {
                _writer.WriteLine(line);
            }

            public void Dispose()
            {
                try { _writer.Dispose(); } catch { }
                try { _reader.Dispose(); } catch { }
                try { _stream.Dispose(); } catch { }
            }
        }
    }
}
