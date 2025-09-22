//#define IOBOARD_TRACE					// ← 通常はOFF。必要時は csproj の DefineConstants に IOBOARD_TRACE を追加（下記③参照）
//#define IOBOARD_PIPE_LOG				// ← 必要なときだけON（UIにパイプ詳細を出す）
//#define IOBOARD_ASYNC_INPUT_PUSH		// ← 実験用。通常はOFF（プッシュしない）

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Common; // PipeConfig, Logger, DiagTrace

namespace IoboardServer.IPC
{
    public sealed class PipeHub : IDisposable
    {
        public static PipeHub Instance { get; } = new PipeHub();

        private readonly CancellationTokenSource _cts = new();
        private readonly ConcurrentDictionary<int, List<ClientConn>> _clients = new();
        private readonly ConcurrentDictionary<int, List<Action<int,int,int>>> _writeSubs = new();
        private readonly ConcurrentDictionary<int, List<Action<string>>> _logSubs = new();
        private readonly ConcurrentDictionary<int, ConcurrentDictionary<int,int>> _lastInputs = new();
        private readonly string _hubPipeName = PipeConfig.PipeName;
        private int _nextId = 1;

        private PipeHub() { StartHubAcceptLoop(_hubPipeName); }
        public void Dispose()
        {
            _cts.Cancel();
            foreach (var list in _clients.Values)
                foreach (var c in list.ToArray()) c.Dispose();
        }

        public void Subscribe(int rsw, Action<int,int,int> onWrite, Action<string>? onLog = null)
        {
            _writeSubs.AddOrUpdate(rsw, _ => new List<Action<int,int,int>> { onWrite },
                                        (_, list) => { list.Add(onWrite); return list; });
            if (onLog != null)
                _logSubs.AddOrUpdate(rsw, _ => new List<Action<string>> { onLog },
                                          (_, list) => { list.Add(onLog); return list; });
            UiLog(rsw, $"[Pipe] subscribed for RSW={rsw}");
        }

        /// 入力をUIから変更：保持値だけ更新（既定はプッシュしない）
        public void BroadcastInput(int rsw, int port, int val)
        {
            var map = _lastInputs.GetOrAdd(rsw, _ => new ConcurrentDictionary<int, int>());
            map[port] = (val != 0) ? 1 : 0;

#if IOBOARD_ASYNC_INPUT_PUSH
            if (_clients.TryGetValue(rsw, out var list))
            {
                var dead = new List<ClientConn>();
                foreach (var c in list)
                {
                    try { _ = c.SendLineAsync($"{PipeConfig.CmdInput} {port} {val}"); }
                    catch { dead.Add(c); }
                }
                foreach (var d in dead) RemoveClient(rsw, d);
            }
            Traffic(rsw, $"[=>Pipe] {PipeConfig.CmdInput} {port} {val}");
#endif
#if IOBOARD_TRACE
            DiagTrace.Write("INPUT", rsw, port, val != 0, "Hub");
#endif
        }

        private Task? _hubTask;
        private void StartHubAcceptLoop(string name)
        {
            _hubTask = Task.Run(async () =>
            {
                while (!_cts.IsCancellationRequested)
                {
                    NamedPipeServerStream? server = null;
                    try
                    {
                        server = new NamedPipeServerStream(
                            name, PipeDirection.InOut,
                            NamedPipeServerStream.MaxAllowedServerInstances,
                            PipeTransmissionMode.Message, PipeOptions.Asynchronous);

                        await server.WaitForConnectionAsync(_cts.Token).ConfigureAwait(false);
                        _ = HandleAcceptedClientAsync(server);
                        server = null;
                    }
                    catch (OperationCanceledException) { break; }
                    catch { try { server?.Dispose(); } catch { } await Task.Delay(250, _cts.Token); }
                }
            }, _cts.Token);
        }

        private static int ResolveRswFromHello(string? hello)
        {
            if (string.IsNullOrWhiteSpace(hello)) return -1;
            var parts = hello.Trim().Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 2 && parts[0].Equals("HELLO_NAME", StringComparison.OrdinalIgnoreCase))
            {
                var m = System.Text.RegularExpressions.Regex.Match(parts[1], @"^FBIDIO(?<n>\d+)$",
                                                                  System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                if (m.Success && int.TryParse(m.Groups["n"].Value, out var rsw)) return rsw;
            }
            return -1;
        }

        private void OnLine(int rsw, ClientConn from, string line)
        {
            if (string.IsNullOrWhiteSpace(line)) return;
            var parts = line.Trim().Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);

            // WRITE <port> <0|1> … ビット単位（既存互換）
            if (parts.Length >= 3 && parts[0].Equals(PipeConfig.CmdWrite, StringComparison.OrdinalIgnoreCase)
                && int.TryParse(parts[1], out var wPort) && int.TryParse(parts[2], out var wVal))
            {
                Traffic(rsw, $"[<=Pipe] {PipeConfig.CmdWrite} {wPort} {wVal}");
#if IOBOARD_TRACE
                DiagTrace.Write("WRITE", rsw, wPort, wVal != 0, "Hub");
#endif
            	if (_writeSubs.TryGetValue(rsw, out var subs))
                    foreach (var h in subs.ToArray()) try { h(rsw, wPort, wVal); } catch { }
                return;
            }

