using System;
using System.Runtime.InteropServices;

namespace APP_A
{
    internal class Program
    {
        [DllImport("IoboardEmulator.dll", CallingConvention = CallingConvention.StdCall)]
        public static extern int RegisterDioHandle([MarshalAs(UnmanagedType.LPStr)] string boardName);

        [DllImport("IoboardEmulator.dll", CallingConvention = CallingConvention.StdCall)]
        public static extern void UnregisterDioHandle();

        [DllImport("IoboardEmulator.dll", CallingConvention = CallingConvention.StdCall)]
        public static extern void SetOutput(int port, int value);

        [DllImport("IoboardEmulator.dll", CallingConvention = CallingConvention.StdCall)]
        public static extern int GetInput(int port);

        static void Main(string[] args)
        {
            Console.WriteLine("APP_A started.");

            Console.WriteLine("Registering handle...");
            int result = RegisterDioHandle("FBIDIO0");
            Console.WriteLine($"RegisterDioHandle result = {result}");

            Console.WriteLine("Setting output port 1 to 1");
            SetOutput(1, 1);

            Console.WriteLine("Getting input port 1");
            int input = GetInput(1);
            Console.WriteLine($"GetInput result = {input}");

            Console.WriteLine("Unregistering handle...");
            UnregisterDioHandle();

            Console.WriteLine("APP_A finished.");
        }
    }
}
