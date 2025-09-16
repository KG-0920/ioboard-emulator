using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Diagnostics;
using SharedConfig;

namespace IoBoardWrapper
{
    public class IoboardWrapper : IIoBoardController
    {
#if DEBUG
        private const string Native = "IoboardEmulator";
#else
        private const string Native = "fbidio";
#endif
        private const uint ERROR_SUCCESS      = 0;
        private const uint FBIDIO_FLAG_NORMAL = 0x0000;
        private const uint FBIDIO_FLAG_SHARE  = 0x0002;

        static IoboardWrapper()
        {
            try
            {
                NativeLibrary.SetDllImportResolver(typeof(IoboardWrapper).Assembly, ResolveNative);
                IoLogger.Info($"IoboardWrapper static init. Native='{Native}' BaseDir='{AppContext.BaseDirectory}'");
            }
            catch (Exception ex)
            {
                IoLogger.Error("IoboardWrapper static init failed.", ex);
            }
        }
        private static IntPtr ResolveNative(string libraryName, Assembly assembly, DllImportSearchPath? _)
        {
            bool IsTarget(string n) => string.Equals(n, Native, StringComparison.OrdinalIgnoreCase)
                                     || string.Equals(n, "IoboardEmulator", StringComparison.OrdinalIgnoreCase)
                                     || string.Equals(n, "fbidio", StringComparison.OrdinalIgnoreCase);
            if (!IsTarget(libraryName)) return IntPtr.Zero;

            foreach (var dir in CandidateDirs())
            {
                var path = Path.Combine(dir, libraryName + ".dll");
                if (File.Exists(path) && NativeLibrary.TryLoad(path, out var h))
                {
                    IoLogger.Info($"Native DLL loaded: {path}");
                    return h;
                }
            }
            IoLogger.Warn($"Native DLL not found by resolver. libraryName='{libraryName}'");
            return IntPtr.Zero;
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

        [DllImport(Native, EntryPoint = "DioOpen", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        private static extern IntPtr DioOpen(string name, uint flags);

        [DllImport(Native, EntryPoint = "DioClose", CallingConvention = CallingConvention.StdCall)]
        private static extern uint DioClose(IntPtr h);

        [DllImport(Native, EntryPoint = "DioInputByte", CallingConvention = CallingConvention.StdCall)]
        private static extern uint DioInputByte(IntPtr h, int port, out byte val);

        [DllImport(Native, EntryPoint = "DioOutputByte", CallingConvention = CallingConvention.StdCall)]
        private static extern uint DioOutputByte(IntPtr h, int port, byte val);

        // ★ 定義修正：本物の15引数
        [DllImport(Native, EntryPoint = "DioCommonGetPciDeviceInfo", CallingConvention = CallingConvention.StdCall)]
        private static extern uint DioCommonGetPciDeviceInfo(
            IntPtr h,
            out uint deviceId, out uint vendorId, out uint classCode, out uint revisionId,
            out uint baseAddress0, out uint baseAddress1, out uint baseAddress2,
            out uint baseAddress3, out uint baseAddress4, out uint baseAddress5,
            out uint subsystemId, out uint subsystemVendorId, out uint interruptLine,
            out uint boardId);

        private const int OUTPUT_BYTES_LEN = 4;

        private static readonly object _sync = new();
        private static readonly Dictionary<int, IntPtr> _handleByRsw    = new();
        private static readonly Dictionary<int, byte[]> _outShadowByRsw = new();

        public bool Open(int rotarySwitchNo)
        {
            var sw = Stopwatch.StartNew();
            try
            {
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
                            h,
                            out _, out _, out _, out _,
                            out _, out _, out _,
                            out _, out _, out _,
                            out _, out _, out _,
                            out uint boardId);

                        if (rc == ERROR_SUCCESS && boardId == (uint)rotarySwitchNo)
                        {
                            found = h;
                            match = true;
                            break;
                        }
                    }
                    catch (Exception exInfo)
                    {
                        IoLogger.Error($"DioCommonGetPciDeviceInfo failed at index={i}", exInfo);
                    }

                    if (!match)
                    {
                        try { _ = DioClose(h); } catch (Exception exClose) { IoLogger.Error($"DioClose failed at index={i}", exClose); }
                    }
                }

                if (found == IntPtr.Zero)
                {
                    IoLogger.Warn($"Open failed: RSW={rotarySwitchNo} not found.");
                    return false;
                }

                lock (_sync)
                {
                    _handleByRsw[rotarySwitchNo] = found;
                    if (!_outShadowByRsw.TryGetValue(rotarySwitchNo, out var shadow) || shadow == null || shadow.Length != OUTPUT_BYTES_LEN)
                    {
                        _outShadowByRsw[rotarySwitchNo] = new byte[OUTPUT_BYTES_LEN];
                    }
                }

                sw.Stop();
                if (sw.ElapsedMilliseconds > 1000)
                    IoLogger.Warn($"Open took {sw.ElapsedMilliseconds} ms (RSW={rotarySwitchNo})");
                else
                    IoLogger.Info($"Open ok (RSW={rotarySwitchNo}, {sw.ElapsedMilliseconds} ms)");
                return true;
            }
            catch (DllNotFoundException exDll)
            {
                sw.Stop();
                IoLogger.Error($"Open DllNotFound (RSW={rotarySwitchNo})", exDll);
                return false;
            }
            catch (EntryPointNotFoundException exEp)
            {
                sw.Stop();
                IoLogger.Error($"Open EntryPointNotFound (RSW={rotarySwitchNo})", exEp);
                return false;
            }
            catch (Exception ex)
            {
                sw.Stop();
                IoLogger.Error($"Open unexpected error (RSW={rotarySwitchNo})", ex);
                return false;
            }
        }

