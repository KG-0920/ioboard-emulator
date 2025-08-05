using System.IO.Pipes;
using System.Text;
using System.Text.Json;

namespace IoboardServer;

public class PipeHandler
{
    private readonly NamedPipeServerStream _pipeServer;
    private readonly Action<string> _onReceive;

    public PipeHandler(string pipeName, Action<string> onReceive)
    {
        _pipeServer = new NamedPipeServerStream(pipeName, PipeDirection.InOut, 1,
            PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
        _onReceive = onReceive;
    }

    public async Task StartAsync(CancellationToken token)
    {
        try
        {
            await _pipeServer.WaitForConnectionAsync(token);
            using var reader = new StreamReader(_pipeServer, Encoding.UTF8);
            while (!token.IsCancellationRequested)
            {
                string? line = await reader.ReadLineAsync();
                if (line != null)
                {
                    _onReceive(line);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // 正常なキャンセル
        }
        catch (Exception ex)
        {
            Console.WriteLine($"PipeHandler error: {ex.Message}");
        }
    }

    public async Task SendAsync(object message)
    {
        try
        {
            var json = JsonSerializer.Serialize(message);
            byte[] data = Encoding.UTF8.GetBytes(json + "\n");
            await _pipeServer.WriteAsync(data, 0, data.Length);
            await _pipeServer.FlushAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"SendAsync error: {ex.Message}");
        }
    }
}
