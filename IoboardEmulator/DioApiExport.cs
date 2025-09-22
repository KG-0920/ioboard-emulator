//#define IOBOARD_TRACE   // ← 通常OFF。必要なときだけプロジェクト設定で定義
using System;
using System.Collections.Concurrent;
using System.IO.Pipes;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Common; // PipeConfig, DiagTrace

namespace IoboardEmulator
{
    internal static class StrUtil
    {
        internal static string FromAnsi(IntPtr p)
            => p == IntPtr.Zero ? string.Empty : (Marshal.PtrToStringAnsi(p) ?? string.Empty);
    }

    // （必要時のみ）簡易ファイルログ
    internal static class ELog
    {
        private static readonly object _sync = new();
        private static string LogPath =>
            Path.Combine(AppContext.BaseDirectory ?? Environment.CurrentDirectory, "emu_link_log.txt");
        internal static void Info(string msg) => Write("INFO", msg);
        internal static void Warn(string msg) => Write("WARN", msg);
        internal static void Err (string msg) => Write("ERR ", msg);
        private static void Write(string lvl, string msg)
        {
#if IOBOARD_TRACE
            try
            {
                var line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [{lvl}] {msg}{Environment.NewLine}";
                lock (_sync) File.AppendAllText(LogPath, line, Encoding.UTF8);
            }
            catch { }
#endif
        }
    }

    // パイプ1本を握る軽量クライアント（既存と同等）
    internal sealed class EmuClient : IDisposable
    {
        private const int TOTAL_CONNECT_TIMEOUT_MS = 15000;
        private const int FIRST_TARGET_MS = 12000;

        private NamedPipeClientStream? _pipe;
        private StreamReader? _r;
        private StreamWriter? _w;

        public int Rsw { get; private set; } = -1;
        public bool IsConnected => _pipe is { IsConnected: true };

        private static int ParseRswFromDevice(string deviceName)
        {
            var m = Regex.Match(deviceName ?? string.Empty, @"^FBIDIO(?<n>\d+)$", RegexOptions.IgnoreCase);
            return (m.Success && int.TryParse(m.Groups["n"].Value, out var n)) ? n : -1;
        }

        public bool OpenByDeviceName(string deviceName)
        {
            var rsw = ParseRswFromDevice(deviceName);
            Rsw = rsw;

            string[] candidates = new[] { PipeConfig.PipeName }; // 正準名
            var remain = TOTAL_CONNECT_TIMEOUT_MS;
            for (int i = 0; i < candidates.Length && remain > 0; i++)
            {
                var budget = (i == 0) ? Math.Min(FIRST_TARGET_MS, remain)
                                      : Math.Max(1000, remain / (candidates.Length - i));
                if (TryConnectOnce(deviceName, candidates[i], budget)) return true;
                remain -= budget;
            }
            return false;
        }

        private bool TryConnectOnce(string deviceName, string pipeName, int timeoutMs)
        {
            ELog.Info($"OpenByDeviceName: name='{deviceName}', pipe='{pipeName}'");
            try
            {
                _pipe = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
                _pipe.Connect(timeoutMs);

                _r = new StreamReader(_pipe, Encoding.ASCII, false, 1024, leaveOpen: true);
                _w = new StreamWriter(_pipe, Encoding.ASCII, 1024, leaveOpen: true)
                { NewLine = "\n", AutoFlush = true };

                _w.WriteLine($"HELLO_NAME {deviceName}");
                var line = _r.ReadLine() ?? string.Empty;

                var tk = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (tk.Length >= 2 && tk[0].Equals("OK", StringComparison.OrdinalIgnoreCase)
                    && int.TryParse(tk[1], out var rsw))
                {
                    Rsw = rsw;
#if IOBOARD_TRACE
                    DiagTrace.Write("HELLO", Rsw, -1, false, "DLL");
#endif
                    return true;
                }
                ELog.Warn("HELLO failed (not OK).");
                Dispose();
                return false;
            }
            catch (TimeoutException tex) { ELog.Err($"Open timeout: {tex.Message}"); Dispose(); return false; }
            catch (Exception ex)         { ELog.Err($"Open exception: {ex}");       Dispose(); return false; }
        }

