// IoboardServer/IPC/PipeHub.cs
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Common; // PipeConfig, Logger

namespace IoboardServer.IPC
{
    public sealed class PipeHub : IDisposable
    {
        public static PipeHub Instance { get; } = new PipeHub();

        private readonly CancellationTokenSource _cts = new();
        private readonly ConcurrentDictionary<int, List<ClientConn>> _clients = new();
        private readonly ConcurrentDictionary<int, List<Action<int,int,int>>> _writeSubs = new();
        private readonly ConcurrentDictionary<int, List<Action<string>>> _logSubs = new();
        private int _nextId = 1;

        // 公式パイプ名は Common.PipeConfig.PipeName で統一（例: "IoboardBus"）
        private readonly string _hubPipeName = PipeConfig.PipeName;

        private PipeHub()
        {
            // 正準：単一パイプ名のハブ待受のみ
            StartHubAcceptLoop(_hubPipeName);
        }

        public void Dispose()
        {
            _cts.Cancel();
            foreach (var list in _clients.Values)
            {
                foreach (var c in list.ToArray()) c.Dispose();
            }
        }

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

        public void BroadcastInput(int rsw, int port, int val)
        {
            if (_clients.TryGetValue(rsw, out var list))
            {
                var dead = new List<ClientConn>();
                foreach (var c in list)
                {
                    try { c.SendLine($"{PipeConfig.CmdInput} {port} {val}"); }
                    catch { dead.Add(c); }
                }
                if (dead.Count > 0)
                {
                    foreach (var d in dead) RemoveClient(rsw, d);
                }
            }
            Log(rsw, $"[=>Pipe] {PipeConfig.CmdInput} {port} {val}");
        }

        // ========== Hub 待受のみ ==========
        private void StartHubAcceptLoop(string pipeName)
        {
            Task.Run(async () =>
            {
                HubLog($"[PipeHub] AcceptLoop started (hub): {pipeName}");
                while (!_cts.IsCancellationRequested)
                {
                    NamedPipeServerStream? server = null;
                    try
                    {
                        server = new NamedPipeServerStream(
                            pipeName,
                            PipeDirection.InOut,
                            254,
                            PipeTransmissionMode.Byte,
                            PipeOptions.Asynchronous);

                        await server.WaitForConnectionAsync(_cts.Token).ConfigureAwait(false);

                        // using を使わず、所有権は ClientConn に委譲する
                        var reader = new StreamReader(server, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, bufferSize: 1024, leaveOpen: true);
                        var writer = new StreamWriter(server, Encoding.UTF8, bufferSize: 1024, leaveOpen: true) { AutoFlush = true };

                        HubLog("[PipeHub] accepted (hub)");

                        var hello = await reader.ReadLineAsync().ConfigureAwait(false);
                        HubLog($"[<=PipeHub] {hello}");

                        var rsw = ResolveRswFromHello(hello);
                        if (rsw < 0)
                        {
                            writer.WriteLine("ERR INVALID_HELLO");
                            HubLog("[=>PipeHub] ERR INVALID_HELLO");
                            reader.Dispose();
                            writer.Dispose();
                            server.Dispose();
                            continue;
                        }

                        writer.WriteLine($"OK {rsw}");
                        HubLog($"[=>PipeHub] OK {rsw}");

                        var id = Interlocked.Increment(ref _nextId);
                        var conn = new ClientConn(id, rsw, server,
                            line => OnLine(rsw, line),
                            ex => OnDisconnected(rsw, id, ex),
                            reader, writer);

                        AddClient(rsw, conn);
                        Log(rsw, $"[PipeHub] accepted id={id} (hub)");

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
                        HubLog($"[PipeHub] accept error (hub): {ex.Message}");
                        await Task.Delay(200).ConfigureAwait(false);
                    }
                }
            }, _cts.Token);
        }

        private static int ResolveRswFromHello(string? hello)
        {
            if (string.IsNullOrWhiteSpace(hello)) return -1;
            var parts = hello.Trim().Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 2 && parts[0].Equals("HELLO_NAME", StringComparison.OrdinalIgnoreCase))
            {
                var m = Regex.Match(parts[1], @"^FBIDIO(?<n>\d+)$", RegexOptions.IgnoreCase);
                if (m.Success && int.TryParse(m.Groups["n"].Value, out var rsw)) return rsw;
            }
            return -1;
        }

        private void OnLine(int rsw, string line)
        {
            if (string.IsNullOrWhiteSpace(line)) return;
            var parts = line.Trim().Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length >= 3 && parts[0].Equals(PipeConfig.CmdWrite, StringComparison.OrdinalIgnoreCase)
                && int.TryParse(parts[1], out var port) && int.TryParse(parts[2], out var val))
            {
                Log(rsw, $"[<=Pipe] {PipeConfig.CmdWrite} {port} {val}");
                if (_writeSubs.TryGetValue(rsw, out var subs))
                {
                    foreach (var h in subs.ToArray())
                    {
                        try { h(rsw, port, val); } catch { }
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
                var dead = list.FindAll(c => c.Id == id);
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
                    try { l(line); } catch { }
                }
            }
        }

        // ハブ用ログ：必ず見えるよう、Console と共通 Logger の両方へ
        private void HubLog(string msg)
        {
            var line = $"[{DateTime.Now:HH:mm:ss.fff}] {msg}";
            try { Console.WriteLine(line); } catch { }
            try { Logger.Log(line); } catch { }
        }

        private sealed class ClientConn : IDisposable
        {
            public int Id { get; }
            private readonly int _rsw;
            private readonly NamedPipeServerStream _stream;
            private readonly StreamReader _reader;
            private readonly StreamWriter _writer;
            private readonly Action<string> _onLine;
            private readonly Action<Exception?> _onClose;

            public ClientConn(int id, int rsw, NamedPipeServerStream stream, Action<string> onLine, Action<Exception?> onClose,
                              StreamReader? reader = null, StreamWriter? writer = null)
            {
                Id = id;
                _rsw = rsw;
                _stream = stream;
                _reader = reader ?? new StreamReader(stream, Encoding.UTF8);
                _writer = writer ?? new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true };
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
