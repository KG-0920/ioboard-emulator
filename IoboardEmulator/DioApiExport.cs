// ===== IoboardEmulator/DioApiExport.cs =====
// 仕様: DLL は IoboardConfig.xml を読みません。
// DioOpen 時にサーバへ "HELLO_NAME <DeviceName>" を送り、サーバ側が DeviceName→RSW を解決。
// サーバが "OK <rsw>" を返したら Open 成功（ハンドル>0）。未起動/未定義は 0 を返して失敗。
// 以降 WRITE/INPUT は、その RSW で処理されます。
// ※ 既存の IoboardEmulator.PipeClient とクラス名が衝突しないよう、ここでは EmuClient を内部利用します。
// ※ unsafe は使用しません（Marshal.Copy / Marshal.WriteInt32 で代替）。

using System;
using System.Collections.Concurrent;
using System.IO;
using System.IO.Pipes;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace IoboardEmulator
{
    internal static class StrUtil
    {
        internal static string FromAnsi(IntPtr p)
            => p == IntPtr.Zero ? string.Empty : (Marshal.PtrToStringAnsi(p) ?? string.Empty);
    }

    // ★ 既存 PipeClient と衝突しない内部クライアント
    internal sealed class EmuClient : IDisposable
    {
        private readonly string _pipeName =
            Environment.GetEnvironmentVariable("IOBOARD_PIPE_NAME") ?? "ioboard_pipe";

        private NamedPipeClientStream? _pipe;
        private StreamReader? _r;
        private StreamWriter? _w;

        public int Rsw { get; private set; } = -1;
        public bool IsConnected => _pipe is { IsConnected: true };

        public bool OpenByDeviceName(string deviceName, int timeoutMs = 1500)
        {
            try
            {
                _pipe = new NamedPipeClientStream(".", _pipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
                _pipe.Connect(timeoutMs);
                _pipe.ReadMode = PipeTransmissionMode.Message;
                _pipe.ReadTimeout = timeoutMs;

                _r = new StreamReader(_pipe, Encoding.ASCII, false, 1024, leaveOpen: true);
                _w = new StreamWriter(_pipe, Encoding.ASCII, 1024, leaveOpen: true)
                {
                    NewLine = "\n",
                    AutoFlush = true
                };

                // サーバが IoboardConfig.xml で DeviceName→RSW を解決
                _w.WriteLine($"HELLO_NAME {deviceName}");

                var line = _r.ReadLine() ?? string.Empty; // 期待: "OK <rsw>" / "ERR ..."
                var tk = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (tk.Length >= 2 && tk[0].Equals("OK", StringComparison.OrdinalIgnoreCase)
                    && int.TryParse(tk[1], out var rsw))
                {
                    Rsw = rsw;
                    return true;
                }

                Dispose();
                return false;
            }
            catch
            {
                Dispose();
                return false;
            }
        }

        public void Write(int ch, int val)
        {
            if (!IsConnected) return;
            try { _w!.WriteLine($"WRITE {ch} {val}"); } catch { /* ignore */ }
        }

        public int Input(int ch)
        {
            if (!IsConnected) return 0;
            try
            {
                _w!.WriteLine($"INPUT {ch}");
                var line = _r!.ReadLine() ?? string.Empty; // 期待: "OK <0|1>"
                var tk = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (tk.Length >= 2 && tk[0].Equals("OK", StringComparison.OrdinalIgnoreCase)
                    && int.TryParse(tk[1], out var v)) return v != 0 ? 1 : 0;
            }
            catch { /* ignore */ }
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

        // ===================== Exports =====================

        [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) }, EntryPoint = "DioOpen")]
        public static nint DioOpen(IntPtr lpszName, uint dwFlags)
        {
            // サーバ未起動 or 未定義デバイス → 0 を返して失敗
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
        public static int DioClose(nint h)
        {
            if (TryGet(h, out var s))
            {
                s.Dispose();
                _ = _map.TryRemove(h, out _);
                return 0;
            }
            return -1;
        }

        [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) }, EntryPoint = "DioOutputByte")]
        public static int DioOutputByte(nint h, int ch, int val)
        {
            if (!TryGet(h, out var s)) return -1;
            s.Client.Write(ch, val != 0 ? 1 : 0);
            return 0;
        }

        [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) }, EntryPoint = "DioInputByte")]
        public static int DioInputByte(nint h, int ch)
        {
            if (!TryGet(h, out var s)) return 0;
            return s.Client.Input(ch);
        }

        [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) }, EntryPoint = "DioCommonGetPciDeviceInfo")]
        public static int DioCommonGetPciDeviceInfo(nint h, IntPtr pInfo /* out buf */)
        {
            // 先頭64bytes=DeviceName(ANSI/NUL終端), 続く4bytes=RSW(int) を書き込む（unsafe 不使用）
            if (!TryGet(h, out var s) || pInfo == IntPtr.Zero) return -1;

            try
            {
                var buf = new byte[64];
                var nameBytes = Encoding.ASCII.GetBytes(s.DeviceName);
                var n = Math.Min(nameBytes.Length, 63);
                Array.Copy(nameBytes, 0, buf, 0, n);
                buf[n] = 0; // NUL 終端

                Marshal.Copy(buf, 0, pInfo, 64);            // 文字列部
                Marshal.WriteInt32(pInfo, 64, s.Rsw);       // 直後に RSW
                return 0;
            }
            catch
            {
                return -1;
            }
        }

        // 置き換え後（unsafe を付けるだけ）
        [System.Runtime.CompilerServices.ModuleInitializer]
        internal static unsafe void __root_exports__()  // ← unsafe を付与
        {
            // これで各メソッドが「参照あり」と見なされ、AOT で生存しやすくなる
            _ = (delegate* unmanaged[Stdcall]<nint, uint, nint>)&DioApiExport.DioOpen;
            _ = (delegate* unmanaged[Stdcall]<nint, int>)&DioApiExport.DioClose;
            _ = (delegate* unmanaged[Stdcall]<nint, int, int>)&DioApiExport.DioInputByte;
            _ = (delegate* unmanaged[Stdcall]<nint, int, int, int>)&DioApiExport.DioOutputByte;
            _ = (delegate* unmanaged[Stdcall]<nint, IntPtr, int>)&DioApiExport.DioCommonGetPciDeviceInfo;
        }
    }
}