        public void Write(int port, int val)
        {
            if (!IsConnected) { ELog.Warn($"WRITE skipped: not connected (port={port}, val={val})"); return; }
            try
            {
                _w!.WriteLine($"{PipeConfig.CmdWrite} {port} {val}");
#if IOBOARD_TRACE
                DiagTrace.Write("WRITE", Rsw, port, val != 0, "DLL");
#endif
            }
            catch (Exception ex) { ELog.Err($"WRITE exception: {ex}"); }
        }

        public int Input(int port)
        {
            if (!IsConnected) { ELog.Warn($"INPUT skipped: not connected (port={port})"); return 0; }
            try
            {
                _w!.WriteLine($"{PipeConfig.CmdInput} {port}");
                // サーバは「OK <0|1>」のみ返す仕様（§7）。:contentReference[oaicite:1]{index=1}
                var line = _r!.ReadLine() ?? string.Empty;
                var tk = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (tk.Length >= 2 && tk[0].Equals("OK", StringComparison.OrdinalIgnoreCase)
                    && int.TryParse(tk[1], out var v))
                {
#if IOBOARD_TRACE
                    DiagTrace.Write("INPUT", Rsw, port, v != 0, "DLL");
#endif
                    return v != 0 ? 1 : 0;
                }
                ELog.Warn($"INPUT invalid response: '{line}'");
            }
            catch (Exception ex) { ELog.Err($"INPUT exception: {ex}"); }
            return 0;
        }

        public void Dispose()
        {
            try { _w?.Dispose(); } catch { }
            try { _r?.Dispose(); } catch { }
            try { _pipe?.Dispose(); } catch { }
            _w = null; _r = null; _pipe = null;
        }
    }

    // ── ここから：セッション（ハンドル）ごとの入力キャッシュ＋ポーラー ──
    internal sealed class Session : IDisposable
    {
        public string DeviceName = string.Empty;
        public int Rsw = -1;
        public readonly EmuClient Client = new();

        // 入力キャッシュ（byte単位：0..7 = 0..63bit）。読み/書きともロック不要（byteは原子的）
        public readonly byte[] InBytes = new byte[8];

        // BGポーリング
        private CancellationTokenSource? _cts;
        private Task? _pollTask;

        // ポーリング周期（ms）— 提案どおり 50ms を既定
        private const int PollIntervalMs = 50;

        public void StartPolling()
        {
            if (_pollTask != null) return;

            // 最初に1巡だけ同期で埋める（初期値が0で見えない問題を避ける）
            PollOnce();

            _cts = new CancellationTokenSource();
            var ct = _cts.Token;
            _pollTask = Task.Run(async () =>
            {
                try
                {
                    while (!ct.IsCancellationRequested)
                    {
                        PollOnce();
                        await Task.Delay(PollIntervalMs, ct).ConfigureAwait(false);
                    }
                }
                catch (OperationCanceledException) { /* normal */ }
            }, ct);
        }

        private void PollOnce()
        {
            // 8バイト（最大64点）分を順に埋める
            for (int no = 0; no < InBytes.Length; no++)
            {
                int basePort = no * 8;
                int accum = 0;
                for (int bit = 0; bit < 8; bit++)
                {
                    int v = Client.Input(basePort + bit); // 0/1
                    if (v != 0) accum |= (1 << bit);
                }
                InBytes[no] = (byte)accum;
            }
        }

        public void StopPolling()
        {
            try { _cts?.Cancel(); } catch { }
            try { _pollTask?.Wait(100); } catch { }
            _cts = null; _pollTask = null;
        }

        public void Dispose()
        {
            StopPolling();
            Client.Dispose();
        }
    }

    public static class DioApiExport
    {
        private static readonly ConcurrentDictionary<nint, Session> _map = new();
        private static nint _next = 1;

        private static nint Add(Session s) { var h = _next++; _map[h] = s; return h; }
        private static bool TryGet(nint h, out Session s) => _map.TryGetValue(h, out s!);

