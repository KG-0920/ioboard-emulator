using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

namespace IoBoardWrapper
{
    /// <summary>
    /// Debug = IoboardEmulator.dll（1bit API）
    /// Release = fbidio.dll（Byte API：DioOpen(string), DioInputByte, DioOutputByte, DioCommonGetPciDeviceInfo）
    /// 公開I/F（IIoBoardController）は変更なし。
    /// </summary>
    public class IoboardWrapper : IIoBoardController
    {
        // ===== 共通：ネイティブDLL解決（従来の探索順を維持） =====
        static IoboardWrapper()
        {
            NativeLibrary.SetDllImportResolver(typeof(IoboardWrapper).Assembly, ResolveNative);
        }

        private static IntPtr ResolveNative(string libraryName, Assembly assembly, DllImportSearchPath? _)
        {
#if DEBUG
            // Debug は IoboardEmulator を必ず使う
            if (!libraryName.Equals("IoboardEmulator", StringComparison.OrdinalIgnoreCase)) return IntPtr.Zero;
            foreach (var dir in CandidateDirs())
            {
                var path = Path.Combine(dir, "IoboardEmulator.dll");
                if (File.Exists(path) && NativeLibrary.TryLoad(path, out var h)) return h;
            }
#else
            // Release は fbidio を必ず使う
            if (!libraryName.Equals("fbidio", StringComparison.OrdinalIgnoreCase)) return IntPtr.Zero;
            foreach (var dir in CandidateDirs())
            {
                var path = Path.Combine(dir, "fbidio.dll");
                if (File.Exists(path) && NativeLibrary.TryLoad(path, out var h)) return h;
            }
#endif
            return IntPtr.Zero; // 既定解決に委譲
        }

        private static IEnumerable<string> CandidateDirs()
        {
            // CS1626回避のため yield を使わずに組み立て
            var list = new List<string>();

            // 0) EXE配置ディレクトリ
            var baseDir = AppContext.BaseDirectory;
            if (!string.IsNullOrEmpty(baseDir)) list.Add(baseDir);

            // 1) 親ディレクトリ
            try
            {
                var p1 = Directory.GetParent(baseDir!)?.FullName;
                if (!string.IsNullOrEmpty(p1)) list.Add(p1);
            }
            catch { }

            // 2) 親の親
            try
            {
                var p1 = Directory.GetParent(baseDir!)?.FullName;
                var p2 = string.IsNullOrEmpty(p1) ? null : Directory.GetParent(p1!)?.FullName;
                if (!string.IsNullOrEmpty(p2)) list.Add(p2);
            }
            catch { }

            // 3) 環境変数 IOBOARD_DLL_DIR（;区切り）
            var env = Environment.GetEnvironmentVariable("IOBOARD_DLL_DIR");
            if (!string.IsNullOrEmpty(env))
            {
                foreach (var p in env.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    var s = p.Trim();
                    if (!string.IsNullOrEmpty(s)) list.Add(s);
                }
            }

            // 4) 現在の作業ディレクトリ
            var cwd = Directory.GetCurrentDirectory();
            if (!string.IsNullOrEmpty(cwd)) list.Add(cwd);

            return list;
        }

        // ====== IIoBoardController ======
        public bool Open(int rotarySwitchNo)
        {
#if DEBUG
            return Emu_DioOpen(rotarySwitchNo) != 0;
#else
            return Real_OpenWithBoardId(rotarySwitchNo);
#endif
        }

        public void Close(int rotarySwitchNo)
        {
#if DEBUG
            try { Emu_DioClose(rotarySwitchNo); } catch { }
#else
            lock (_sync)
            {
                if (_handleByRsw.TryGetValue(rotarySwitchNo, out var h))
                {
                    try { Real_DioClose(h); } catch { }
                    _handleByRsw.Remove(rotarySwitchNo);
                }
                _outShadowByRsw.Remove(rotarySwitchNo);
            }
#endif
        }

        public void WriteOutput(int rotarySwitchNo, int port, bool value)
        {
#if DEBUG
            Emu_DioWriteOutput(rotarySwitchNo, port, value ? 1 : 0);
#else
            if (port < 0) return;
            int byteIndex = port / 8;
            int bitIndex  = port % 8;
            if (byteIndex >= OUTPUT_BYTES_LEN) return;

            lock (_sync)
            {
                if (!_handleByRsw.TryGetValue(rotarySwitchNo, out var h) || h == IntPtr.Zero) return;

                if (!_outShadowByRsw.TryGetValue(rotarySwitchNo, out var shadow))
                {
                    shadow = new byte[OUTPUT_BYTES_LEN]; // 初期値OFF
                    _outShadowByRsw[rotarySwitchNo] = shadow;
                }

                byte mask = (byte)(1 << bitIndex);
                if (value) shadow[byteIndex] = (byte)(shadow[byteIndex] |  mask);
                else       shadow[byteIndex] = (byte)(shadow[byteIndex] & ~mask);

                // nNo は 0-based のバイトインデックス想定
                Real_DioOutputByte(h, byteIndex, shadow[byteIndex]);
            }
#endif
        }

