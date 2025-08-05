using System;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

namespace IoboardEmulator
{
    public static unsafe class DioApiExport
    {
        // Open
        [UnmanagedCallersOnly(EntryPoint = "DioOpen", CallConvs = new[] { typeof(CallConvStdcall) })]
        public static int DioOpen(int rotarySwitchNo)
        {
            Console.WriteLine($"[Native] DioOpen called with rotarySwitchNo = {rotarySwitchNo}");
            return 1; // 成功
        }

        // Close
        [UnmanagedCallersOnly(EntryPoint = "DioClose", CallConvs = new[] { typeof(CallConvStdcall) })]
        public static void DioClose(int rotarySwitchNo)
        {
            Console.WriteLine($"[Native] DioClose called with rotarySwitchNo = {rotarySwitchNo}");
        }

        // WriteOutput
        [UnmanagedCallersOnly(EntryPoint = "DioWriteOutput", CallConvs = new[] { typeof(CallConvStdcall) })]
        public static void DioWriteOutput(int rotarySwitchNo, int port, bool value)
        {
            Console.WriteLine($"[Native] DioWriteOutput: RSW={rotarySwitchNo}, Port={port}, Value={value}");
        }

        // ReadInput
        [UnmanagedCallersOnly(EntryPoint = "DioReadInput", CallConvs = new[] { typeof(CallConvStdcall) })]
        public static bool DioReadInput(int rotarySwitchNo, int port)
        {
            Console.WriteLine($"[Native] DioReadInput: RSW={rotarySwitchNo}, Port={port}");
            return true; // 仮で常に ON
        }
    }
}
