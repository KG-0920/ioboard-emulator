using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

namespace IoBoardWrapper
{
    public class IoboardWrapper : IIoBoardController
    {
#if DEBUG
        private const string Native = "IoboardEmulator";
#else
        private const string Native = "fbidio";
#endif
        // 共通：戻り値0=SUCCESS を想定（fbidio準拠）
        private const uint ERROR_SUCCESS      = 0;
        private const uint FBIDIO_FLAG_NORMAL = 0x0000;
        private const uint FBIDIO_FLAG_SHARE  = 0x0002;

        // ========= DLL 解決（エミュ/実機どちらでも使う）=========
        static IoboardWrapper()
        {
            NativeLibrary.SetDllImportResolver(typeof(IoboardWrapper).Assembly, ResolveNative);
        }
        private static IntPtr ResolveNative(string libraryName, Assembly assembly, DllImportSearchPath? _)
        {
            // DLL名が一致したら、実行ディレクトリ・親・環境変数IOBOARD_DLL_DIRなどから探す
            bool IsTarget(string n) => string.Equals(n, Native, StringComparison.OrdinalIgnoreCase)
                                     || string.Equals(n, "IoboardEmulator", StringComparison.OrdinalIgnoreCase)
                                     || string.Equals(n, "fbidio", StringComparison.OrdinalIgnoreCase);
            if (!IsTarget(libraryName)) return IntPtr.Zero;

            foreach (var dir in CandidateDirs())
            {
                var path = Path.Combine(dir, libraryName + ".dll");
                if (File.Exists(path) && NativeLibrary.TryLoad(path, out var h)) return h;
            }
            return IntPtr.Zero; // 既定探索にフォールバック
        }
        private static IEnumerable<string> CandidateDirs()
        {
            var list = new List<string>();
            var baseDir = AppContext.BaseDirectory ?? "";
            if (!string.IsNullOrEmpty(baseDir)) list.Add(baseDir);
            try { var p1 = Directory.GetParent(baseDir!)?.FullName; if (!string.IsNullOrEmpty(p1)) list.Add(p1!); } catch { }
            try
            {
                var p1 = Directory.GetParent(baseDir!)?.FullName;
                var p2 = string.IsNullOrEmpty(p1) ? null : Directory.GetParent(p1!)?.FullName;
                if (!string.IsNullOrEmpty(p2)) list.Add(p2!);
            } catch { }
            var env = Environment.GetEnvironmentVariable("IOBOARD_DLL_DIR");
            if (!string.IsNullOrEmpty(env))
            {
                foreach (var s in env.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    var t = s.Trim(); if (!string.IsNullOrEmpty(t)) list.Add(t);
                }
            }
            var cwd = Directory.GetCurrentDirectory();
            if (!string.IsNullOrEmpty(cwd)) list.Add(cwd);
            return list;
        }

