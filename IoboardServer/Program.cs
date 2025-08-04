using System;

namespace IoboardServer
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            Console.Title = "Ioboard Server";
            Console.WriteLine("Ioboard Server started.");
            Console.WriteLine("Press ENTER to exit.");
            var server = new PipeHandler();
            server.Start();
            Console.ReadLine();
            server.Stop();
        }
    }
}
