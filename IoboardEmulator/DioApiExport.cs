using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace IoboardEmulator
{
    public static class DioApiExport
    {
        private sealed class Session : IDisposable
        {
            private readonly object _lock = new();
            public readonly int Rsw;
            public readonly PipeClient Pipe;
            public readonly byte[] Inputs  = new byte[256];
            public readonly byte[] Outputs = new byte[256];
            public bool IsOpen;

            public Session(int rsw)
            {
                Rsw  = rsw;
                Pipe = new PipeClient();
                Pipe.OnInput += OnInputFromServer;
                Pipe.OnLog   += msg => Common.Logger.Log(msg);
            }

            public void Open()
            {
                if (IsOpen) return;
                Pipe.Start();
                Pipe.SendHelloRsw(Rsw); // ★ 正規APIで握手
                IsOpen = true;
            }

            public void Close()
            {
                if (!IsOpen) return;
                try { Pipe.Dispose(); } catch { }
                IsOpen = false;
            }

            public void WriteBit(int port, int val)
            {
                if (!IsOpen) return;
                if ((uint)port >= 256) return;

                lock (_lock) { Outputs[port] = (byte)(val != 0 ? 1 : 0); }
                Pipe.SendWrite(port, val != 0 ? 1 : 0);
            }

            public int ReadBit(int port)
            {
                if (!IsOpen) return 0;
                if ((uint)port >= 256) return 0;
                lock (_lock) return Inputs[port];
            }

            private void OnInputFromServer(int port, int val)
            {
                if ((uint)port >= 256) return;
                lock (_lock) Inputs[port] = (byte)(val != 0 ? 1 : 0);
            }

            public void Dispose() => Close();
        }

        private static readonly object _sync = new();
        private static readonly Dictionary<int, Session> _sessions = new();

        private static Session GetOrCreate(int rsw)
        {
            lock (_sync)
            {
                if (!_sessions.TryGetValue(rsw, out var s))
                {
                    s = new Session(rsw);
                    _sessions[rsw] = s;
                }
                return s;
            }
        }

        [UnmanagedCallersOnly(EntryPoint = "DioOpen", CallConvs = new[] { typeof(CallConvStdcall) })]
        public static int DioOpen(int rotarySwitchNo)
        {
            try
            {
                var s = GetOrCreate(rotarySwitchNo);
                s.Open();
                return 1;
            }
            catch { return 0; }
        }

        [UnmanagedCallersOnly(EntryPoint = "DioClose", CallConvs = new[] { typeof(CallConvStdcall) })]
        public static void DioClose(int rotarySwitchNo)
        {
            try
            {
                lock (_sync)
                {
                    if (_sessions.TryGetValue(rotarySwitchNo, out var s))
                    {
                        s.Close();
                        _sessions.Remove(rotarySwitchNo);
                    }
                }
            }
            catch { }
        }

        [UnmanagedCallersOnly(EntryPoint = "DioWriteOutput", CallConvs = new[] { typeof(CallConvStdcall) })]
        public static int DioWriteOutput(int rotarySwitchNo, int port, int value)
        {
            try
            {
                var s = GetOrCreate(rotarySwitchNo);
                s.Open();
                s.WriteBit(port, value);
                return 1;
            }
            catch { return 0; }
        }

        [UnmanagedCallersOnly(EntryPoint = "DioReadInput", CallConvs = new[] { typeof(CallConvStdcall) })]
        public static int DioReadInput(int rotarySwitchNo, int port)
        {
            try
            {
                var s = GetOrCreate(rotarySwitchNo);
                s.Open();
                return s.ReadBit(port);
            }
            catch { return 0; }
        }

        [UnmanagedCallersOnly(EntryPoint = "Emu_SetInput", CallConvs = new[] { typeof(CallConvStdcall) })]
        public static void Emu_SetInput(int port, int value)
        {
            Session? target = null;
            lock (_sync)
            {
                if (_sessions.TryGetValue(0, out var s)) target = s;
                else if (_sessions.Count > 0) target = System.Linq.Enumerable.First(_sessions.Values);
            }
            if (target is null) return;
            if ((uint)port >= 256) return;

            typeof(Session)
                .GetMethod("OnInputFromServer", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)?
                .Invoke(target, new object[] { port, value != 0 ? 1 : 0 });
        }
    }
}
