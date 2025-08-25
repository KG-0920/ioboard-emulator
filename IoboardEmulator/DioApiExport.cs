using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace IoboardEmulator
{
    public static class DioApiExport
    {
        private static readonly object _lock = new();
        private static int _isOpen;
        private static readonly byte[] Inputs  = new byte[256];
        private static readonly byte[] Outputs = new byte[256];

        [UnmanagedCallersOnly(EntryPoint = "DioOpen", CallConvs = new[] { typeof(CallConvStdcall) })]
        public static int DioOpen(int rotarySwitchNo)
        {
            lock (_lock) _isOpen = 1;
            return 1;
        }

        [UnmanagedCallersOnly(EntryPoint = "DioClose", CallConvs = new[] { typeof(CallConvStdcall) })]
        public static void DioClose(int rotarySwitchNo)
        {
            lock (_lock) _isOpen = 0;
        }

        [UnmanagedCallersOnly(EntryPoint = "DioWriteOutput", CallConvs = new[] { typeof(CallConvStdcall) })]
        public static int DioWriteOutput(int rotarySwitchNo, int port, int value)
        {
            if (_isOpen == 0) return 0;
            if ((uint)port >= 256) return 0;
            lock (_lock) Outputs[port] = (byte)(value != 0 ? 1 : 0);
            return 1;
        }

        [UnmanagedCallersOnly(EntryPoint = "DioReadInput", CallConvs = new[] { typeof(CallConvStdcall) })]
        public static int DioReadInput(int rotarySwitchNo, int port)
        {
            if (_isOpen == 0) return 0;
            if ((uint)port >= 256) return 0;
            lock (_lock) return Inputs[port];
        }

        // テスト用：入力を外部から刺す
        [UnmanagedCallersOnly(EntryPoint = "Emu_SetInput", CallConvs = new[] { typeof(CallConvStdcall) })]
        public static void Emu_SetInput(int port, int value)
        {
            if ((uint)port >= 256) return;
            lock (_lock) Inputs[port] = (byte)(value != 0 ? 1 : 0);
        }
    }
}