        public bool ReadInput(int rotarySwitchNo, int port)
        {
#if DEBUG
            return Emu_DioReadInput(rotarySwitchNo, port) != 0;
#else
            if (port < 0) return false;
            int byteIndex = port / 8;
            int bitIndex  = port % 8;

            lock (_sync)
            {
                if (!_handleByRsw.TryGetValue(rotarySwitchNo, out var h) || h == IntPtr.Zero) return false;

                var rc = Real_DioInputByte(h, byteIndex, out byte val);
                if (rc != FBIDIO_ERROR_SUCCESS) return false; // エラーはOFF扱い
                return ((val >> bitIndex) & 0x01) != 0;
            }
#endif
        }

        // ========= Debug（エミュレータ） =========
#if DEBUG
        private const string EmuDll = "IoboardEmulator";

        [DllImport(EmuDll, EntryPoint = "DioOpen", CallingConvention = CallingConvention.StdCall)]
        private static extern int Emu_DioOpen(int rotarySwitchNo);

        [DllImport(EmuDll, EntryPoint = "DioClose", CallingConvention = CallingConvention.StdCall)]
        private static extern void Emu_DioClose(int rotarySwitchNo);

        [DllImport(EmuDll, EntryPoint = "DioWriteOutput", CallingConvention = CallingConvention.StdCall)]
        private static extern int Emu_DioWriteOutput(int rotarySwitchNo, int port, int value);

        [DllImport(EmuDll, EntryPoint = "DioReadInput", CallingConvention = CallingConvention.StdCall)]
        private static extern int Emu_DioReadInput(int rotarySwitchNo, int port);

        // Debug では追加状態は不要（1bit API のため）
#else
        // ========= Release（実機） =========

        // IFCDIO.cs より
        private const uint FBIDIO_ERROR_SUCCESS = 0;
        private const uint FBIDIO_FLAG_NORMAL   = 0x0000;
        private const uint FBIDIO_FLAG_SHARE    = 0x0002;

        private const int MAX_OUTPUT_BITS  = 64;                 // 想定：最大64点
        private const int OUTPUT_BYTES_LEN = (MAX_OUTPUT_BITS + 7) / 8; // 8

        private static readonly object _sync = new();
        private static readonly Dictionary<int, IntPtr> _handleByRsw    = new();
        private static readonly Dictionary<int, byte[]> _outShadowByRsw = new();

        private const string RealDll = "fbidio";

        [DllImport(RealDll, EntryPoint = "DioOpen", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        private static extern IntPtr Real_DioOpen(string name, uint flags);

        [DllImport(RealDll, EntryPoint = "DioClose", CallingConvention = CallingConvention.StdCall)]
        private static extern uint Real_DioClose(IntPtr h);

        [DllImport(RealDll, EntryPoint = "DioInputByte", CallingConvention = CallingConvention.StdCall)]
        private static extern uint Real_DioInputByte(IntPtr h, int nNo, out byte value);

        [DllImport(RealDll, EntryPoint = "DioOutputByte", CallingConvention = CallingConvention.StdCall)]
        private static extern uint Real_DioOutputByte(IntPtr h, int nNo, byte value);

        [DllImport(RealDll, EntryPoint = "DioCommonGetPciDeviceInfo", CallingConvention = CallingConvention.StdCall)]
        private static extern uint Real_DioCommonGetPciDeviceInfo(
            IntPtr h,
            out uint pdwDeviceID, out uint pdwVenderID, out uint pdwClassCode, out uint pdwRevisionID,
            out uint pdwBaseAddress0, out uint pdwBaseAddress1, out uint pdwBaseAddress2,
            out uint pdwBaseAddress3, out uint pdwBaseAddress4, out uint pdwBaseAddress5,
            out uint pdwSubsystemID, out uint pdwSubsystemVenderID, out uint pdwInterruptLine,
            out uint pdwBoardID);

        private static bool Real_OpenWithBoardId(int rsw)
        {
            lock (_sync)
            {
                if (_handleByRsw.ContainsKey(rsw)) return true;

                // FBIDIO1..255 を SHARE で探索 → BoardID == RSW のものを採用
                for (int i = 1; i <= 255; i++)
                {
                    string name = $"FBIDIO{i}";
                    IntPtr h = Real_DioOpen(name, FBIDIO_FLAG_SHARE);
                    if (h == IntPtr.Zero) continue;

                    bool match = false;
                    try
                    {
                        var rc = Real_DioCommonGetPciDeviceInfo(
                            h,
                            out _, out _, out _, out _,
                            out _, out _, out _, out _, out _, out _,
                            out _, out _, out _,
                            out uint boardId);

                        if (rc == FBIDIO_ERROR_SUCCESS && boardId == (uint)rsw)
                        {
                            _handleByRsw[rsw] = h;
                            if (!_outShadowByRsw.ContainsKey(rsw))
                                _outShadowByRsw[rsw] = new byte[OUTPUT_BYTES_LEN]; // 初期値OFF
                            match = true;
                            return true;
                        }
                    }
                    finally
                    {
                        if (!match)
                        {
                            try { Real_DioClose(h); } catch { }
                        }
                    }
                }

                return false; // 見つからず
            }
        }
#endif
    }
}
