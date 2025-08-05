using System;
using System.Runtime.InteropServices;
using System.Text;

namespace IoboardServer.NativeDll
{
    internal static class IoBoardWrapper
    {
        // DLL名（拡張子不要、IoboardEmulator.dll を対象とする）
        private const string DllName = "IoboardEmulator";

        [DllImport(DllName, EntryPoint = "DioOpen", CallingConvention = CallingConvention.StdCall)]
        public static extern bool DioOpen(int rotarySwitchNo);

        [DllImport(DllName, EntryPoint = "DioClose", CallingConvention = CallingConvention.StdCall)]
        public static extern void DioClose(int rotarySwitchNo);

        [DllImport(DllName, EntryPoint = "DioWriteOutput", CallingConvention = CallingConvention.StdCall)]
        public static extern void DioWriteOutput(int rotarySwitchNo, int port, bool value);

        [DllImport(DllName, EntryPoint = "DioReadInput", CallingConvention = CallingConvention.StdCall)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool DioReadInput(int rotarySwitchNo, int port);
    }
}
