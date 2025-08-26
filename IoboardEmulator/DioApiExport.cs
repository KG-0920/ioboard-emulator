using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace IoboardEmulator;

public static class DioApiExport
{
    private static readonly object _lock = new();

    // ステート
    private static int _isOpen;
    private static readonly byte[] Inputs  = new byte[256];
    private static readonly byte[] Outputs = new byte[256];

    // Pipeクライアント（サーバは IoboardServer 側）
    private static PipeClient? _pipe;
    private static volatile bool _pipeStarted;

    private static void EnsurePipeStarted()
    {
        if (_pipeStarted) return;
        lock (_lock)
        {
            if (_pipeStarted) return;
            _pipe = new PipeClient();
            _pipe.OnInput += (port, val) =>
            {
                if ((uint)port < 256) lock (_lock) Inputs[port] = (byte)val;
            };
            _pipe.OnLog += msg => { /* 必要ならデバッグ出力 */ };
            _pipe.Start();
            _pipeStarted = true;
        }
    }

    [UnmanagedCallersOnly(EntryPoint = "DioOpen", CallConvs = new[] { typeof(CallConvStdcall) })]
    public static int DioOpen(int rotary)
    {
        EnsurePipeStarted();
        lock (_lock) _isOpen = 1;
        return 1;
    }

    [UnmanagedCallersOnly(EntryPoint = "DioClose", CallConvs = new[] { typeof(CallConvStdcall) })]
    public static void DioClose(int rotary)
    {
        lock (_lock) _isOpen = 0;
        // Pipeは常駐でよい（アプリ終了時にプロセスごと閉じる）
    }

    [UnmanagedCallersOnly(EntryPoint = "DioWriteOutput", CallConvs = new[] { typeof(CallConvStdcall) })]
    public static int DioWriteOutput(int rotary, int port, int value)
    {
        if (_isOpen == 0) return 0;
        if ((uint)port >= 256) return 0;

        lock (_lock) Outputs[port] = (byte)(value != 0 ? 1 : 0);

        // Serverへ「WRITE port val」を送信（UI更新用）
        _pipe?.SendWrite(port, value != 0 ? 1 : 0);
        return 1;
    }

    [UnmanagedCallersOnly(EntryPoint = "DioReadInput", CallConvs = new[] { typeof(CallConvStdcall) })]
    public static int DioReadInput(int rotary, int port)
    {
        if (_isOpen == 0) return 0;
        if ((uint)port >= 256) return 0;
        lock (_lock) return Inputs[port];
    }

    // テスト用：外部から入力を刺す
    [UnmanagedCallersOnly(EntryPoint = "Emu_SetInput", CallConvs = new[] { typeof(CallConvStdcall) })]
    public static void Emu_SetInput(int port, int value)
    {
        if ((uint)port >= 256) return;
        lock (_lock) Inputs[port] = (byte)(value != 0 ? 1 : 0);
    }
}
