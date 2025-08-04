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

        public bool Register(string boardName)
        {
            try
            {
                return RegisterDioHandle(boardName) == 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Register error: {ex.Message}");
                return false;
            }
        }

        public void Unregister()
        {
            try
            {
                UnregisterDioHandle();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unregister error: {ex.Message}");
            }
        }

        public void SetOutput(int port, int value)
        {
            try
            {
                SetOutput(port, value);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"SetOutput error: {ex.Message}");
            }
        }

        public int GetInput(int port)
        {
            try
            {
                return GetInput(port);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"GetInput error: {ex.Message}");
                return -1;
            }
        }
    }
}