        [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) }, EntryPoint = "DioOpen")]
        public static nint DioOpen(IntPtr lpszName, uint dwFlags)
        {
            var name = StrUtil.FromAnsi(lpszName);
            if (string.IsNullOrWhiteSpace(name)) return 0;

            var s = new Session { DeviceName = name };
            if (!s.Client.OpenByDeviceName(name)) { s.Dispose(); return 0; }

            s.Rsw = s.Client.Rsw;
            s.StartPolling();                // ★ 提案どおり：DLL側で定周期ポーリング開始
            return Add(s);
        }

        [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) }, EntryPoint = "DioClose")]
        public static uint DioClose(nint h)
        {
            if (TryGet(h, out var s))
            {
                s.StopPolling();
                s.Dispose();
                _ = _map.TryRemove(h, out _);
                return 0;
            }
            return 1;
        }

        [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) }, EntryPoint = "DioOutputByte")]
        public static uint DioOutputByte(nint h, int no, byte value)
        {
            if (!TryGet(h, out var s)) return 1;

            // サーバはポート単位プロトコルなので8bit展開（既存踏襲）
            int basePort = no * 8;
            for (int bit = 0; bit < 8; bit++)
            {
                int port   = basePort + bit;
                int bitVal = ((value >> bit) & 0x01);
                s.Client.Write(port, bitVal);
            }
#if IOBOARD_TRACE
            DiagTrace.Write("WRITE", s.Rsw, no, value != 0, "DLL");
#endif
            return 0;
        }

        [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) }, EntryPoint = "DioInputByte")]
        public static unsafe uint DioInputByte(nint h, int no, byte* pValue)
        {
            if (!TryGet(h, out var s) || pValue == null) return 1;

            // ★ 提案どおり：通信せずキャッシュを即返す
            byte val = 0;
            if (no >= 0 && no < s.InBytes.Length) val = s.InBytes[no];
            *pValue = val;
#if IOBOARD_TRACE
            DiagTrace.Write("INPUT", s.Rsw, no, val != 0, "DLL-Cached");
#endif
            return 0;
        }

        [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) }, EntryPoint = "DioCommonGetPciDeviceInfo")]
        public static unsafe uint DioCommonGetPciDeviceInfo(
            nint h,
            uint* deviceId, uint* vendorId, uint* classCode, uint* revisionId,
            uint* baseAddress0, uint* baseAddress1, uint* baseAddress2,
            uint* baseAddress3, uint* baseAddress4, uint* baseAddress5,
            uint* subsystemId, uint* subsystemVendorId, uint* interruptLine,
            uint* boardId)
        {
            void Z(uint* p) { if (p != null) *p = 0; }
            Z(deviceId); Z(vendorId); Z(classCode); Z(revisionId);
            Z(baseAddress0); Z(baseAddress1); Z(baseAddress2);
            Z(baseAddress3); Z(baseAddress4); Z(baseAddress5);
            Z(subsystemId); Z(subsystemVendorId); Z(interruptLine); Z(boardId);

            if (!TryGet(h, out var s) || s is null) return 1;
            if (boardId != null) *boardId = (uint)(s.Rsw < 0 ? 0 : s.Rsw);
            return 0;
        }

        [ModuleInitializer]
        internal static unsafe void __root_exports__() // keep
        {
            _ = (delegate* unmanaged[Stdcall]<nint, uint, nint>)&DioApiExport.DioOpen;
            _ = (delegate* unmanaged[Stdcall]<nint, uint>)&DioApiExport.DioClose;
            _ = (delegate* unmanaged[Stdcall]<nint, int, byte, uint>)&DioApiExport.DioOutputByte;
            _ = (delegate* unmanaged[Stdcall]<nint, int, byte*, uint>)&DioApiExport.DioInputByte;
            _ = (delegate* unmanaged[Stdcall]<
                    nint,
                    uint*, uint*, uint*, uint*,
                    uint*, uint*, uint*,
                    uint*, uint*, uint*,
                    uint*, uint*, uint*,
                    uint*,
                    uint>)&DioApiExport.DioCommonGetPciDeviceInfo;
        }
    }
}
