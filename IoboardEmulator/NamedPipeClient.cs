using System;
using System.IO.Pipes;
using System.Text;
using Common;

namespace IoboardEmulator
{
    public class NamedPipeClient
    {
        private readonly string _pipeName = "IOBOARD_PIPE";

        public string SendCommand(string command)
        {
            try
            {
                using var pipe = new NamedPipeClientStream(".", _pipeName, PipeDirection.InOut);
                pipe.Connect(1000); // 最大1秒待機

                var data = Encoding.ASCII.GetBytes(command);
                pipe.Write(data, 0, data.Length);
                pipe.Flush();

                var buffer = new byte[256];
                int bytesRead = pipe.Read(buffer, 0, buffer.Length);
                var response = Encoding.ASCII.GetString(buffer, 0, bytesRead);

                Logger.Log($"[PipeClient] Sent: {command}, Received: {response}");
                return response;
            }
            catch (Exception ex)
            {
                Logger.Log($"[PipeClient Error] {ex.Message}");
                return null;
            }
        }
    }
}
