using System;
using System.Collections.Concurrent;
using System.IO;
using System.IO.Pipes;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using Common; // PipeConfig

namespace IoboardEmulator
{
    internal static class StrUtil
    {
        internal static string FromAnsi(IntPtr p)
            => p == IntPtr.Zero ? string.Empty : (Marshal.PtrToStringAnsi(p) ?? string.Empty);
    }

    // Emulator専用の簡易ロガー（APP 直下に emu_link_log.txt）
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
            try
            {
                var line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [{lvl}] {msg}{Environment.NewLine}";
                lock (_sync)
                {
                    File.AppendAllText(LogPath, line, Encoding.UTF8);
                }
            }
            catch { /* logging must not break */ }
        }
    }

    internal sealed class EmuClient : IDisposable
    {
        private const int TOTAL_CONNECT_TIMEOUT_MS = 15000; // 合計15秒
        private const int FIRST_TARGET_MS = 12000;          // うち先頭候補に12秒

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

            // 正準名（PipeConfig）のみを使用（旧パイプ名は排除）
            string[] candidates = new[] { PipeConfig.PipeName };

            var remain = TOTAL_CONNECT_TIMEOUT_MS;
            for (int i = 0; i < candidates.Length && remain > 0; i++)
            {
                var budget = (i == 0) ? Math.Min(FIRST_TARGET_MS, remain)
                                      : Math.Max(1000, remain / (candidates.Length - i));

                if (TryConnectOnce(deviceName, candidates[i], budget))
                {
                    return true;
                }
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
                {
                    NewLine = "\n",
                    AutoFlush = true
                };

                _w.WriteLine($"HELLO_NAME {deviceName}");
                var line = _r.ReadLine() ?? string.Empty;
                ELog.Info($"HELLO response: '{line}'");

                var tk = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (tk.Length >= 2 && tk[0].Equals("OK", StringComparison.OrdinalIgnoreCase)
                    && int.TryParse(tk[1], out var rsw))
                {
                    Rsw = rsw;
                    ELog.Info($"Resolved RSW={Rsw}");
                    return true;
                }

                ELog.Warn("HELLO failed (not OK).");
                Dispose();
                return false;
            }
            catch (TimeoutException tex)
            {
                ELog.Err($"OpenByDeviceName timeout: {tex.Message}");
                Dispose();
                return false;
            }
            catch (Exception ex)
            {
                ELog.Err($"OpenByDeviceName exception: {ex}");
                Dispose();
                return false;
            }
        }

        public void Write(int ch, int val)
        {
            if (!IsConnected)
            {
                ELog.Warn($"WRITE skipped: not connected (ch={ch}, val={val})");
            }
            else
            {
                try
                {
                    _w!.WriteLine($"{PipeConfig.CmdWrite} {ch} {val}");
                    ELog.Info($"WRITE sent: ch={ch} val={val}");
                }
                catch (Exception ex)
                {
                    ELog.Err($"WRITE exception: {ex}");
                }
            }
        }

        public int Input(int ch)
        {
            if (!IsConnected)
            {
                ELog.Warn($"INPUT skipped: not connected (ch={ch})");
                return 0;
            }
            try
            {
                _w!.WriteLine($"{PipeConfig.CmdInput} {ch}");
                var line = _r!.ReadLine() ?? string.Empty;
                var tk = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (tk.Length >= 2 && tk[0].Equals("OK", StringComparison.OrdinalIgnoreCase)
                    && int.TryParse(tk[1], out var v))
                {
                    ELog.Info($"INPUT got: ch={ch} -> {v}");
                    return v != 0 ? 1 : 0;
                }
                ELog.Warn($"INPUT invalid response: '{line}'");
            }
            catch (Exception ex)
            {
                ELog.Err($"INPUT exception: {ex}");
            }
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

    internal sealed class Session : IDisposable
    {
        public string DeviceName = string.Empty;
        public int Rsw = -1;
        public EmuClient Client = new();

        public void Dispose() => Client.Dispose();
    }

    public static class DioApiExport
    {
        private static readonly ConcurrentDictionary<nint, Session> _map = new();
        private static nint _next = 1;

        private static nint Add(Session s)
        {
            var h = _next++;
            _map[h] = s;
            return h;
        }
        private static bool TryGet(nint h, out Session s) => _map.TryGetValue(h, out s!);

        [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) }, EntryPoint = "DioOpen")]
        public static nint DioOpen(IntPtr lpszName, uint dwFlags)
        {
            var name = StrUtil.FromAnsi(lpszName);
            if (string.IsNullOrWhiteSpace(name)) return 0;

            var s = new Session { DeviceName = name };
            if (!s.Client.OpenByDeviceName(name))
            {
                s.Dispose();
                return 0;
            }

            s.Rsw = s.Client.Rsw;
            return Add(s);
        }

        [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) }, EntryPoint = "DioClose")]
        public static uint DioClose(nint h)
        {
            if (TryGet(h, out var s))
            {
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
            s.Client.Write(no, value != 0 ? 1 : 0);
            return 0;
        }

        [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) }, EntryPoint = "DioInputByte")]
        public static unsafe uint DioInputByte(nint h, int no, byte* pValue)
        {
            if (!TryGet(h, out var s) || pValue == null) return 1;
            var bit = s.Client.Input(no);
            *pValue = (byte)(bit != 0 ? 0x01 : 0x00);
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
            Z(subsystemId); Z(subsystemVendorId); Z(interruptLine);
            Z(boardId);

            if (!TryGet(h, out var s) || s is null) return 1;

            if (boardId != null)
                *boardId = (uint)(s.Rsw < 0 ? 0 : s.Rsw);

            return 0;
        }

        [ModuleInitializer]
        internal static unsafe void __root_exports__()
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