        public void Close(int rotarySwitchNo)
        {
            lock (_sync)
            {
                if (_handleByRsw.TryGetValue(rotarySwitchNo, out var h) && h != IntPtr.Zero)
                {
                    try { _ = DioClose(h); }
                    catch (Exception ex) { IoLogger.Error($"Close error (RSW={rotarySwitchNo})", ex); }
                }
                _handleByRsw.Remove(rotarySwitchNo);
                _outShadowByRsw.Remove(rotarySwitchNo);
            }
            IoLogger.Info($"Close done (RSW={rotarySwitchNo})");
        }

        public void WriteOutput(int rotarySwitchNo, int port, bool value)
        {
            if (port < 0) return;
            int byteIndex = port / 8;
            int bitIndex  = port % 8;

            lock (_sync)
            {
                if (!_handleByRsw.TryGetValue(rotarySwitchNo, out var h) || h == IntPtr.Zero) return;

                if (!_outShadowByRsw.TryGetValue(rotarySwitchNo, out var shadow) || shadow == null || shadow.Length != OUTPUT_BYTES_LEN)
                {
                    shadow = new byte[OUTPUT_BYTES_LEN];
                    _outShadowByRsw[rotarySwitchNo] = shadow;
                }

                byte mask = (byte)(1 << bitIndex);
                if (value) shadow[byteIndex] = (byte)(shadow[byteIndex] |  mask);
                else       shadow[byteIndex] = (byte)(shadow[byteIndex] & ~mask);

                try
                {
                    var rc = DioOutputByte(h, byteIndex, shadow[byteIndex]);
                    if (rc != ERROR_SUCCESS)
                        IoLogger.Warn($"DioOutputByte rc={rc} (RSW={rotarySwitchNo}, port={port}, byteIndex={byteIndex})");
                }
                catch (Exception ex)
                {
                    IoLogger.Error($"DioOutputByte exception (RSW={rotarySwitchNo}, port={port}, byteIndex={byteIndex})", ex);
                }
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

                try
                {
                    var rc = DioInputByte(h, byteIndex, out byte val);
                    if (rc != ERROR_SUCCESS)
                    {
                        IoLogger.Warn($"DioInputByte rc={rc} (RSW={rotarySwitchNo}, port={port}, byteIndex={byteIndex})");
                        return false;
                    }
                    return ((val >> bitIndex) & 0x01) != 0;
                }
                catch (Exception ex)
                {
                    IoLogger.Error($"DioInputByte exception (RSW={rotarySwitchNo}, port={port}, byteIndex={byteIndex})", ex);
                    return false;
                }
            }
        }
    }
}
