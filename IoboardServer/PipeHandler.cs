using System;
using System.IO.Pipes;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace IoboardServer
{
    public class PipeHandler
    {
        private bool _running;
        private Task _serverTask;
        private CancellationTokenSource _cts;

        public void Start()
        {
            _running = true;
            _cts = new CancellationTokenSource();
            _serverTask = Task.Run(() => RunServer(_cts.Token));
        }

        public void Stop()
        {
            _running = false;
            _cts?.Cancel();
            _serverTask?.Wait();
        }

        private void RunServer(CancellationToken token)
        {
            Console.WriteLine("NamedPipeServer started.");

            while (_running && !token.IsCancellationRequested)
            {
                try
                {
                    using var pipeServer = new NamedPipeServerStream("IOBOARD_PIPE", PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
                    Console.WriteLine("Waiting for client connection...");
                    pipeServer.WaitForConnection();

                    Console.WriteLine("Client connected.");

                    var buffer = new byte[256];
                    int bytesRead;

                    while ((bytesRead = pipeServer.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        var received = Encoding.ASCII.GetString(buffer, 0, bytesRead);
                        Console.WriteLine($"Received: {received}");

                        // echo back
                        var response = Encoding.ASCII.GetBytes($"Echo: {received}");
                        pipeServer.Write(response, 0, response.Length);
                    }

                    Console.WriteLine("Client disconnected.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Pipe Error] {ex.Message}");
                }
            }

            Console.WriteLine("NamedPipeServer stopped.");
        }
    }
}