            // INPUT <port> … 保持値で即応答
            if (parts.Length >= 2 && parts[0].Equals(PipeConfig.CmdInput, StringComparison.OrdinalIgnoreCase)
                && int.TryParse(parts[1], out var qPort))
            {
                int val = 0;
                if (_lastInputs.TryGetValue(rsw, out var map) && map.TryGetValue(qPort, out var v))
                    val = (v != 0) ? 1 : 0;

                _ = from.SendLineAsync($"OK {val}");
                Traffic(rsw, $"[=>Pipe] OK {val}  (INPUT {qPort})");
#if IOBOARD_TRACE
                DiagTrace.Write("INPUT", rsw, qPort, val != 0, "Hub");
#endif
            	return;
            }

            Traffic(rsw, $"[<=Pipe] {line}");
        }

        private void AddClient(int rsw, ClientConn conn)
        {
            _clients.AddOrUpdate(rsw, _ => new List<ClientConn> { conn },
                                      (_, list) => { list.Add(conn); return list; });
        }
        private void RemoveClient(int rsw, ClientConn conn)
        {
            if (_clients.TryGetValue(rsw, out var list)) { list.Remove(conn); conn.Dispose(); }
        }

        private void OnDisconnected(int rsw, int id, Exception? ex)
        {
            UiLog(rsw, ex == null ? $"[Pipe] closed id={id}" : $"[Pipe] closed id={id} ({ex.Message})");
            if (_clients.TryGetValue(rsw, out var list))
            {
                var dead = list.FindAll(c => c.Id == id);
                foreach (var d in dead) RemoveClient(rsw, d);
            }
        }

        private Task HandleAcceptedClientAsync(NamedPipeServerStream stream)
        {
            return Task.Run(async () =>
            {
                StreamReader? r = null; StreamWriter? w = null;
                try
                {
                    r = new StreamReader(stream, Encoding.ASCII, false, 1024, leaveOpen: true);
                    w = new StreamWriter(stream, new UTF8Encoding(false), 1024, leaveOpen: true)
                    { NewLine = "\n", AutoFlush = true };

                    string? hello = await r.ReadLineAsync().ConfigureAwait(false);
                    int rsw = ResolveRswFromHello(hello);
                    if (rsw < 0) { await w.WriteLineAsync("ERR INVALID_HELLO"); try { stream.Dispose(); } catch { } return; }

                    await w.WriteLineAsync($"OK {rsw}").ConfigureAwait(false);
                    DiagTrace.Write("HELLO", rsw, -1, false, "Hub");

                    int id = Interlocked.Increment(ref _nextId);
                    var conn = new ClientConn(
                        id, stream,
                        (c, line) => OnLine(rsw, c, line),
                        ex => OnDisconnected(rsw, id, ex),
                        r, w);

                    AddClient(rsw, conn);
                    UiLog(rsw, $"[Pipe] connected id={id}");
                    await conn.ReadLoopAsync(_cts.Token).ConfigureAwait(false);
                }
                catch { try { stream.Dispose(); } catch { } }
            }, _cts.Token);
        }

        // --- ログ(UI向けは既定OFF) ---
        private void UiLog(int rsw, string msg)
        {
#if IOBOARD_PIPE_LOG
            if (_logSubs.TryGetValue(rsw, out var logs))
            {
                var line = $"[{DateTime.Now:HH:mm:ss.fff}] {msg}";
                foreach (var l in logs.ToArray()) try { l(line); } catch { }
            }
#endif
            try { Logger.Log(msg); } catch { }
        }
        private void Traffic(int rsw, string msg) => UiLog(rsw, msg);

        // --- クライアント ---
        private sealed class ClientConn : IDisposable
        {
            public int Id { get; }
            private readonly NamedPipeServerStream _stream;
            private readonly StreamReader _reader;
            private readonly StreamWriter _writer;
            private readonly SemaphoreSlim _sendLock = new(1, 1);
            private readonly Action<ClientConn, string> _onLine;
            private readonly Action<Exception?> _onClose;

            public ClientConn(int id, NamedPipeServerStream stream,
                              Action<ClientConn, string> onLine, Action<Exception?> onClose,
                              StreamReader? reader = null, StreamWriter? writer = null)
            {
                Id = id; _stream = stream; _onLine = onLine; _onClose = onClose;
                _reader = reader ?? new StreamReader(stream, Encoding.UTF8);
                _writer = writer ?? new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true };
            }

            public async Task ReadLoopAsync(CancellationToken token)
            {
                try
                {
                    while (!token.IsCancellationRequested && _stream.IsConnected)
                    {
                        var line = await _reader.ReadLineAsync().ConfigureAwait(false);
                        if (line == null) break;
                        _onLine(this, line);
                    }
                    _onClose(null);
                }
                catch (Exception ex) { _onClose(ex); }
            }

            public async Task SendLineAsync(string line)
            {
                await _sendLock.WaitAsync().ConfigureAwait(false);
                try { await _writer.WriteLineAsync(line).ConfigureAwait(false); }
                finally { _sendLock.Release(); }
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
