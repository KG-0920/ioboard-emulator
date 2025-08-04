using System;
using System.Runtime.InteropServices;
using Common;

namespace IoboardEmulator
{
    public static class IoboardExports
    {
        [DllExport("RegisterDioHandle", CallingConvention = CallingConvention.StdCall)]
        public static int RegisterDioHandle([MarshalAs(UnmanagedType.LPStr)] string boardName)
        {
            Logger.Log($"[DLL] RegisterDioHandle called with {boardName}");
            return 0; // 仮の戻り値
        }

        [DllExport("UnregisterDioHandle", CallingConvention = CallingConvention.StdCall)]
        public static void UnregisterDioHandle()
        {
            Logger.Log("[DLL] UnregisterDioHandle called");
        }

        [DllExport("SetOutput", CallingConvention = CallingConvention.StdCall)]
        public static void SetOutput(int port, int value)
        {
            Logger.Log($"[DLL] SetOutput: Port={port}, Value={value}");
        }

        [DllExport("GetInput", CallingConvention = CallingConvention.StdCall)]
        public static int GetInput(int port)
        {
            Logger.Log($"[DLL] GetInput: Port={port}");
            return 1; // 仮の入力値
        }
    }
}