        // ========= ネイティブ（fbidioと同一シグネチャ）=========
        [DllImport(Native, EntryPoint = "DioOpen", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        private static extern IntPtr DioOpen(string name, uint flags);

        [DllImport(Native, EntryPoint = "DioClose", CallingConvention = CallingConvention.StdCall)]
        private static extern uint DioClose(IntPtr h);

        [DllImport(Native, EntryPoint = "DioInputByte", CallingConvention = CallingConvention.StdCall)]
        private static extern uint DioInputByte(IntPtr h, int nNo, out byte value);

        [DllImport(Native, EntryPoint = "DioOutputByte", CallingConvention = CallingConvention.StdCall)]
        private static extern uint DioOutputByte(IntPtr h, int nNo, byte value);

        [DllImport(Native, EntryPoint = "DioCommonGetPciDeviceInfo", CallingConvention = CallingConvention.StdCall)]
        private static extern uint DioCommonGetPciDeviceInfo(
            IntPtr h,
            out uint deviceId, out uint venderId, out uint classCode, out uint revisionId,
            out uint ba0, out uint ba1, out uint ba2, out uint ba3, out uint ba4, out uint ba5,
            out uint subsystemId, out uint subsystemVenderId, out uint interruptLine,
            out uint boardId);

        // ========= 状態 =========
        private const int MAX_OUTPUT_BITS  = 64;
        private const int OUTPUT_BYTES_LEN = (MAX_OUTPUT_BITS + 7) / 8;

        private static readonly object _sync = new();
        private static readonly Dictionary<int, IntPtr> _handleByRsw    = new();
        private static readonly Dictionary<int, byte[]> _outShadowByRsw = new();

        // ========= IIoBoardController =========
        public bool Open(int rotarySwitchNo)
        {
            // FBIDIO0..255 を SHARE で走査し、BoardID==rsw のハンドルを探す
            IntPtr found = IntPtr.Zero;
            for (int i = 0; i <= 255; i++)
            {
                string name = $"FBIDIO{i}";
                IntPtr h = DioOpen(name, FBIDIO_FLAG_SHARE);
                if (h == IntPtr.Zero) continue;

                bool match = false;
                try
                {
                    var rc = DioCommonGetPciDeviceInfo(
                        h, out _, out _, out _, out _,
                        out _, out _, out _, out _, out _, out _,
                        out _, out _, out _,
                        out uint boardId);

                    if (rc == ERROR_SUCCESS && boardId == (uint)rotarySwitchNo)
                    {
                        found = h;
                        match = true;
                        break;
                    }
                }
                finally
                {
                    if (!match) { try { DioClose(h); } catch { } }
                }
            }

            if (found == IntPtr.Zero) return false;

            lock (_sync)
            {
                if (_handleByRsw.ContainsKey(rotarySwitchNo))
                {
                    try { DioClose(found); } catch { }
                    return true;
                }
                _handleByRsw[rotarySwitchNo] = found;
                if (!_outShadowByRsw.ContainsKey(rotarySwitchNo))
                    _outShadowByRsw[rotarySwitchNo] = new byte[OUTPUT_BYTES_LEN]; // 全OFF
                return true;
            }
        }

        public void Close(int rotarySwitchNo)
        {
            lock (_sync)
            {
                if (_handleByRsw.TryGetValue(rotarySwitchNo, out var h))
                {
                    try { DioClose(h); } catch { }
                    _handleByRsw.Remove(rotarySwitchNo);
                }
                _outShadowByRsw.Remove(rotarySwitchNo);
            }
        }

        public void WriteOutput(int rotarySwitchNo, int port, bool value)
        {
            if (port < 0) return;
            int byteIndex = port / 8;
            int bitIndex  = port % 8;

            lock (_sync)
            {
                if (!_handleByRsw.TryGetValue(rotarySwitchNo, out var h) || h == IntPtr.Zero) return;

                if (!_outShadowByRsw.TryGetValue(rotarySwitchNo, out var shadow))
                {
                    shadow = new byte[OUTPUT_BYTES_LEN];
                    _outShadowByRsw[rotarySwitchNo] = shadow;
                }

                byte mask = (byte)(1 << bitIndex);
                if (value) shadow[byteIndex] = (byte)(shadow[byteIndex] |  mask);
                else       shadow[byteIndex] = (byte)(shadow[byteIndex] & ~mask);

                DioOutputByte(h, byteIndex, shadow[byteIndex]); // 1バイト単位で出力
            }
        }

        public bool ReadInput(int rotarySwitchNo, int port)
        {
            if (port < 0) return false;
            int byteIndex = port / 8;
            int bitIndex  = port % 8;

            lock (_sync)
            {
                if (!_handleByRsw.TryGetValue(rotarySwitchNo, out var h) || h == IntPtr.Zero) return false;

                var rc = DioInputByte(h, byteIndex, out byte val);
                if (rc != ERROR_SUCCESS) return false;
                return ((val >> bitIndex) & 0x01) != 0;
            }
        }
    }
}
