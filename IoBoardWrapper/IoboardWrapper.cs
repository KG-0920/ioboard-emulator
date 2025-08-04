using System;
using System.Runtime.InteropServices;

namespace IoBoardWrapper
{
    public class IoboardWrapper : IIoBoardController
    {
        [DllImport("IoboardEmulator.dll", CallingConvention = CallingConvention.StdCall)]
        private static extern int RegisterDioHandle([MarshalAs(UnmanagedType.LPStr)] string boardName);

        [DllImport("IoboardEmulator.dll", CallingConvention = CallingConvention.StdCall)]
        private static extern void UnregisterDioHandle();

        [DllImport("IoboardEmulator.dll", CallingConvention = CallingConvention.StdCall)]
        private static extern void SetOutput(int port, int value);

        [DllImport("IoboardEmulator.dll", CallingConvention = CallingConvention.StdCall)]
        private static extern int GetInput(int port);

        private string _boardName = "FBIDIO0";

        public bool Open(int rotarySwitchNo)
        {
            _boardName = $"FBIDIO{rotarySwitchNo}";
            try
            {
                return RegisterDioHandle(_boardName) == 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Open error: {ex.Message}");
                return false;
            }
        }

        public void Close(int rotarySwitchNo)
        {
            try
            {
                UnregisterDioHandle();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Close error: {ex.Message}");
            }
        }

        public void WriteOutput(int rotarySwitchNo, int port, bool value)
        {
            try
            {
                SetOutput(port, value ? 1 : 0);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"WriteOutput error: {ex.Message}");
            }
        }

        public bool ReadInput(int rotarySwitchNo, int port)
        {
            try
            {
                return GetInput(port) != 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ReadInput error: {ex.Message}");
                return false;
            }
        }
    }
}
