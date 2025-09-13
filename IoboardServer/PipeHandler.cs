// IoboardServer/PipeHandler.cs
// ※ PipeHub.cs 内部で同等の機能を持つため、プロジェクト内に本ファイルが不要なら削除しても構いません。
// （残す場合はテキスト行ベースの簡易ラッパとして利用してください）

using System;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace IoboardServer.IPC
{
    public sealed class PipeHandler : IDisposable
    {
        private readonly NamedPipeServerStream _server;
        private readonly StreamReader _reader;
        private readonly StreamWriter _writer;
        private readonly Action<string> _onLine;

        public PipeHandler(NamedPipeServerStream server, Action<string> onLine)
        {
            _server = server;
            _reader = new StreamReader(server, Encoding.UTF8);
            _writer = new StreamWriter(server, Encoding.UTF8) { AutoFlush = true };
            _onLine = onLine;
        }

        public async Task RunAsync(CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested && _server.IsConnected)
                {
                    var line = await _reader.ReadLineAsync().ConfigureAwait(false);
                    if (line == null) break;
                    _onLine(line);
                }
            }
            catch { /* ignore */ }
        }

        public void SendLine(string line) => _writer.WriteLine(line);

        public void Dispose()
        {
            try { _writer.Dispose(); } catch { }
            try { _reader.Dispose(); } catch { }
            try { _server.Dispose(); } catch { }
        }
    }
}
